<#
Analyze-Logs.ps1
Summarizes large debug-heavy logs into an AI-friendly report.

Typical usage:
  pwsh .\Tools\Analyze-Logs.ps1 -LogPath .\Logs\output.log
  pwsh .\Tools\Analyze-Logs.ps1 -LogPath .\Logs\output.log -Focus "ParkedVehicle","FindParking","TMPE" -Context 40
  pwsh .\Tools\Analyze-Logs.ps1 -LogPath .\Logs\output.log -Prefix "\[PickyParking\]" -Top 40 -OutDir .\_logreports

Notes:
- Works best if your logs have either:
  - timestamps (any format), OR
  - consistent prefix / category markers, OR
  - at least stable repeated lines
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [string]$LogPath,

    # Directory where reports will be written
    [string]$OutDir = ".\_logreports",

    # If you have a stable mod prefix like "[PickyParking]" set it here to focus analysis.
    # Pass "" (empty) to disable prefix filtering.
    [string]$Prefix = "[PickyParking]",

    # If set, prompt interactively for Prefix at runtime (useful for Rider External Tools).
    # - Enter empty string to disable prefix filtering.
    [switch]$PromptPrefix,

    # Optional: focus keywords. Script will show context windows around these.
    [string[]]$Focus = @(),

    # How many lines of context around each focus hit
    [int]$Context = 25,

    # How many items to show in "Top" sections
    [int]$Top = 30,

    # Collapse repeated consecutive identical lines (recommended for spammy logs)
    [switch]$WriteCollapsedLog,

    # Try to detect log "level" (INFO/WARN/ERROR/DEBUG) in-line; works if you have these tokens
    [switch]$DetectLevels,

    # Optional: only analyze last N lines (faster on massive logs)
    [int]$Tail = 0
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ($PromptPrefix) {
    $inputPrefix = Read-Host "Prefix filter (e.g. [PickyParking]) - leave empty for no filter"
    if ([string]::IsNullOrEmpty($inputPrefix)) {
        $Prefix = ""
    } else {
        $Prefix = $inputPrefix
    }
}

if ($PromptPrefix) {
    $entered = Read-Host "Prefix filter (e.g. [PickyParking]) - leave empty to disable"
    if ([string]::IsNullOrEmpty($entered)) { $Prefix = "" } else { $Prefix = $entered }
}

function New-DirIfMissing([string]$path) {
    if (-not (Test-Path -LiteralPath $path)) {
        New-Item -ItemType Directory -Path $path | Out-Null
    }
}

function Normalize-Line([string]$line) {
    # Reduce noise but keep meaning.
    $s = $line.Trim()
    
    # Remove leading "123,456ms |" or "123456ms |"
    $s = [regex]::Replace($s, '^\s*\d{1,3}(?:[.,]\d{3})*ms\s*\|\s*', '')
    
    # Optional: remove leading timestamp forms too (if you have them)
    $s = [regex]::Replace($s, '^\s*\d{4}[-/]\d{2}[-/]\d{2}[ T]\d{2}:\d{2}:\d{2}(?:\.\d{1,3})?\s*\|\s*', '')
    $s = [regex]::Replace($s, '^\s*\d{2}:\d{2}:\d{2}(?:\.\d{1,3})?\s*\|\s*', '')

    # Replace obvious numeric IDs with placeholders to improve grouping.
    # Keep small numbers (like set counts) intact; target longer IDs.
    $s = [regex]::Replace($s, '\b\d{4,}\b', '{N}')

    # Replace hex-ish addresses / ids
    $s = [regex]::Replace($s, '\b0x[0-9A-Fa-f]+\b', '{HEX}')

    # Replace Unity instance IDs like (12345) sometimes
    $s = [regex]::Replace($s, '\(#?\d{4,}\)', '({N})')

    # Collapse whitespace
    $s = [regex]::Replace($s, '\s+', ' ')
    return $s
}

function Guess-Level([string]$line) {
    # Very lightweight token-based detection
    if ($line -match '\b(ERROR|ERR)\b') { return "ERROR" }
    if ($line -match '\b(WARN|WARNING)\b') { return "WARN" }
    if ($line -match '\b(INFO)\b') { return "INFO" }
    if ($line -match '\b(DEBUG|TRACE)\b') { return "DEBUG" }
    return ""
}

function Extract-Timestamp([string]$line) {
    # Try common patterns:
    # 2026-01-16 12:34:56.789
    # 2026/01/16 12:34:56
    # 12:34:56.789
    # 12:34:56
    if ($line -match '^\s*(\d{4}[-/]\d{2}[-/]\d{2}[ T]\d{2}:\d{2}:\d{2}(?:\.\d{1,3})?)') { return $Matches[1] }
    if ($line -match '^\s*(\d{2}:\d{2}:\d{2}(?:\.\d{1,3})?)') { return $Matches[1] }
    return ""
}

function Extract-Category([string]$line, [string]$prefix) {
    $work = $line

    # Strip leading "123,456ms |" to allow bracket parsing even when logs have the ms prefix
    $work = [regex]::Replace($work, '^\s*\d{1,3}(?:[.,]\d{3})*ms\s*\|\s*', '')

    # Extract bracket header from the start (up to 6 tokens)
    $head = [regex]::Match($work, '^\s*(?:\[[^\]]+\]\s*){1,6}').Value
    if (-not [string]::IsNullOrEmpty($head)) {

        $tokens = @([regex]::Matches($head, '\[([^\]]+)\]') | ForEach-Object { $_.Groups[1].Value })

        $prefixToken = ""
        if (-not [string]::IsNullOrEmpty($prefix)) {
            $prefixToken = $prefix.Trim('[', ']')
        }

        # [PickyParking] [DecisionPipeline] [Parking] ...
        if (-not [string]::IsNullOrEmpty($prefixToken) -and $tokens.Length -ge 2 -and $tokens[0] -eq $prefixToken) {
            return $tokens[1]
        }

        if ($tokens.Length -ge 2) { return $tokens[1] }
        if ($tokens.Length -ge 1) { return $tokens[0] }
    }

    # Fallback: "Category: message"
    if ($work -match '^\s*([A-Za-z0-9_.-]{3,40})\s*:\s+') {
        return $Matches[1]
    }

    return "Uncategorized"
}

function Extract-DecisionGroupKey([string]$line) {
    if ([string]::IsNullOrEmpty($line)) { return "" }

    # Work on a copy and strip common leading timing prefixes so parsing is stable:
    # "123,456ms | ..." or "123456ms | ..."
    $work = [regex]::Replace($line, '^\s*\d{1,3}(?:[.,]\d{3})*ms\s*\|\s*', '')

    # If you also sometimes have timestamps like "2026-01-16 12:34:56.789 | ..."
    $work = [regex]::Replace($work, '^\s*\d{4}[-/]\d{2}[-/]\d{2}[ T]\d{2}:\d{2}:\d{2}(?:\.\d{1,3})?\s*\|\s*', '')
    $work = [regex]::Replace($work, '^\s*\d{2}:\d{2}:\d{2}(?:\.\d{1,3})?\s*\|\s*', '')

    # Only group "decision-ish" lines: either explicitly tagged, or from your decision pipeline category.
    # (Adjust tokens if you rename categories.)
    if ($work -notmatch '\bevent=' -and $work -notmatch '\bDecisionPipeline\b') { return "" }

    $event = ""
    $denied = ""
    $reason = ""
    $isVisitor = ""
    $source = ""
    $ruleFlags = ""

    # event=CandidateDecision
    if ($work -match '\bevent=([A-Za-z0-9_.:-]+)\b') { $event = $Matches[1] }

    # denied=True/False
    if ($work -match '\bdenied=(True|False)\b') { $denied = $Matches[1] }

    # reason=Denied_NoMatch (or any token up to whitespace)
    if ($work -match '\breason=([A-Za-z0-9_]+)\b') { $reason = $Matches[1] }

    # isVisitor=True/False
    if ($work -match '\bisVisitor=(True|False)\b') { $isVisitor = $Matches[1] }

    # source=TMPE.FindParkingSpaceForCitizen
    if ($work -match '\bsource=([A-Za-z0-9_.:-]+)\b') { $source = $Matches[1] }

    # rule=ResidentsOnly=True (500m), WorkSchoolOnly=True (200m), VisitorsAllowed=False
    # Extract only booleans (ignore radii) to avoid exploding grouping keys.
    $ro = ""
    $wo = ""
    $va = ""
    if ($work -match '\bResidentsOnly=(True|False)\b') { $ro = $Matches[1] }
    if ($work -match '\bWorkSchoolOnly=(True|False)\b') { $wo = $Matches[1] }
    if ($work -match '\bVisitorsAllowed=(True|False)\b') { $va = $Matches[1] }
 if ($ro -ne "" -or $wo -ne "" -or $va -ne "") {
        # Helper variables for logic (Safe for all PowerShell versions)
        $roVal = if ($ro -ne "") { $ro } else { "?" }
        $woVal = if ($wo -ne "") { $wo } else { "?" }
        $vaVal = if ($va -ne "") { $va } else { "?" }

        $ruleFlags = ('ResidentsOnly={0},WorkSchoolOnly={1},VisitorsAllowed={2}' -f $roVal, $woVal, $vaVal)
    }

    # Fallback "event" if you haven't added event= everywhere yet:
    # Try to use the 2nd bracket token for your logs: [PickyParking] [DecisionPipeline] [Parking] ...
    if ([string]::IsNullOrEmpty($event)) {
        $head = [regex]::Match($work, '^\s*(?:\[[^\]]+\]\s*){1,8}').Value
        if (-not [string]::IsNullOrEmpty($head)) {
            $tokens = @([regex]::Matches($head, '\[([^\]]+)\]') | ForEach-Object { $_.Groups[1].Value })
            if ($tokens.Length -ge 2) { $event = $tokens[1] }
            elseif ($tokens.Length -ge 1) { $event = $tokens[0] }
        }
    }

    # If we still don't have a stable anchor, skip.
    if ([string]::IsNullOrEmpty($event)) { return "" }

    # Normalize missing fields so grouping stays stable.
    if ($denied -eq "") { $denied = "?" }
    if ($reason -eq "") { $reason = "?" }
    if ($isVisitor -eq "") { $isVisitor = "?" }
    if ($source -eq "") { $source = "?" }
    if ($ruleFlags -eq "") { $ruleFlags = "ruleFlags=?" }

    # Final grouping key.
    return ('event={0} | denied={1} | reason={2} | isVisitor={3} | source={4} | {5}' -f `
        $event, $denied, $reason, $isVisitor, $source, $ruleFlags)
}

function Take-TailIfNeeded([string[]]$lines, [int]$tail) {
    if ($tail -le 0) { return $lines }
    if ($lines.Count -le $tail) { return $lines }
    return $lines[($lines.Count - $tail)..($lines.Count - 1)]
}

function Build-CollapsedLog([object[]]$entries) {
    # entries: objects with .Raw
    $out = New-Object System.Collections.Generic.List[string]
    $i = 0
    while ($i -lt $entries.Count) {
        $raw = $entries[$i].Raw
        $count = 1
        $j = $i + 1
        while ($j -lt $entries.Count -and $entries[$j].Raw -eq $raw) {
            $count++
            $j++
        }

        if ($count -ge 3) {
            $out.Add("$raw")
            $out.Add("  ... repeated $count times ...")
        } else {
            for ($k=0; $k -lt $count; $k++) { $out.Add($raw) }
        }

        $i = $j
    }
    return $out
}

function Get-ContextWindows([string[]]$rawLines, [string[]]$focus, [int]$context) {
    if (-not $focus -or $focus.Count -eq 0) { return @() }

    $hits = New-Object System.Collections.Generic.List[object]
    for ($i=0; $i -lt $rawLines.Count; $i++) {
        $line = $rawLines[$i]
        foreach ($f in $focus) {
            if ([string]::IsNullOrWhiteSpace($f)) { continue }
            if ($line -match [regex]::Escape($f)) {
                $start = [Math]::Max(0, $i - $context)
                $end = [Math]::Min($rawLines.Count - 1, $i + $context)
                $window = $rawLines[$start..$end]
                $hits.Add([pscustomobject]@{
                    Focus = $f
                    LineIndex = $i
                    Start = $start
                    End = $end
                    Window = $window
                })
                break
            }
        }
    }

    # Merge overlapping windows per focus to avoid spam
    $merged = New-Object System.Collections.Generic.List[object]
    $grouped = $hits | Group-Object Focus
    foreach ($g in $grouped) {
        $focusKey = $g.Name
        $sorted = $g.Group | Sort-Object Start, End
        $cur = $null
        foreach ($h in $sorted) {
            if ($null -eq $cur) { $cur = $h; continue }
            if ($h.Start -le ($cur.End + 3)) {
                $cur = [pscustomobject]@{
                    Focus = $focusKey
                    Start = [Math]::Min($cur.Start, $h.Start)
                    End = [Math]::Max($cur.End, $h.End)
                }
            } else {
                $merged.Add($cur)
                $cur = $h
            }
        }
        if ($null -ne $cur) { $merged.Add($cur) }
    }

    # Rebuild windows from merged ranges
    return $merged | ForEach-Object {
        [pscustomobject]@{
            Focus = $_.Focus
            Start = $_.Start
            End = $_.End
            Window = $rawLines[$_.Start..$_.End]
        }
    }
}

trap {
    Write-Host ""
    Write-Host "ERROR at:" -ForegroundColor Red
    Write-Host $_.InvocationInfo.PositionMessage -ForegroundColor Red
    throw
}

# ---- main ----

if (-not (Test-Path -LiteralPath $LogPath)) {
    throw "LogPath not found: $LogPath"
}

New-DirIfMissing $OutDir

$baseName = [IO.Path]::GetFileNameWithoutExtension($LogPath)
$stamp = (Get-Date).ToString("yyyyMMdd_HHmmss")
$reportPath = Join-Path $OutDir "$baseName.$stamp.report.md"
$collapsedPath = Join-Path $OutDir "$baseName.$stamp.collapsed.log"

Write-Host "Reading log: $LogPath"
$rawLinesAll = Get-Content -LiteralPath $LogPath -ErrorAction Stop

$rawLines = Take-TailIfNeeded $rawLinesAll $Tail
$lineCount = $rawLines.Count

# Build entries
# IMPORTANT: compute all derived fields first, then add exactly one entry per raw line.
$entries = New-Object System.Collections.Generic.List[object]
for ($i = 0; $i -lt $rawLines.Count; $i++) {
    $raw = $rawLines[$i]

    $isPrefixMatch = $true
    if (-not [string]::IsNullOrEmpty($Prefix) -and ($raw -notmatch [regex]::Escape($Prefix))) {
        $isPrefixMatch = $false
    }

    $norm = Normalize-Line $raw
    $ts = Extract-Timestamp $raw
    $lvl = ""
    if ($DetectLevels) { $lvl = Guess-Level $raw }
    $cat = Extract-Category $raw $Prefix
    $groupKey = Extract-DecisionGroupKey $raw

    # This is what we group on for the "Hot spots" table:
    # - If the line can be parsed into a decision group key, use that.
    # - Otherwise use the normalized line.
    $hotspotKey = $norm
    if (-not [string]::IsNullOrEmpty($groupKey)) { $hotspotKey = $groupKey }

    $entries.Add([pscustomobject]@{
        Index        = $i
        Raw          = $raw
        Normalized   = $norm
        HotspotKey   = $hotspotKey
        Timestamp    = $ts
        Level        = $lvl
        Category     = $cat
        PrefixMatch  = $isPrefixMatch
        DecisionGroup = $groupKey
    })
}

# Stats: duplicates (normalized), consecutive duplicates, rare lines
$analysisEntries = if ($Prefix) { $entries | Where-Object { $_.PrefixMatch } } else { $entries }

$decisionGroups = $analysisEntries |
    Where-Object { -not [string]::IsNullOrEmpty($_.DecisionGroup) } |
    Group-Object DecisionGroup |
    Sort-Object Count -Descending |
    Select-Object -First ([Math]::Min($Top, 30))
    
$topNormalized = $analysisEntries |
    Group-Object HotspotKey |
    Sort-Object Count -Descending |
    Select-Object -First $Top

$rareNormalized = $analysisEntries |
    Group-Object Normalized |
    Where-Object { $_.Count -eq 1 } |
    Select-Object -First ([Math]::Min($Top, 30))

$byCategory = $analysisEntries |
    Group-Object Category |
    Sort-Object Count -Descending |
    Select-Object -First ([Math]::Min($Top, 25))

$byLevel = @()
if ($DetectLevels) {
    $byLevel = $analysisEntries |
        Group-Object Level |
        Sort-Object Count -Descending
}

# Context windows around focus terms (use full raw lines, not prefix-filtered)
$windows = Get-ContextWindows -rawLines $rawLines -focus $Focus -context $Context

# Write collapsed log if requested
if ($WriteCollapsedLog) {
    Write-Host "Writing collapsed log: $collapsedPath"
    $collapsed = Build-CollapsedLog $entries
    $collapsed | Set-Content -LiteralPath $collapsedPath -Encoding UTF8
}

# Build report
$sb = New-Object System.Text.StringBuilder
$null = $sb.AppendLine("# Log analysis report")
$null = $sb.AppendLine("")

$resolvedLogPath = (Resolve-Path -LiteralPath $LogPath).Path

$tailSuffix = ""
if ($Tail -gt 0) { $tailSuffix = " (tail $Tail)" }

$prefixLine = "none"
if ($Prefix) { $prefixLine = "$Prefix (analysis filtered to matching lines)" }

$null = $sb.AppendLine("- **Source:** $resolvedLogPath")
$null = $sb.AppendLine("- **Lines analyzed:** $lineCount$tailSuffix")
$null = $sb.AppendLine("- **Prefix filter:** $prefixLine")
$null = $sb.AppendLine("- **Generated:** $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')")

if ($WriteCollapsedLog) {
    $resolvedCollapsedPath = (Resolve-Path -LiteralPath $collapsedPath).Path
    $null = $sb.AppendLine("- **Collapsed log:** $resolvedCollapsedPath")
}

$null = $sb.AppendLine("")


$null = $sb.AppendLine("## Hot spots (most frequent grouped lines)")
$null = $sb.AppendLine("")
$null = $sb.AppendLine("| Count | Example (grouped key) |")
$null = $sb.AppendLine("|---:|---|")
foreach ($g in $topNormalized) {
    $example = $g.Name.Replace("|","\|")
    $null = $sb.AppendLine("| $($g.Count) | $example |")
}
$null = $sb.AppendLine("")

$null = $sb.AppendLine("## Top categories")
$null = $sb.AppendLine("")
$null = $sb.AppendLine("| Count | Category |")
$null = $sb.AppendLine("|---:|---|")
foreach ($g in $byCategory) {
    $catName = $g.Name
    if ([string]::IsNullOrEmpty($catName)) { $catName = "Uncategorized" }
    $cat = $catName.Replace("|","\|")
    $null = $sb.AppendLine("| $($g.Count) | $cat |")
}
$null = $sb.AppendLine("")

if ($DetectLevels) {
    $null = $sb.AppendLine("## Levels (heuristic)")
    $null = $sb.AppendLine("")
    $null = $sb.AppendLine("| Count | Level |")
    $null = $sb.AppendLine("|---:|---|")
    foreach ($g in $byLevel) {
        $lvlName = $g.Name
        if ([string]::IsNullOrEmpty($lvlName)) { $lvlName = "(none)" }
        $lvl = $lvlName.Replace("|","\|")
        $null = $sb.AppendLine("| $($g.Count) | $lvl |")
    }
    $null = $sb.AppendLine("")
}

$null = $sb.AppendLine("## Rare lines (occur once; often the clue)")
$null = $sb.AppendLine("")
# Show the raw line (not normalized) for each unique normalized, to preserve content
$shown = 0
foreach ($g in $rareNormalized) {
    if ($shown -ge 20) { break }
    $norm = $g.Name
    $entry = $analysisEntries | Where-Object { $_.Normalized -eq $norm } | Select-Object -First 1
    if ($null -eq $entry) { continue }
    $rawEsc = $entry.Raw
    $null = $sb.AppendLine("- Line $($entry.Index): $rawEsc")
    $shown++
}
$null = $sb.AppendLine("")

if ($windows -and $windows.Count -gt 0) {
    $null = $sb.AppendLine("## Focus term context windows")
    $null = $sb.AppendLine("")

    foreach ($w in $windows) {
        $focusHeader = '### Focus: "{0}" (lines {1}-{2})' -f $w.Focus, $w.Start, $w.End
        $null = $sb.AppendLine($focusHeader)
        $null = $sb.AppendLine("")
        $null = $sb.AppendLine("---- BEGIN CONTEXT ----")
        
        foreach ($line in $w.Window) {
            $null = $sb.AppendLine($line)
        }
        
        $null = $sb.AppendLine("---- END CONTEXT ----")

        $null = $sb.AppendLine("")
    }
}

# Extra: simple "bursts" detection: where the same normalized line repeats heavily
$null = $sb.AppendLine("## Bursts (consecutive repetition hotspots)")
$null = $sb.AppendLine("")
$burstCount = 0
$i = 0
while ($i -lt $analysisEntries.Count -and $burstCount -lt 15) {
    $cur = $analysisEntries[$i].Normalized
    $start = $analysisEntries[$i].Index
    $len = 1
    $j = $i + 1
    while ($j -lt $analysisEntries.Count -and $analysisEntries[$j].Normalized -eq $cur) {
        $len++
        $j++
    }
    if ($len -ge 20) {
        $end = $analysisEntries[$j-1].Index
        $null = $sb.AppendLine(('- **{0}x** consecutive at lines {1}-{2}: `{3}`' -f $len, $start, $end, $cur))
        $burstCount++
    }
    $i = $j
}
if ($burstCount -eq 0) {
    $null = $sb.AppendLine("None found (no long consecutive repeats).")
}
$null = $sb.AppendLine("")

Write-Host "Writing report: $reportPath"
$sb.ToString() | Set-Content -LiteralPath $reportPath -Encoding UTF8

Write-Host ""
Write-Host "Done."
Write-Host "Report: $reportPath"
if ($WriteCollapsedLog) { Write-Host "Collapsed log: $collapsedPath" }

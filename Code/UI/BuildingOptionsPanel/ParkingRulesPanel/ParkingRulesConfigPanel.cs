using UnityEngine;
using ColossalFramework.UI;
using PickyParking.Features.Debug;
using PickyParking.Features.ParkingRules;
using PickyParking.Logging;
using PickyParking.UI.BuildingOptionsPanel;
using PickyParking.Settings;

namespace PickyParking.UI.BuildingOptionsPanel.ParkingRulesPanel
{
    
    
    
    
    public sealed class ParkingRulesConfigPanel : UIPanel
    {
        private ParkingRulesConfigPanelState _state;
        private ParkingRulesConfigPanelUi _ui;
        private ParkingRulesConfigEditor _editor;
        private ParkingRulesConfigUiConfig _uiConfig;
        private ParkingPanelTheme _theme;
        private UiServices _services;

        public void Initialize(UiServices services)
        {
            _services = services;
        }

        public override void Start()
        {
            base.Start();

            _editor = _services != null ? _services.ParkingRulesConfigEditor : null;
            _uiConfig = _editor != null ? _editor.UiConfig : ParkingRulesConfigUiConfig.Default;
            _theme = new ParkingPanelTheme(_services);
            _state = new ParkingRulesConfigPanelState();

            _ui = ParkingRulesConfigPanelUi.Create(
                this,
                _theme,
                _uiConfig,
                GetDefaultSliderValue,
                ToggleRestrictions,
                ToggleSliderRow,
                HandleSliderValueChanged,
                ToggleVisitorsRow,
                ApplyChangesFromButton,
                HandleCopyRule,
                HandlePasteRule,
                HandleResetChanges);
            _ui.ConfigurePanel();
            _ui.BuildUi();
        }

        public override void Update()
        {
            base.Update();
            if (_ui == null || !IsPanelVisibleForStats())
                return;

            if (!_state.IsReadyForStatsUpdate(Time.unscaledTime))
                return;

            _state.ScheduleNextStatsUpdate(Time.unscaledTime + BuildingOptionsPanelUiValues.RulesPanel.ParkingStatsUpdateIntervalSeconds);
            UpdateParkingSpaceStats();
        }

        public void Bind(ushort buildingId)
        {
            if (_state.BuildingId != 0 && _state.BuildingId != buildingId)
            {
                DiscardUnappliedChanges();
            }

            _state.BindBuilding(buildingId);
            Refresh();
        }

        public void SetPrefabSupported(bool supported)
        {
            _state.SetPrefabSupported(supported);
        }

        public void CommitPendingChanges()
        {
            if (!_state.IsDirty)
                return;

            _state.ClearDirty();
            RefreshFooterButtons();

            if (!CanOperateOnBuilding())
                return;

            if (!_state.RestrictionsEnabled)
                return;

            _editor.CommitPendingChanges(_state.BuildingId, BuildInput());
        }

        public void DiscardUnappliedChangesIfAny()
        {
            DiscardUnappliedChanges();
        }

        public void ClearPreview()
        {
            if (_editor == null)
                return;

            _editor.ClearPreview(_state.BuildingId);
        }

        public void RequestPendingReevaluationIfAny()
        {
            RequestPendingReevaluationIfAny(_state.BuildingId);
        }

        private void ToggleSliderRow(ParkingRulesSliderRow row)
        {
            if (!_state.RestrictionsEnabled)
                return;

            if (row.IsEnabled)
            {
                DisableSliderRow(row);
            }
            else
            {
                EnableSliderRow(row);
            }

            MarkDirty();
            UpdatePreviewRule();
        }

        private void ToggleVisitorsRow()
        {
            if (!_state.RestrictionsEnabled)
                return;

            _ui.VisitorsRow.IsEnabled = !_ui.VisitorsRow.IsEnabled;
            _ui.UpdateToggleRowVisuals(_ui.VisitorsRow);
            MarkDirty();
            UpdatePreviewRule();
        }

        private void HandleSliderValueChanged(ParkingRulesSliderRow row, float value)
        {
            if (!_state.RestrictionsEnabled)
                return;

            if (_state.IsUpdatingUi)
                return;

            float snappedValue = SnapSliderValue(value);
            if (!Mathf.Approximately(snappedValue, value))
            {
                _state.BeginUiSync();
                row.Slider.value = snappedValue;
                _state.EndUiSync();
            }

            value = snappedValue;

            if (value <= 0f)
            {
                row.IsEnabled = false;
                _ui.UpdateSliderRowLabel(row);
                _ui.UpdateSliderRowVisuals(row);
            }
            else
            {
                row.IsEnabled = true;
                row.LastNonZeroValue = value;
                _ui.UpdateSliderRowLabel(row);
                _ui.UpdateSliderRowVisuals(row);
            }

            MarkDirty();
            UpdatePreviewRule();
        }

        private void EnableSliderRow(ParkingRulesSliderRow row)
        {
            row.IsEnabled = true;
            if (row.Slider.value <= 0f)
            {
                float restoreValue = row.LastNonZeroValue > 0f ? row.LastNonZeroValue : GetDefaultSliderValue();
                SetSliderValue(row, restoreValue);
            }
            _ui.UpdateSliderRowVisuals(row);
        }

        private void DisableSliderRow(ParkingRulesSliderRow row)
        {
            row.IsEnabled = false;

            if (row.Slider.value > 0f)
                row.LastNonZeroValue = row.Slider.value;

            _ui.UpdateSliderRowVisuals(row);
        }

        private void SetSliderValue(ParkingRulesSliderRow row, float value)
        {
            value = SnapSliderValue(value);
            _state.BeginUiSync();
            row.Slider.value = value;
            if (value > 0f)
                row.LastNonZeroValue = value;
            _ui.UpdateSliderRowLabel(row);
            _state.EndUiSync();
        }

        private float SnapSliderValue(float value)
        {
            if (_uiConfig != null)
            {
                float offThreshold = Mathf.Max(0f, _uiConfig.DistanceSliderMinValue - _uiConfig.SliderStep * 0.5f);
                if (value <= offThreshold)
                    return 0f;
            }
            else if (value <= 0f)
                return 0f;
            if (value >= BuildingOptionsPanelUiValues.RulesPanel.SliderAllThreshold)
                return 1f;
            return value;
        }

        private void DiscardUnappliedChanges()
        {
            if (!_state.HasUnappliedChanges)
                return;

            ClearPreview();
            ApplyRuleToUi(_state.BaselineRule);
            _state.ClearDirty();
            RefreshFooterButtons();
        }

        private void MarkDirty()
        {
            _state.MarkDirty();
            RefreshFooterButtons();
        }

        private bool CanOperateOnBuilding()
        {
            return _state.BuildingId != 0 && _editor != null && _state.IsPrefabSupported;
        }

        private bool CanOperateOnBuilding(ushort buildingId)
        {
            return buildingId != 0 && _editor != null && _state.IsPrefabSupported;
        }

        private void Refresh()
        {
            if (_editor == null)
                return;

            bool hasStoredRule = _editor.TryGetStoredRule(_state.BuildingId, out var storedRule);

            if (hasStoredRule)
            {
                _state.SetBaselineRule(storedRule, restrictionsEnabled: true, hasStoredRule: true);
                ApplyRuleToUi(storedRule);
            }
            else
            {
                var baselineRule = new ParkingRulesConfigDefinition(
                    residentsWithinRadiusOnly: false,
                    residentsRadiusMeters: ParkingRulesLimits.DefaultRadiusMeters,
                    workSchoolWithinRadiusOnly: false,
                    workSchoolRadiusMeters: ParkingRulesLimits.DefaultRadiusMeters,
                    visitorsAllowed: false);
                _state.SetBaselineRule(baselineRule, restrictionsEnabled: false, hasStoredRule: false);
            }

            UpdateRestrictionsVisibility();
            UpdatePreviewRule();
            UpdateParkingSpaceStats();
            RefreshFooterButtons();

            if (hasStoredRule && Log.Dev.IsEnabled(DebugLogCategory.RuleUi))
            {
                Log.Dev.Info(
                    DebugLogCategory.RuleUi,
                    LogPath.Any,
                    "RulesPanelRefreshed",
                    "buildingId=" + _state.BuildingId + " | rule=" + _editor.FormatRule(storedRule));
            }
        }

        private void ApplyRuleToUi(ParkingRulesConfigDefinition rule)
        {
            ApplySliderRowFromRule(_ui.ResidentsRow, rule.ResidentsWithinRadiusOnly, rule.ResidentsRadiusMeters);
            ApplySliderRowFromRule(_ui.WorkSchoolRow, rule.WorkSchoolWithinRadiusOnly, rule.WorkSchoolRadiusMeters);

            _ui.VisitorsRow.IsEnabled = rule.VisitorsAllowed;
            _ui.UpdateToggleRowVisuals(_ui.VisitorsRow);
        }

        private void ApplySliderRowFromRule(ParkingRulesSliderRow row, bool enabled, ushort radiusMeters)
        {
            if (_ui == null)
                return;

            _ui.ApplySliderRowFromRule(
                row,
                enabled,
                radiusMeters,
                ConvertRadiusToSliderValue,
                SetSliderValue);
        }

        private void UpdatePreviewRule()
        {
            if (!CanOperateOnBuilding())
                return;

            if (!_state.RestrictionsEnabled)
                return;

            _editor.UpdatePreview(_state.BuildingId, BuildInput());
        }

        private void ApplyChangesInternal(string reason)
        {
            if (!CanOperateOnBuilding())
                return;

            _state.BeginApplying();
            _editor.ApplyRuleNow(_state.BuildingId, BuildInput(), reason);
            _state.ClearDirty();
            RefreshFooterButtons();
        }

        private void ApplyChangesFromButton()
        {
            if (!CanOperateOnBuilding())
                return;

            if (!_state.RestrictionsEnabled)
                return;

            ParkingRulesConfigDefinition input = BuildInput();
            _state.BeginApplying();
            _editor.ApplyRuleNow(_state.BuildingId, input, "ApplyButton");
            _editor.RequestPendingReevaluationIfAny(_state.BuildingId);
            _state.CommitApplied(input);
            RefreshFooterButtons();
        }

        private void HandleCopyRule()
        {
            if (!CanOperateOnBuilding() || !_state.RestrictionsEnabled)
                return;

            _state.StoreClipboardRule(BuildInput());
            RefreshFooterButtons();
        }

        private void HandlePasteRule()
        {
            if (!_state.HasClipboardRule)
                return;

            if (!CanOperateOnBuilding() || !_state.RestrictionsEnabled)
                return;

            ApplyRuleToUi(_state.ClipboardRule);
            MarkDirty();
            UpdatePreviewRule();
        }

        private void HandleResetChanges()
        {
            if (!_state.HasStoredRule)
                return;

            if (!CanOperateOnBuilding() || !_state.RestrictionsEnabled)
                return;

            ApplyRuleToUi(_state.BaselineRule);
            _state.ClearDirty();
            UpdatePreviewRule();
            RefreshFooterButtons();
        }

        private void RequestPendingReevaluationIfAny(ushort buildingId)
        {
            if (!CanOperateOnBuilding(buildingId))
                return;

            _editor.RequestPendingReevaluationIfAny(buildingId);
        }

        private float GetDefaultSliderValue()
        {
            return ConvertRadiusToSliderValue(ParkingRulesLimits.DefaultRadiusMeters);
        }

        private void ToggleRestrictions()
        {
            if (!CanOperateOnBuilding())
                return;

            bool enableRestrictions = !_state.RestrictionsEnabled;
            if (enableRestrictions)
                _state.EnableRestrictions();
            else
                _state.DisableRestrictions();
            UpdateRestrictionsVisibility();

            if (!enableRestrictions)
            {
                ClearPreview();
                _editor.RemoveRule(_state.BuildingId, "RestrictionsToggleOff");
                RefreshFooterButtons();
                return;
            }

            if (!_state.HasStoredRule)
            {
                var baselineRule = new ParkingRulesConfigDefinition(
                    residentsWithinRadiusOnly: true,
                    residentsRadiusMeters: BuildingOptionsPanelUiValues.RulesPanel.DefaultNewRuleRadiusMeters,
                    workSchoolWithinRadiusOnly: true,
                    workSchoolRadiusMeters: BuildingOptionsPanelUiValues.RulesPanel.DefaultNewRuleRadiusMeters,
                    visitorsAllowed: true);
                ApplyRuleToUi(baselineRule);
                _editor.ApplyRuleNow(_state.BuildingId, BuildInput(), "DefaultsOnEnable");
                _state.CommitApplied(baselineRule);
            }
            else
            {
                ApplyRuleToUi(_state.BaselineRule);
            }
            UpdatePreviewRule();
            RefreshFooterButtons();
        }

        private void UpdateRestrictionsVisibility()
        {
            if (_ui == null)
                return;

            _ui.SetRestrictionsContentVisible(_state.RestrictionsEnabled);
            _ui.UpdateRestrictionsToggleVisuals(_state.RestrictionsEnabled);
        }

        private void RefreshFooterButtons()
        {
            if (_ui == null)
                return;

            bool hasUnappliedChanges = _state.HasUnappliedChanges;
            bool canOperate = CanOperateOnBuilding();
            bool restrictionsEnabled = _state.RestrictionsEnabled;
            bool actionAvailable = canOperate && restrictionsEnabled;

            _ui.UpdateApplyButtonState(hasUnappliedChanges);
            bool copyEnabled = !hasUnappliedChanges && actionAvailable;
            bool pasteEnabled = _state.HasClipboardRule && actionAvailable;
            bool resetEnabled = hasUnappliedChanges && _state.HasStoredRule && actionAvailable;
            _ui.UpdateCopyButtonState(copyEnabled);
            _ui.UpdatePasteButtonState(pasteEnabled);
            _ui.UpdateResetButtonState(resetEnabled);
        }

        private bool IsPanelVisibleForStats()
        {
            return isVisible;
        }

        private void UpdateParkingSpaceStats()
        {
            if (_ui == null || _services == null)
                return;

            int totalSpaces;
            int occupiedSpaces;
            if (!_services.Game.TryGetParkingSpaceStats(_state.BuildingId, out totalSpaces, out occupiedSpaces))
            {
                _ui.UpdateParkingSpacesUnavailable();
                return;
            }

            int freeSpaces = totalSpaces - occupiedSpaces;
            if (freeSpaces < 0)
                freeSpaces = 0;

            _ui.UpdateParkingSpacesText(totalSpaces, freeSpaces);
        }

        private ParkingRulesConfigDefinition BuildInput()
        {
            ParkingRulesSliderRow residentsRow = _ui != null ? _ui.ResidentsRow : null;
            ParkingRulesSliderRow workSchoolRow = _ui != null ? _ui.WorkSchoolRow : null;
            ParkingRulesToggleRow visitorsRow = _ui != null ? _ui.VisitorsRow : null;

            float residentsValue = residentsRow != null && residentsRow.Slider != null ? residentsRow.Slider.value : 0f;
            float workValue = workSchoolRow != null && workSchoolRow.Slider != null ? workSchoolRow.Slider.value : 0f;

            bool residentsEnabled = residentsRow != null && residentsRow.IsEnabled;
            bool workEnabled = workSchoolRow != null && workSchoolRow.IsEnabled;

            float storedResidents = residentsRow != null ? residentsRow.LastNonZeroValue : 0f;
            float storedWork = workSchoolRow != null ? workSchoolRow.LastNonZeroValue : 0f;

            float residentsSliderValue = residentsEnabled ? residentsValue : GetStoredSliderValue(storedResidents);
            float workSliderValue = workEnabled ? workValue : GetStoredSliderValue(storedWork);

            return new ParkingRulesConfigDefinition(
                residentsEnabled,
                ConvertSliderValueToRadius(residentsSliderValue),
                workEnabled,
                ConvertSliderValueToRadius(workSliderValue),
                visitorsRow != null && visitorsRow.IsEnabled);
        }

        private float GetStoredSliderValue(float stored)
        {
            return stored > 0f ? stored : ConvertRadiusToSliderValue(ParkingRulesLimits.DefaultRadiusMeters);
        }

        private float ConvertRadiusToSliderValue(ushort radiusMeters)
        {
            if (radiusMeters == 0)
                return 0f;

            if (radiusMeters >= ParkingRulesLimits.AllRadiusMeters)
                return 1f;

            float clamped = Mathf.Clamp(radiusMeters, ParkingRulesLimits.MinRadiusMeters, ParkingRulesLimits.MaxRadiusMeters);
            if (clamped <= ParkingRulesLimits.MinRadiusMeters)
                return _uiConfig.DistanceSliderMinValue;
            if (clamped >= ParkingRulesLimits.MaxRadiusMeters)
                return _uiConfig.DistanceSliderMaxValue;

            return DistanceSliderMapping.DistanceMetersToSlider(clamped, _uiConfig);
        }

        private ushort ConvertSliderValueToRadius(float normalizedSliderValue)
        {
            if (normalizedSliderValue <= 0f)
                return 0;

            if (normalizedSliderValue >= 1f)
                return ParkingRulesLimits.AllRadiusMeters;

            if (normalizedSliderValue <= _uiConfig.DistanceSliderMinValue)
                return ParkingRulesLimits.MinRadiusMeters;
            if (normalizedSliderValue >= _uiConfig.DistanceSliderMaxValue)
                return ParkingRulesLimits.MaxRadiusMeters;

            float meters = DistanceSliderMapping.SliderToDistanceMeters(normalizedSliderValue, _uiConfig);
            int rounded = Mathf.RoundToInt(meters);
            if (rounded < ParkingRulesLimits.MinRadiusMeters) rounded = ParkingRulesLimits.MinRadiusMeters;
            if (rounded > ParkingRulesLimits.MaxRadiusMeters) rounded = ParkingRulesLimits.MaxRadiusMeters;
            return (ushort)rounded;
        }

        private sealed class ParkingRulesConfigPanelState
        {
            internal enum PanelMode
            {
                Disabled,
                Viewing,
                Editing,
                Applying
            }

            private ushort _buildingId;
            private bool _isDirty;
            private bool _isUpdatingUi;
            private bool _restrictionsEnabled;
            private bool _hasStoredRule;
            private ParkingRulesConfigDefinition _baselineRule;
            private bool _isPrefabSupported;
            private float _nextParkingStatsUpdateTime;
            private PanelMode _mode;
            private ParkingRulesConfigDefinition _clipboardRule;
            private bool _hasClipboardRule;

            public ushort BuildingId => _buildingId;
            public bool IsDirty => _isDirty;
            public bool IsUpdatingUi => _isUpdatingUi;
            public bool HasUnappliedChanges => _isDirty;
            public bool RestrictionsEnabled => _restrictionsEnabled;
            public bool HasStoredRule => _hasStoredRule;
            public ParkingRulesConfigDefinition BaselineRule => _baselineRule;
            public bool IsPrefabSupported => _isPrefabSupported;
            public float NextParkingStatsUpdateTime => _nextParkingStatsUpdateTime;
            public PanelMode Mode => _mode;
            public bool HasClipboardRule => _hasClipboardRule;
            public ParkingRulesConfigDefinition ClipboardRule => _clipboardRule;

            public void BindBuilding(ushort buildingId)
            {
                _buildingId = buildingId;
                _isDirty = false;
                _isUpdatingUi = false;
                _restrictionsEnabled = false;
                _hasStoredRule = false;
                _baselineRule = default;
                _nextParkingStatsUpdateTime = 0f;
                _mode = buildingId == 0 ? PanelMode.Disabled : PanelMode.Viewing;
            }

            public void SetPrefabSupported(bool supported)
            {
                _isPrefabSupported = supported;
            }

            public void BeginUiSync()
            {
                _isUpdatingUi = true;
            }

            public void EndUiSync()
            {
                _isUpdatingUi = false;
            }

            public bool IsReadyForStatsUpdate(float currentTime)
            {
                return _buildingId != 0 && currentTime >= _nextParkingStatsUpdateTime;
            }

            public void ScheduleNextStatsUpdate(float nextTime)
            {
                _nextParkingStatsUpdateTime = nextTime;
            }

            public void SetBaselineRule(ParkingRulesConfigDefinition rule, bool restrictionsEnabled, bool hasStoredRule)
            {
                _baselineRule = rule;
                _restrictionsEnabled = restrictionsEnabled;
                _hasStoredRule = hasStoredRule;
                ClearDirty();
            }

            public void StoreClipboardRule(ParkingRulesConfigDefinition rule)
            {
                _clipboardRule = rule;
                _hasClipboardRule = true;
            }

            public void ClearClipboardRule()
            {
                _hasClipboardRule = false;
            }

            public void EnableRestrictions()
            {
                _restrictionsEnabled = true;
                if (_mode == PanelMode.Disabled)
                    _mode = PanelMode.Viewing;
            }

            public void DisableRestrictions()
            {
                _restrictionsEnabled = false;
                _hasStoredRule = false;
                ClearDirty();
                _mode = PanelMode.Disabled;
            }

            public void MarkDirty()
            {
                if (!_restrictionsEnabled)
                    return;

                _isDirty = true;
                _mode = PanelMode.Editing;
            }

            public void ClearDirty()
            {
                _isDirty = false;
                if (!_restrictionsEnabled)
                    _mode = PanelMode.Disabled;
                else if (_mode != PanelMode.Applying)
                    _mode = PanelMode.Viewing;
            }

            public void BeginApplying()
            {
                _mode = PanelMode.Applying;
                _isDirty = false;
            }

            public void CommitApplied(ParkingRulesConfigDefinition baselineRule)
            {
                _baselineRule = baselineRule;
                _hasStoredRule = true;
                _isDirty = false;
                _mode = _restrictionsEnabled ? PanelMode.Viewing : PanelMode.Disabled;
            }
        }
    }
}










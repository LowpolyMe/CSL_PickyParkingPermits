using System;
using ICities;
using PickyParking.Logging;
using PickyParking.ModEntry;
using PickyParking.Settings;

namespace PickyParking.UI
{
    internal static class LoggingOptions
    {
        public static void Build(UIHelperBase helper, ModSettings settings, Action saveSettings)
        {
            helper.AddCheckbox("Verbose logging", settings.EnableVerboseLogging, isChecked =>
            {
                HandleVerboseLoggingChanged(isChecked, settings, saveSettings);
            });

            UIHelperBase debugGroup = helper.AddGroup("Debug logging (requires verbose)");
            debugGroup.AddCheckbox("Parking search episodes", settings.EnableDebugParkingSearchEpisodes, isChecked =>
            {
                settings.EnableDebugParkingSearchEpisodes = isChecked;
                HandleDebugLoggingChanged("OptionsUI: Parking search episodes", settings, saveSettings);
            });
            debugGroup.AddCheckbox("Game access parking spaces", settings.EnableDebugGameAccessLogs, isChecked =>
            {
                settings.EnableDebugGameAccessLogs = isChecked;
                HandleDebugLoggingChanged("OptionsUI: Game access parking spaces", settings, saveSettings);
            });
            debugGroup.AddCheckbox("Candidate blocker decisions", settings.EnableDebugCandidateBlockerLogs, isChecked =>
            {
                settings.EnableDebugCandidateBlockerLogs = isChecked;
                HandleDebugLoggingChanged("OptionsUI: Candidate blocker decisions", settings, saveSettings);
            });
            debugGroup.AddCheckbox("CreateParkedVehicle parking violations", settings.EnableDebugCreateParkedVehicleLogs, isChecked =>
            {
                settings.EnableDebugCreateParkedVehicleLogs = isChecked;
                HandleDebugLoggingChanged("OptionsUI: CreateParkedVehicle logging", settings, saveSettings);
            });
            debugGroup.AddCheckbox("Building-specific debug logs", settings.EnableDebugBuildingLogs, isChecked =>
            {
                settings.EnableDebugBuildingLogs = isChecked;
                HandleDebugLoggingChanged("OptionsUI: Building debug logs", settings, saveSettings);
            });
            debugGroup.AddTextfield(
                "Building id for debug logs",
                settings.DebugBuildingId.ToString(),
                _ => { },
                text =>
                {
                    if (!ushort.TryParse(text, out var buildingId))
                    {
                        Log.Warn("[Settings] Invalid building id for debug logs: " + (text ?? "NULL"));
                        return;
                    }

                    settings.DebugBuildingId = buildingId;
                    HandleDebugLoggingChanged("OptionsUI: Building debug id", settings, saveSettings);
                });
            debugGroup.AddCheckbox("UI diagnostics", settings.EnableDebugUiLogs, isChecked =>
            {
                settings.EnableDebugUiLogs = isChecked;
                HandleDebugLoggingChanged("OptionsUI: UI diagnostics", settings, saveSettings);
            });
            debugGroup.AddCheckbox("TMPE integration diagnostics", settings.EnableDebugTmpeLogs, isChecked =>
            {
                settings.EnableDebugTmpeLogs = isChecked;
                HandleDebugLoggingChanged("OptionsUI: TMPE diagnostics", settings, saveSettings);
            });
            debugGroup.AddCheckbox("Parking permission evaluation", settings.EnableDebugPermissionEvaluatorLogs, isChecked =>
            {
                settings.EnableDebugPermissionEvaluatorLogs = isChecked;
                HandleDebugLoggingChanged("OptionsUI: Permission evaluation", settings, saveSettings);
            });
        }

        private static void HandleVerboseLoggingChanged(bool isChecked, ModSettings settings, Action saveSettings)
        {
            settings.EnableVerboseLogging = isChecked;
            SaveSettings(saveSettings);
            ReloadSettings("OptionsUI: Verbose logging");
            ModRuntime.ApplyLoggingSettings(settings);
            Log.Info(isChecked ? "[Settings] Verbose logging enabled." : "[Settings] Verbose logging disabled.");
        }

        private static void HandleDebugLoggingChanged(string reason, ModSettings settings, Action saveSettings)
        {
            SaveSettings(saveSettings);
            ReloadSettings(reason);
            ModRuntime.ApplyLoggingSettings(settings);
        }

        private static void SaveSettings(Action saveSettings)
        {
            if (saveSettings != null)
                saveSettings();
        }

        private static void ReloadSettings(string reason)
        {
            ModRuntime runtime = ModRuntime.Current;
            if (runtime == null || runtime.SettingsController == null || runtime.SettingsController.Current == null)
                return;

            runtime.SettingsController.Reload(reason);
        }
    }
}

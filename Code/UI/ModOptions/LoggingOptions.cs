using System;
using ICities;
using PickyParking.Logging;
using PickyParking.Settings;

namespace PickyParking.UI
{
    internal static class LoggingOptions
    {
        public static void Build(UIHelperBase helper, ModSettings settings, Action saveSettings, UiServices services)
        {
            helper.AddCheckbox("Verbose logging", settings.EnableVerboseLogging, isChecked =>
            {
                HandleVerboseLoggingChanged(isChecked, settings, saveSettings, services);
            });

            UIHelperBase debugGroup = helper.AddGroup("Debug logging (requires verbose)");
            debugGroup.AddCheckbox("Parking search episodes", settings.EnableDebugParkingSearchEpisodes, isChecked =>
            {
                settings.EnableDebugParkingSearchEpisodes = isChecked;
                HandleDebugLoggingChanged("OptionsUI: Parking search episodes", settings, saveSettings, services);
            });
            debugGroup.AddCheckbox("Game access parking spaces", settings.EnableDebugGameAccessLogs, isChecked =>
            {
                settings.EnableDebugGameAccessLogs = isChecked;
                HandleDebugLoggingChanged("OptionsUI: Game access parking spaces", settings, saveSettings, services);
            });
            debugGroup.AddCheckbox("Candidate blocker decisions", settings.EnableDebugCandidateBlockerLogs, isChecked =>
            {
                settings.EnableDebugCandidateBlockerLogs = isChecked;
                HandleDebugLoggingChanged("OptionsUI: Candidate blocker decisions", settings, saveSettings, services);
            });
            debugGroup.AddCheckbox("CreateParkedVehicle parking violations", settings.EnableDebugCreateParkedVehicleLogs, isChecked =>
            {
                settings.EnableDebugCreateParkedVehicleLogs = isChecked;
                HandleDebugLoggingChanged("OptionsUI: CreateParkedVehicle logging", settings, saveSettings, services);
            });
            debugGroup.AddCheckbox("Building-specific debug logs", settings.EnableDebugBuildingLogs, isChecked =>
            {
                settings.EnableDebugBuildingLogs = isChecked;
                HandleDebugLoggingChanged("OptionsUI: Building debug logs", settings, saveSettings, services);
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
                    HandleDebugLoggingChanged("OptionsUI: Building debug id", settings, saveSettings, services);
                });
            debugGroup.AddCheckbox("UI diagnostics", settings.EnableDebugUiLogs, isChecked =>
            {
                settings.EnableDebugUiLogs = isChecked;
                HandleDebugLoggingChanged("OptionsUI: UI diagnostics", settings, saveSettings, services);
            });
            debugGroup.AddCheckbox("TMPE integration diagnostics", settings.EnableDebugTmpeLogs, isChecked =>
            {
                settings.EnableDebugTmpeLogs = isChecked;
                HandleDebugLoggingChanged("OptionsUI: TMPE diagnostics", settings, saveSettings, services);
            });
            debugGroup.AddCheckbox("Parking permission evaluation", settings.EnableDebugPermissionEvaluatorLogs, isChecked =>
            {
                settings.EnableDebugPermissionEvaluatorLogs = isChecked;
                HandleDebugLoggingChanged("OptionsUI: Permission evaluation", settings, saveSettings, services);
            });
        }

        private static void HandleVerboseLoggingChanged(bool isChecked, ModSettings settings, Action saveSettings, UiServices services)
        {
            settings.EnableVerboseLogging = isChecked;
            SaveSettings(saveSettings);
            ReloadSettings("OptionsUI: Verbose logging", services);
            ApplyLoggingSettings(settings, services);
            Log.Info(isChecked ? "[Settings] Verbose logging enabled." : "[Settings] Verbose logging disabled.");
        }

        private static void HandleDebugLoggingChanged(string reason, ModSettings settings, Action saveSettings, UiServices services)
        {
            SaveSettings(saveSettings);
            ReloadSettings(reason, services);
            ApplyLoggingSettings(settings, services);
        }

        private static void SaveSettings(Action saveSettings)
        {
            if (saveSettings != null)
                saveSettings();
        }

        private static void ReloadSettings(string reason, UiServices services)
        {
            if (services == null)
                return;
            services.ReloadSettings(reason);
        }

        private static void ApplyLoggingSettings(ModSettings settings, UiServices services)
        {
            if (services == null)
                return;
            services.ApplyLoggingSettings(settings);
        }
    }
}

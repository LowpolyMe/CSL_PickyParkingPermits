using System;
using ICities;
using PickyParking.Logging;
using PickyParking.Settings;

namespace PickyParking.UI.ModOptions
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
            debugGroup.AddCheckbox("Rule + UI diagnostics", settings.EnableDebugRuleUiLogs, isChecked =>
            {
                settings.EnableDebugRuleUiLogs = isChecked;
                HandleDebugLoggingChanged("OptionsUI: Rule + UI diagnostics", settings, saveSettings, services);
            });
            debugGroup.AddCheckbox("Lot inspection diagnostics", settings.EnableDebugLotInspectionLogs, isChecked =>
            {
                settings.EnableDebugLotInspectionLogs = isChecked;
                HandleDebugLoggingChanged("OptionsUI: Lot inspection diagnostics", settings, saveSettings, services);
            });
            debugGroup.AddCheckbox("Decision pipeline diagnostics", settings.EnableDebugDecisionPipelineLogs, isChecked =>
            {
                settings.EnableDebugDecisionPipelineLogs = isChecked;
                HandleDebugLoggingChanged("OptionsUI: Decision pipeline diagnostics", settings, saveSettings, services);
            });
            debugGroup.AddCheckbox("Enforcement + cleanup diagnostics", settings.EnableDebugEnforcementLogs, isChecked =>
            {
                settings.EnableDebugEnforcementLogs = isChecked;
                HandleDebugLoggingChanged("OptionsUI: Enforcement + cleanup diagnostics", settings, saveSettings, services);
            });
            debugGroup.AddCheckbox("TMPE integration diagnostics", settings.EnableDebugTmpeLogs, isChecked =>
            {
                settings.EnableDebugTmpeLogs = isChecked;
                HandleDebugLoggingChanged("OptionsUI: TMPE diagnostics", settings, saveSettings, services);
            });
            debugGroup.AddTextfield(
                "Building id for lot inspection logs",
                settings.DebugBuildingId.ToString(),
                _ => { },
                text =>
                {
                    if (!ushort.TryParse(text, out var buildingId))
                    {
                        Log.Warn("[Settings] Invalid building id for lot inspection logs: " + (text ?? "NULL"));
                        return;
                    }

                    settings.DebugBuildingId = buildingId;
                    HandleDebugLoggingChanged("OptionsUI: Lot inspection building id", settings, saveSettings, services);
            });

            UIHelperBase overridesGroup = helper.AddGroup("Debug overrides (changes behavior)");
            overridesGroup.AddCheckbox("Disable parking enforcement", settings.DisableParkingEnforcement, isChecked =>
            {
                settings.DisableParkingEnforcement = isChecked;
                HandleDebugLoggingChanged("OptionsUI: Disable parking enforcement", settings, saveSettings, services);
            });
            overridesGroup.AddCheckbox("Disable TMPE candidate blocking", settings.DisableTMPECandidateBlocking, isChecked =>
            {
                settings.DisableTMPECandidateBlocking = isChecked;
                HandleDebugLoggingChanged("OptionsUI: Disable TMPE candidate blocking", settings, saveSettings, services);
            });
            overridesGroup.AddCheckbox("Disable clear known location on denial", settings.DisableClearKnownParkingOnDenied, isChecked =>
            {
                settings.DisableClearKnownParkingOnDenied = isChecked;
                HandleDebugLoggingChanged("OptionsUI: Disable clear known location on denial", settings, saveSettings, services);
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


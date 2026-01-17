using System;
using ICities;
using PickyParking.Logging;
using PickyParking.ModEntry;
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
            
            BuildLoggingGroup(helper, settings, saveSettings, services);
            BuildBehaviourOverridesGroup(helper, settings, saveSettings, services);
        }

        private static void BuildBehaviourOverridesGroup(UIHelperBase helper, ModSettings settings, Action saveSettings,
            UiServices services)
        {
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

        private static void BuildLoggingGroup(UIHelperBase helper, ModSettings settings, Action saveSettings,
            UiServices services)
        {
            
            UIHelperBase debugGroup = helper.AddGroup("Detailed feature logging");
            foreach (DebugLogCategory category in (DebugLogCategory[])System.Enum.GetValues(typeof(DebugLogCategory)))
            {                
                if (!IsSingleFlag(category))
                    continue;

                string label = category.ToString();
                var isChecked = (settings.EnabledDebugLogCategories & category) != 0;
                
                debugGroup.AddCheckbox(label, isChecked, isNowChecked =>
                {
                    settings.EnabledDebugLogCategories = SetFlag(settings.EnabledDebugLogCategories, category, isNowChecked);
                    HandleDebugLoggingChanged("OptionsUI: Detailed feature logging - " + label, settings, saveSettings, services);
                });
            }
//KEPT AS FALLBACK UNTIL WE ARE SURE REWORK WORKS
            /*debugGroup.AddCheckbox("Rule + UI diagnostics", settings.IsDebugLogCategoryEnabled(DebugLogCategory.RuleUi), isChecked =>
            {
                settings.EnabledDebugLogCategories = SetFlag(settings.EnabledDebugLogCategories, DebugLogCategory.RuleUi, isChecked);
                HandleDebugLoggingChanged("OptionsUI: Rule + UI diagnostics", settings, saveSettings, services);
            });
            debugGroup.AddCheckbox("Lot inspection diagnostics", settings.IsDebugLogCategoryEnabled(DebugLogCategory.LotInspection), isChecked =>
            {
                settings.EnabledDebugLogCategories = SetFlag(settings.EnabledDebugLogCategories, DebugLogCategory.LotInspection, isChecked);
                HandleDebugLoggingChanged("OptionsUI: Lot inspection diagnostics", settings, saveSettings, services);
            });
            debugGroup.AddCheckbox("Decision pipeline diagnostics", settings.IsDebugLogCategoryEnabled(DebugLogCategory.DecisionPipeline), isChecked =>
            {
                settings.EnabledDebugLogCategories = SetFlag(settings.EnabledDebugLogCategories, DebugLogCategory.DecisionPipeline, isChecked);
                HandleDebugLoggingChanged("OptionsUI: Decision pipeline diagnostics", settings, saveSettings, services);
            });
            debugGroup.AddCheckbox("Enforcement + cleanup diagnostics", settings.IsDebugLogCategoryEnabled(DebugLogCategory.Enforcement), isChecked =>
            {
                settings.EnabledDebugLogCategories = SetFlag(settings.EnabledDebugLogCategories, DebugLogCategory.Enforcement, isChecked);
                HandleDebugLoggingChanged("OptionsUI: Enforcement + cleanup diagnostics", settings, saveSettings, services);
            });
            debugGroup.AddCheckbox("TMPE integration diagnostics", settings.IsDebugLogCategoryEnabled(DebugLogCategory.Tmpe), isChecked =>
            {
                settings.EnabledDebugLogCategories = SetFlag(settings.EnabledDebugLogCategories, DebugLogCategory.Tmpe, isChecked);
                HandleDebugLoggingChanged("OptionsUI: TMPE diagnostics", settings, saveSettings, services);
            });*/
            debugGroup.AddTextfield(
                "Building id for lot inspection logs",
                settings.DebugBuildingId.ToString(),
                _ => { },
                text =>
                {
                    if (!ushort.TryParse(text, out var buildingId))
                    {
                        Log.Warn(DebugLogCategory.None, "[Settings] Invalid building id for lot inspection logs: " + (text ?? "NULL"));
                        return;
                    }

                    settings.DebugBuildingId = buildingId;
                    HandleDebugLoggingChanged("OptionsUI: Lot inspection building id", settings, saveSettings, services);
                });
        }

        private static bool IsSingleFlag(DebugLogCategory category)
        {
            int v = (int)category;
            return v != 0 && (v & (v - 1)) == 0;
        }

        private static void HandleVerboseLoggingChanged(bool isChecked, ModSettings settings, Action saveSettings, UiServices services)
        {
            settings.EnableVerboseLogging = isChecked;
            SaveSettings(saveSettings);
            ReloadSettings("OptionsUI: Verbose logging", services);
            ApplyLoggingSettings(settings, services);
            Log.Info(DebugLogCategory.RuleUi, isChecked ? "[Settings] Verbose logging enabled." : "[Settings] Verbose logging disabled.");
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

        private static DebugLogCategory SetFlag(DebugLogCategory current, DebugLogCategory flag, bool enabled)
        {
            if (enabled)
                return current | flag;

            return current & ~flag;
        }
    }
}


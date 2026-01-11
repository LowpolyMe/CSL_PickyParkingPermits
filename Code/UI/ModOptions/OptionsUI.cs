using System;
using ColossalFramework.UI;
using ICities;
using PickyParking.Settings;

namespace PickyParking.UI.ModOptions
{
    public static class OptionsUI
    {
        public static void Build(UIHelperBase helper, ModSettings settings, Action saveSettings, UiServices services)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            if (services != null)
                services.ApplyLoggingSettings(settings);

            CustomizationOptions.Build(helper, settings, saveSettings, services);
            CreateSupportedPrefabList(helper, settings, saveSettings, services);
            LoggingOptions.Build(helper, settings, saveSettings, services);
            BuildResetOptions(helper, services);
        }

        private static void CreateSupportedPrefabList(UIHelperBase helper, ModSettings settings, Action saveSettings, UiServices services)
        {
            UIHelperBase supportedGroup = helper.AddGroup("Supported parking prefabs");
            var helperPanel = supportedGroup as UIHelper;
            if (helperPanel == null)
                return;

            var groupPanel = helperPanel.self as UIPanel;
            if (groupPanel == null)
                return;

            var listPanel = groupPanel.AddUIComponent<SupportedPrefabListPanel>();
            listPanel.Initialize(services);
            listPanel.Bind(settings, saveSettings);
        }

        private static void BuildResetOptions(UIHelperBase helper, UiServices services)
        {
            UIHelperBase resetGroup = helper.AddGroup("Settings reset");
            resetGroup.AddButton("Reset settings (delete file)", () =>
            {
                var storage = new ModSettingsStorage();
                storage.ResetToDefaults();
                if (services != null)
                {
                    services.ReloadSettings("OptionsUI: Reset settings");
                    if (services.Settings != null)
                        services.ApplyLoggingSettings(services.Settings);
                }
            });
        }
    }
}


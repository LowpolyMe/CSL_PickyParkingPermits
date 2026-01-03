using System;
using ColossalFramework.UI;
using ICities;
using PickyParking.ModEntry;
using PickyParking.Settings;

namespace PickyParking.UI
{
    public static class OptionsUI
    {
        public static void Build(UIHelperBase helper, ModSettings settings, Action saveSettings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            ModRuntime.ApplyLoggingSettings(settings);

            CustomizationOptions.Build(helper, settings, saveSettings);
            CreateSupportedPrefabList(helper, settings, saveSettings);
            LoggingOptions.Build(helper, settings, saveSettings);
        }

        private static void CreateSupportedPrefabList(UIHelperBase helper, ModSettings settings, Action saveSettings)
        {
            UIHelperBase supportedGroup = helper.AddGroup("Supported parking prefabs");
            var helperPanel = supportedGroup as UIHelper;
            if (helperPanel == null)
                return;

            var groupPanel = helperPanel.self as UIPanel;
            if (groupPanel == null)
                return;

            var listPanel = groupPanel.AddUIComponent<SupportedPrefabListPanel>();
            listPanel.Bind(settings, saveSettings);
        }
    }
}

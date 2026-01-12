using System;
using ColossalFramework.UI;
using ICities;
using PickyParking.Settings;

namespace PickyParking.UI.ModOptions
{
    internal static class ReevaluationOptions
    {
        public static void Build(UIHelperBase helper, ModSettings settings, Action saveSettings, UiServices services)
        {
            UIHelperBase group = helper.AddGroup("Parking rule sweeps");
            AddInfoLabel(group, "Sweeps re-check parked cars on lots with rules once per in-game day and move or release invalid cars. This costs CPU; disable to reduce impact.");

            group.AddCheckbox("Enable background sweeps", settings.EnableParkingRuleSweeps, isChecked =>
            {
                settings.EnableParkingRuleSweeps = isChecked;
                SaveSettings(saveSettings);
                ReloadSettings("OptionsUI: Enable background sweeps", services);
            });
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

        private static void AddInfoLabel(UIHelperBase group, string text)
        {
            var helperPanel = group as UIHelper;
            if (helperPanel == null)
                return;

            var panel = helperPanel.self as UIPanel;
            if (panel == null)
                return;

            UILabel label = panel.AddUIComponent<UILabel>();
            label.text = text;
            label.autoSize = false;
            label.autoHeight = true;
            label.wordWrap = true;
            label.textScale = 0.8f;
            float width = panel.width - 20f;
            if (width < 100f)
                width = ModOptionsUiValues.OptionsPanel.DefaultWidth - 20f;
            label.width = width;
        }

    }
}

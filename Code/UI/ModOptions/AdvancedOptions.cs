using System;
using ColossalFramework.UI;
using ICities;
using PickyParking.Logging;
using PickyParking.Settings;
using UnityEngine;

namespace PickyParking.UI.ModOptions
{
    internal static class AdvancedOptions
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
            

            AddInfoLabel(group, "Reevaluation limits apply per simulation tick. Lower values smooth CPU spikes but take longer to process.");

            AddIntField(
                group,
                "Max evaluations per tick. Recommended 64-256",
                settings.ReevaluationMaxEvaluationsPerTick,
                Mathf.RoundToInt(ModOptionsUiValues.ReevaluationSliders.MaxEvaluationsMin),
                Mathf.RoundToInt(ModOptionsUiValues.ReevaluationSliders.MaxEvaluationsMax),
                value =>
                {
                    settings.ReevaluationMaxEvaluationsPerTick = value;
                    SaveSettings(saveSettings);
                    ReloadSettings("OptionsUI: Max evaluations per tick", services);
                });

            AddIntField(
                group,
                "Max relocations per tick. Recommended 8-32.",
                settings.ReevaluationMaxRelocationsPerTick,
                Mathf.RoundToInt(ModOptionsUiValues.ReevaluationSliders.MaxRelocationsMin),
                Mathf.RoundToInt(ModOptionsUiValues.ReevaluationSliders.MaxRelocationsMax),
                value =>
                {
                    settings.ReevaluationMaxRelocationsPerTick = value;
                    SaveSettings(saveSettings);
                    ReloadSettings("OptionsUI: Max relocations per tick", services);
                });
            
            group.AddSpace(10);
            
            AddInfoLabel(group, "Attempts to fix rare invisible parked vehicles stuck in a parking-in-progress state by finalizing them if they persist across two daily sweeps.");

            group.AddCheckbox("Fix stuck owned parked vehicles", settings.EnableStuckParkedVehicleFix, isChecked =>
            {
                settings.EnableStuckParkedVehicleFix = isChecked;
                SaveSettings(saveSettings);
                ReloadSettings("OptionsUI: Fix stuck owned parked vehicles", services);
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
            label.processMarkup = true;
            label.autoSize = false;
            label.autoHeight = true;
            label.wordWrap = true;
            label.textScale = 0.8f;
            label.text = "<i>" + text + "</i>";
            float width = panel.width - 20f;
            if (width < 100f)
                width = ModOptionsUiValues.OptionsPanel.DefaultWidth - 20f;
            label.width = width;
        }

        private static void AddIntField(
            UIHelperBase group,
            string label,
            int initialValue,
            int min,
            int max,
            Action<int> onChanged)
        {
            int lastValid = initialValue;
            UITextField field = null;
            object fieldObj = group.AddTextfield(label, initialValue.ToString(), _ => { }, text =>
            {
                if (!int.TryParse(text, out var value))
                {
                    Log.Warn("[Settings] Invalid value for " + label + ": " + (text ?? "NULL"));
                    if (field != null)
                        field.text = lastValid.ToString();
                    return;
                }

                int clamped = Mathf.Clamp(value, min, max);
                lastValid = clamped;
                if (field != null)
                    field.text = clamped.ToString();
                if (onChanged != null)
                    onChanged(clamped);
            });

            field = fieldObj as UITextField;
        }

    }
}

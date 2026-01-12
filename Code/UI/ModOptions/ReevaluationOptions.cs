using System;
using ColossalFramework.UI;
using ICities;
using PickyParking.Logging;
using PickyParking.Settings;
using UnityEngine;

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

            AddInfoLabel(group, "Reevaluation limits apply per simulation tick. Lower values smooth CPU spikes but take longer to process.");

            AddIntField(
                group,
                "Max evaluations per tick",
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
                "Max relocations per tick",
                settings.ReevaluationMaxRelocationsPerTick,
                Mathf.RoundToInt(ModOptionsUiValues.ReevaluationSliders.MaxRelocationsMin),
                Mathf.RoundToInt(ModOptionsUiValues.ReevaluationSliders.MaxRelocationsMax),
                value =>
                {
                    settings.ReevaluationMaxRelocationsPerTick = value;
                    SaveSettings(saveSettings);
                    ReloadSettings("OptionsUI: Max relocations per tick", services);
                });

            AddInfoLabel(group, "Buildings per day = 0 resets the sweep daily. Any value > 0 spreads sweeps across days.");

            AddIntField(
                group,
                "Buildings processed per day",
                settings.ReevaluationSweepBuildingsPerDay,
                Mathf.RoundToInt(ModOptionsUiValues.ReevaluationSliders.BuildingsPerDayMin),
                Mathf.RoundToInt(ModOptionsUiValues.ReevaluationSliders.BuildingsPerDayMax),
                value =>
                {
                    settings.ReevaluationSweepBuildingsPerDay = value;
                    SaveSettings(saveSettings);
                    ReloadSettings("OptionsUI: Buildings processed per day", services);
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

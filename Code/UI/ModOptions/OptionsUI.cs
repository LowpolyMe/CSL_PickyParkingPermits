using System;
using ICities;
using ColossalFramework.UI;
using PickyParking.Logging;
using PickyParking.UI.ModResources;
using PickyParking.Settings;
using PickyParking.Features.ParkingPolicing;
using PickyParking.ModEntry;
using UnityEngine;

namespace PickyParking.UI
{
    
    
    
    public static class OptionsUI
    {
        public static void Build(UIHelperBase helper, ModSettings settings, Action saveSettings)
        {
            if (settings == null) throw new ArgumentNullException("settings");

            ModRuntime.ApplyLoggingSettings(settings);

            UIHelperBase overlayGroup = helper.AddGroup("Overlay Colors");
            Texture2D hueTexture = ModResourceLoader.LoadTexture("HueGradient.png");

            CreateHueSlider(
                overlayGroup,
                "Resident radius hue",
                settings.ResidentsRadiusHue,
                value =>
                {
                    settings.ResidentsRadiusHue = value;
                    if (saveSettings != null) saveSettings();
                    ReloadSettings("OptionsUI: Resident radius hue");
                },
                hueTexture);

            CreateHueSlider(
                overlayGroup,
                "Work/school radius hue",
                settings.WorkSchoolRadiusHue,
                value =>
                {
                    settings.WorkSchoolRadiusHue = value;
                    if (saveSettings != null) saveSettings();
                    ReloadSettings("OptionsUI: Work/school radius hue");
                },
                hueTexture);

            CreateSupportedPrefabList(helper, settings, saveSettings);

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

        private static void HandleVerboseLoggingChanged(bool isChecked, ModSettings settings, Action saveSettings)
        {
            settings.EnableVerboseLogging = isChecked;
            if (saveSettings != null)
                saveSettings();
            ReloadSettings("OptionsUI: Verbose logging");
            ModRuntime.ApplyLoggingSettings(settings);
            Log.Info(isChecked ? "[Settings] Verbose logging enabled." : "[Settings] Verbose logging disabled.");
        }

        private static void HandleDebugLoggingChanged(string reason, ModSettings settings, Action saveSettings)
        {
            if (saveSettings != null)
                saveSettings();
            ReloadSettings(reason);
            ModRuntime.ApplyLoggingSettings(settings);
        }

        private static void CreateHueSlider(
            UIHelperBase group,
            string label,
            float initialHue,
            OnValueChanged onChanged,
            Texture2D backgroundTexture)
        {
            object sliderObj = group.AddSlider(
                label,
                ModOptionsUiValues.HueSliders.Min,
                ModOptionsUiValues.HueSliders.Max,
                ModOptionsUiValues.HueSliders.Step,
                initialHue,
                onChanged);
            var slider = sliderObj as UISlider;
            if (slider == null)
                return;

            slider.backgroundSprite = string.Empty;
            slider.color = Color.white;

            if (backgroundTexture == null)
                return;

            slider.clipChildren = true;
            var hueBar = slider.AddUIComponent<UITextureSprite>();
            hueBar.texture = backgroundTexture;
            hueBar.size = slider.size;
            hueBar.relativePosition = Vector3.zero;
            hueBar.zOrder = 0;

            if (slider.thumbObject != null)
                slider.thumbObject.zOrder = hueBar.zOrder + 1;
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

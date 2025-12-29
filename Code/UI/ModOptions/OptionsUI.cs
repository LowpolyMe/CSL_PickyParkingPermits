using System;
using ICities;
using ColossalFramework.UI;
using PickyParking.Infrastructure;
using PickyParking.Infrastructure.Integration;
using PickyParking.ModEntry;
using UnityEngine;

namespace PickyParking.Settings
{
    
    
    
    public static class OptionsUI
    {
        public static void Build(UIHelperBase helper, ModSettings settings, Action saveSettings)
        {
            if (settings == null) throw new ArgumentNullException("settings");

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

            helper.AddCheckbox("Verbose logging", settings.EnableVerboseLogging, isChecked =>
            {
                HandleVerboseLoggingChanged(isChecked, settings, saveSettings);
            });

            
            
        }

        private static void HandleVerboseLoggingChanged(bool isChecked, ModSettings settings, Action saveSettings)
        {
            settings.EnableVerboseLogging = isChecked;
            if (saveSettings != null)
                saveSettings();
            ReloadSettings("OptionsUI: Verbose logging");
            Log.SetVerboseEnabled(isChecked);
            ParkingSearchContext.EnableEpisodeLogs = isChecked;
            Log.Info(isChecked ? "[Settings] Verbose logging enabled." : "[Settings] Verbose logging disabled.");
        }

        private static void CreateHueSlider(
            UIHelperBase group,
            string label,
            float initialHue,
            OnValueChanged onChanged,
            Texture2D backgroundTexture)
        {
            object sliderObj = group.AddSlider(label, 0f, 1f, 0.01f, initialHue, onChanged);
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

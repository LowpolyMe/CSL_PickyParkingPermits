using System;
using ColossalFramework.UI;
using ICities;
using PickyParking.Settings;
using PickyParking.UI.ModResources;
using UnityEngine;

namespace PickyParking.UI
{
    internal static class CustomizationOptions
    {
        public static void Build(UIHelperBase helper, ModSettings settings, Action saveSettings, UiServices services)
        {
            UIHelperBase overlayGroup = helper.AddGroup("Overlay Colors");
            Texture2D hueTexture = ModResourceLoader.LoadTexture("HueGradient.png");

            CreateHueSlider(
                overlayGroup,
                "Resident radius hue",
                settings.ResidentsRadiusHue,
                value =>
                {
                    settings.ResidentsRadiusHue = value;
                    SaveSettings(saveSettings);
                    ReloadSettings("OptionsUI: Resident radius hue", services);
                },
                hueTexture);

            CreateHueSlider(
                overlayGroup,
                "Work/school radius hue",
                settings.WorkSchoolRadiusHue,
                value =>
                {
                    settings.WorkSchoolRadiusHue = value;
                    SaveSettings(saveSettings);
                    ReloadSettings("OptionsUI: Work/school radius hue", services);
                },
                hueTexture);
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
    }
}

using UnityEngine;
namespace PickyParking.UI
{
    internal sealed class ParkingPanelTheme
    {
        private readonly UiServices _services;

        public ParkingPanelTheme(UiServices services)
        {
            _services = services;
        }
        public float RowHeight { get; } = 35f;
        public float HorizontalPadding { get; } = 10f;
        public float VerticalPadding { get; } = 2f;
        public float SliderHeight { get; } = 15f;
        public float MinIconSize { get; } = 18f;
        public float MinButtonHeight { get; } = 18f;
        public float MinSliderWidth { get; } = 10f;
        public float SliderThumbSize { get; } = 16f;
        public float IconScale { get; } = 0.72f;
        public float ValueLabelWidth { get; } = 44f;
        public float ValueLabelHeight { get; } = 15f;
        public float HeaderTextScale { get; } = 1f;
        public float ParkingStatsTextScale { get; } = 0.75f;
        public float ToggleTextScale { get; } = 0.7f;
        public float ValueLabelTextScale { get; } = 0.7f;
        public float RestrictionsToggleTextScale { get; } = 0.85f;
        public float ApplyButtonTextScale { get; } = 1f;
        public float PanelRowCount { get; } = 7f;
        public float PanelExtraHeight { get; } = 30f;
        public float EnabledOpacity { get; } = 1f;
        public float DisabledOpacity { get; } = 0.7f;
        public float DefaultResidentsHue { get; } = 0.35f;
        public float DefaultWorkSchoolHue { get; } = 0.1f;
        public Color ThumbColor { get; } = Color.white;
        public Color32 EnabledColor { get; } = new Color32(255, 255, 255, 255);
        public Color32 DisabledColor { get; } = new Color32(140, 140, 140, 255);
        public Color32 ValueLabelColor { get; } = new Color32(185, 221, 254, 255);
        public Color32 SliderTrackColor { get; } = new Color32(150, 150, 150, 255);

        public float RowPanelHeight => RowHeight + VerticalPadding * 2f;
        public float IconSize => Mathf.Max(MinIconSize, RowHeight);
        public Color32 ResidentsFillColor => GetResidentsFillColor();
        public Color32 WorkSchoolFillColor => GetWorkSchoolFillColor();

        private Color32 GetResidentsFillColor()
        {
            float hue = DefaultResidentsHue;
            if (_services != null && _services.Settings != null)
                hue = _services.Settings.ResidentsRadiusHue;
            return ColorConversion.FromHue(hue);
        }

        private Color32 GetWorkSchoolFillColor()
        {
            float hue = DefaultWorkSchoolHue;
            if (_services != null && _services.Settings != null)
                hue = _services.Settings.WorkSchoolRadiusHue;
            return ColorConversion.FromHue(hue);
        }
    }
}

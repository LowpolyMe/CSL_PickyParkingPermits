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
        public float RowHeight => ConfigPanelUiValues.PanelTheme.RowHeight;
        public float HorizontalPadding => ConfigPanelUiValues.PanelTheme.HorizontalPadding;
        public float VerticalPadding => ConfigPanelUiValues.PanelTheme.VerticalPadding;
        public float SliderHeight => ConfigPanelUiValues.PanelTheme.SliderHeight;
        public float MinIconSize => ConfigPanelUiValues.PanelTheme.MinIconSize;
        public float MinButtonHeight => ConfigPanelUiValues.PanelTheme.MinButtonHeight;
        public float MinSliderWidth => ConfigPanelUiValues.PanelTheme.MinSliderWidth;
        public float SliderThumbSize => ConfigPanelUiValues.PanelTheme.SliderThumbSize;
        public float IconScale => ConfigPanelUiValues.PanelTheme.IconScale;
        public float ValueLabelWidth => ConfigPanelUiValues.PanelTheme.ValueLabelWidth;
        public float ValueLabelHeight => ConfigPanelUiValues.PanelTheme.ValueLabelHeight;
        public float HeaderTextScale => ConfigPanelUiValues.PanelTheme.HeaderTextScale;
        public float ParkingStatsTextScale => ConfigPanelUiValues.PanelTheme.ParkingStatsTextScale;
        public float ToggleTextScale => ConfigPanelUiValues.PanelTheme.ToggleTextScale;
        public float ValueLabelTextScale => ConfigPanelUiValues.PanelTheme.ValueLabelTextScale;
        public float RestrictionsToggleTextScale => ConfigPanelUiValues.PanelTheme.RestrictionsToggleTextScale;
        public float ApplyButtonTextScale => ConfigPanelUiValues.PanelTheme.ApplyButtonTextScale;
        public float PanelRowCount => ConfigPanelUiValues.PanelTheme.PanelRowCount;
        public float PanelExtraHeight => ConfigPanelUiValues.PanelTheme.PanelExtraHeight;
        public float EnabledOpacity => ConfigPanelUiValues.PanelTheme.EnabledOpacity;
        public float DisabledOpacity => ConfigPanelUiValues.PanelTheme.DisabledOpacity;
        public float DefaultResidentsHue => ConfigPanelUiValues.PanelTheme.DefaultResidentsHue;
        public float DefaultWorkSchoolHue => ConfigPanelUiValues.PanelTheme.DefaultWorkSchoolHue;
        public Color ThumbColor => ConfigPanelUiValues.PanelTheme.ThumbColor;
        public Color32 EnabledColor => ConfigPanelUiValues.PanelTheme.EnabledColor;
        public Color32 DisabledColor => ConfigPanelUiValues.PanelTheme.DisabledColor;
        public Color32 ValueLabelColor => ConfigPanelUiValues.PanelTheme.ValueLabelColor;
        public Color32 SliderTrackColor => ConfigPanelUiValues.PanelTheme.SliderTrackColor;

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

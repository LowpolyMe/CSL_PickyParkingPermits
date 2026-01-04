using UnityEngine;
namespace PickyParking.UI.BuildingOptionsPanel
{
    internal sealed class ParkingPanelTheme
    {
        private readonly UiServices _services;

        public ParkingPanelTheme(UiServices services)
        {
            _services = services;
        }
        public float RowHeight => BuildingOptionsPanelUiValues.PanelTheme.RowHeight;
        public float HorizontalPadding => BuildingOptionsPanelUiValues.PanelTheme.HorizontalPadding;
        public float VerticalPadding => BuildingOptionsPanelUiValues.PanelTheme.VerticalPadding;
        public float SliderHeight => BuildingOptionsPanelUiValues.PanelTheme.SliderHeight;
        public float MinIconSize => BuildingOptionsPanelUiValues.PanelTheme.MinIconSize;
        public float MinButtonHeight => BuildingOptionsPanelUiValues.PanelTheme.MinButtonHeight;
        public float MinSliderWidth => BuildingOptionsPanelUiValues.PanelTheme.MinSliderWidth;
        public float SliderThumbSize => BuildingOptionsPanelUiValues.PanelTheme.SliderThumbSize;
        public float IconScale => BuildingOptionsPanelUiValues.PanelTheme.IconScale;
        public float ValueLabelWidth => BuildingOptionsPanelUiValues.PanelTheme.ValueLabelWidth;
        public float ValueLabelHeight => BuildingOptionsPanelUiValues.PanelTheme.ValueLabelHeight;
        public float HeaderTextScale => BuildingOptionsPanelUiValues.PanelTheme.HeaderTextScale;
        public float ParkingStatsTextScale => BuildingOptionsPanelUiValues.PanelTheme.ParkingStatsTextScale;
        public float ToggleTextScale => BuildingOptionsPanelUiValues.PanelTheme.ToggleTextScale;
        public float ValueLabelTextScale => BuildingOptionsPanelUiValues.PanelTheme.ValueLabelTextScale;
        public float RestrictionsToggleTextScale => BuildingOptionsPanelUiValues.PanelTheme.RestrictionsToggleTextScale;
        public float ApplyButtonTextScale => BuildingOptionsPanelUiValues.PanelTheme.ApplyButtonTextScale;
        public float PanelRowCount => BuildingOptionsPanelUiValues.PanelTheme.PanelRowCount;
        public float PanelExtraHeight => BuildingOptionsPanelUiValues.PanelTheme.PanelExtraHeight;
        public float EnabledOpacity => BuildingOptionsPanelUiValues.PanelTheme.EnabledOpacity;
        public float DisabledOpacity => BuildingOptionsPanelUiValues.PanelTheme.DisabledOpacity;
        public float DefaultResidentsHue => BuildingOptionsPanelUiValues.PanelTheme.DefaultResidentsHue;
        public float DefaultWorkSchoolHue => BuildingOptionsPanelUiValues.PanelTheme.DefaultWorkSchoolHue;
        public Color ThumbColor => BuildingOptionsPanelUiValues.PanelTheme.ThumbColor;
        public Color32 EnabledColor => BuildingOptionsPanelUiValues.PanelTheme.EnabledColor;
        public Color32 DisabledColor => BuildingOptionsPanelUiValues.PanelTheme.DisabledColor;
        public Color32 ValueLabelColor => BuildingOptionsPanelUiValues.PanelTheme.ValueLabelColor;
        public Color32 SliderTrackColor => BuildingOptionsPanelUiValues.PanelTheme.SliderTrackColor;

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







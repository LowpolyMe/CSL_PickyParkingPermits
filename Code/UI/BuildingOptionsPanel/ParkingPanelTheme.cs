using System;
using UnityEngine;
using ColossalFramework.UI;
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
        public float FooterIconEnabledAlpha => BuildingOptionsPanelUiValues.PanelTheme.FooterIconEnabledAlpha;
        public float FooterIconHoverAlpha => BuildingOptionsPanelUiValues.PanelTheme.FooterIconHoverAlpha;
        public float FooterIconPressedAlpha => BuildingOptionsPanelUiValues.PanelTheme.FooterIconPressedAlpha;
        public float FooterIconDisabledAlpha => BuildingOptionsPanelUiValues.PanelTheme.FooterIconDisabledAlpha;
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

        public UIPanel CreateRowContainer(UIPanel parent, string name, float? height = null)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));

            float rowHeight = height ?? RowPanelHeight;
            UIPanel row = parent.AddUIComponent<UIPanel>();
            row.name = name;
            row.width = parent.width;
            row.height = rowHeight;
            row.autoLayout = false;
            return row;
        }

        public UIButton CreateButton(UIPanel parent, bool useDefaultSprites, string text = "", string tooltip = "")
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));

            UIButton button = parent.AddUIComponent<UIButton>();
            if (!string.IsNullOrEmpty(text))
                button.text = text;

            if (!string.IsNullOrEmpty(tooltip))
                button.tooltip = tooltip;

            button.atlas = UIView.GetAView().defaultAtlas;

            if (useDefaultSprites)
            {
                button.normalBgSprite = BuildingOptionsPanelUiValues.PanelTheme.ButtonsNormalBgSprite;
                button.hoveredBgSprite = BuildingOptionsPanelUiValues.PanelTheme.ButtonsHoveredBgSprite;
                button.pressedBgSprite = BuildingOptionsPanelUiValues.PanelTheme.ButtonsPressedBgSprite;
                button.disabledBgSprite = BuildingOptionsPanelUiValues.PanelTheme.ButtonsDisabledBgSprite;
            }

            button.playAudioEvents = true;
            button.pressedColor = new Color32(210, 210, 210, 255);
            return button;
        }

        public UILabel CreateLabel(
            UIPanel parent,
            string text,
            Vector2 size,
            float textScale,
            Color32 textColor,
            UIHorizontalAlignment horizontalAlignment,
            UIVerticalAlignment verticalAlignment,
            Vector3 relativePosition,
            bool wordWrap = false,
            string backgroundSprite = null,
            Color32? backgroundColor = null)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));

            UILabel label = parent.AddUIComponent<UILabel>();
            label.text = text;
            label.autoSize = false;
            label.size = size;
            label.textScale = textScale;
            label.textColor = textColor;
            label.textAlignment = horizontalAlignment;
            label.verticalAlignment = verticalAlignment;
            label.relativePosition = relativePosition;
            label.wordWrap = wordWrap;

            if (!string.IsNullOrEmpty(backgroundSprite))
                label.backgroundSprite = backgroundSprite;

            if (backgroundColor.HasValue)
                label.color = backgroundColor.Value;

            return label;
        }

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







using System;
using UnityEngine;
using ColossalFramework.UI;
using PickyParking.Features.ParkingRules;
using PickyParking.Infrastructure;

namespace PickyParking.UI
{
    internal sealed class PickyParkingPanelVisuals
    {
        #region readonly
        private readonly ParkingRulesConfigPanel _panel;
        private readonly ParkingPanelTheme _theme;
        private readonly float _sliderMinValue;
        private readonly float _sliderMaxValue;
        private readonly float _sliderStep;
        private readonly Func<float> _getDefaultSliderValue;
        private readonly float _distanceSliderMinValue;
        private readonly float _distanceSliderMaxValue;
        private readonly ushort _minDistanceMeters;
        private readonly ushort _midDistanceMeters;
        private readonly ushort _maxDistanceMeters;
        private readonly float _distanceMidpointT;
        private readonly Action _onToggleRestrictions;
        private readonly Action<ParkingRulesSliderRow> _onToggleSlider;
        private readonly Action<ParkingRulesSliderRow, float> _onSliderValueChanged;
        private readonly Action _onToggleVisitors;
        private readonly Action _onApplyChanges;
#endregion

        public UIButton RestrictionsToggleButton { get; private set; }
        public ParkingRulesSliderRow ResidentsRow { get; private set; }
        public ParkingRulesSliderRow WorkSchoolRow { get; private set; }
        public ParkingRulesToggleRow VisitorsRow { get; private set; }
        public UIPanel FooterRow { get; private set; }

        public PickyParkingPanelVisuals(
            ParkingRulesConfigPanel panel,
            ParkingPanelTheme theme,
            float sliderMinValue,
            float sliderMaxValue,
            float sliderStep,
            Func<float> getDefaultSliderValue,
            float distanceSliderMinValue,
            float distanceSliderMaxValue,
            ushort minDistanceMeters,
            ushort midDistanceMeters,
            ushort maxDistanceMeters,
            float distanceMidpointT,
            Action onToggleRestrictions,
            Action<ParkingRulesSliderRow> onToggleSlider,
            Action<ParkingRulesSliderRow, float> onSliderValueChanged,
            Action onToggleVisitors,
            Action onApplyChanges)
        {
            _panel = panel;
            _theme = theme;
            _sliderMinValue = sliderMinValue;
            _sliderMaxValue = sliderMaxValue;
            _sliderStep = sliderStep;
            _getDefaultSliderValue = getDefaultSliderValue;
            _distanceSliderMinValue = distanceSliderMinValue;
            _distanceSliderMaxValue = distanceSliderMaxValue;
            _minDistanceMeters = minDistanceMeters;
            _midDistanceMeters = midDistanceMeters;
            _maxDistanceMeters = maxDistanceMeters;
            _distanceMidpointT = distanceMidpointT;
            _onToggleRestrictions = onToggleRestrictions;
            _onToggleSlider = onToggleSlider;
            _onSliderValueChanged = onSliderValueChanged;
            _onToggleVisitors = onToggleVisitors;
            _onApplyChanges = onApplyChanges;
        }

        public void BuildUi()
        {
            float rowHeight = _theme.RowHeight;
            float horizontalPadding = _theme.HorizontalPadding;
            float verticalPadding = _theme.VerticalPadding;
            float rowPanelHeight = _theme.RowPanelHeight;
            float iconSize = _theme.IconSize;
            float sliderHeight = _theme.SliderHeight;
            float sliderWidth = Mathf.Max(_theme.MinSliderWidth, _panel.width - (horizontalPadding * 3f + iconSize));
            Color32 residentsFillColor = _theme.ResidentsFillColor;
            Color32 workSchoolFillColor = _theme.WorkSchoolFillColor;

            SetupPanelLayout(rowPanelHeight);
            CreateHeader(rowPanelHeight, rowHeight, verticalPadding);
            CreateRestrictionsToggleRow(rowPanelHeight, rowHeight, horizontalPadding, verticalPadding);
            CreateRows(
                rowPanelHeight,
                rowHeight,
                horizontalPadding,
                verticalPadding,
                iconSize,
                sliderWidth,
                sliderHeight,
                residentsFillColor,
                workSchoolFillColor);
            CreateFooter(horizontalPadding, verticalPadding, rowPanelHeight);
        }

        public void ConfigurePanel()
        {
            _panel.name = "PickyParkingPanel";
            _panel.isVisible = false;

            if (_panel.parent != null)
                _panel.width = _panel.parent.width;
            else
                _panel.width = 300f;

            _panel.backgroundSprite = string.Empty;
        }

        public void UpdateSliderRowLabel(ParkingRulesSliderRow row)
        {
            float displayValue = row.IsEnabled ? row.Slider.value : GetRowDisplayValue(row);
            if (row.ValueLabel != null)
                row.ValueLabel.text = FormatDistanceDisplay(displayValue);
        }

        public void UpdateSliderRowVisuals(ParkingRulesSliderRow row)
        {
            if (row == ResidentsRow)
                row.FillColor = _theme.ResidentsFillColor;
            else if (row == WorkSchoolRow)
                row.FillColor = _theme.WorkSchoolFillColor;

            Color32 color = row.IsEnabled ? _theme.EnabledColor : _theme.DisabledColor;
            row.ToggleButton.color = color;
            row.ToggleButton.textColor = color;
            if (row.IconSprite != null)
                row.IconSprite.color = color;
            if (row.DisabledOverlay != null)
            {
                row.DisabledOverlay.color = _theme.DisabledColor;
                row.DisabledOverlay.isVisible = !row.IsEnabled;
            }
            if (row.FillSprite != null)
                row.FillSprite.color = row.IsEnabled ? row.FillColor : _theme.DisabledColor;
            UpdateSliderFill(row);
            if (row.Thumb != null)
                row.Thumb.color = color;
            if (row.ValueLabel != null)
                row.ValueLabel.textColor = _theme.ValueLabelColor;
        }

        public void UpdateToggleRowVisuals(ParkingRulesToggleRow row)
        {
            Color32 color = row.IsEnabled ? _theme.EnabledColor : _theme.DisabledColor;
            row.ToggleButton.color = color;
            row.ToggleButton.textColor = color;
            row.ToggleButton.opacity = row.IsEnabled ? _theme.EnabledOpacity : _theme.DisabledOpacity;
            if (row.IconSprite != null)
            {
                row.IconSprite.color = color;
                row.IconSprite.opacity = row.IsEnabled ? _theme.EnabledOpacity : _theme.DisabledOpacity;
            }
            if (row.DisabledOverlay != null)
            {
                row.DisabledOverlay.color = _theme.DisabledColor;
                row.DisabledOverlay.isVisible = !row.IsEnabled;
            }
        }

        public void ApplySliderRowFromRule(
            ParkingRulesSliderRow row,
            bool enabled,
            ushort radiusMeters,
            Func<ushort, float> convertRadiusToSliderValue,
            Action<ParkingRulesSliderRow, float> setSliderValue)
        {
            float storedValue = convertRadiusToSliderValue(radiusMeters);
            if (storedValue <= 0f)
                storedValue = _getDefaultSliderValue();

            row.LastNonZeroValue = storedValue;

            if (enabled)
            {
                row.IsEnabled = true;
                setSliderValue(row, storedValue);
            }
            else
            {
                row.IsEnabled = false;
                setSliderValue(row, 0f);
            }

            UpdateSliderRowVisuals(row);
        }

        private void SetupPanelLayout(float rowPanelHeight)
        {
            _panel.height = rowPanelHeight * _theme.PanelRowCount + _theme.PanelExtraHeight;
            _panel.autoLayout = true;
            _panel.autoLayoutDirection = LayoutDirection.Vertical;
            _panel.autoLayoutStart = LayoutStart.TopLeft;
            _panel.autoLayoutPadding = new RectOffset(0, 0, 0, 0);
            _panel.autoFitChildrenVertically = true;
        }

        private void CreateHeader(float rowPanelHeight, float rowHeight, float verticalPadding)
        {
            CreateHeaderRow(rowPanelHeight, rowHeight, verticalPadding);
        }

        private void CreateRestrictionsToggleRow(float rowPanelHeight, float rowHeight, float horizontalPadding, float verticalPadding)
        {
            UIPanel row = CreateRowContainer("RestrictionsToggleRow", rowPanelHeight);
            UIButton toggle = row.AddUIComponent<UIButton>();
            toggle.textScale = _theme.RestrictionsToggleTextScale;
            float height = Mathf.Max(_theme.MinButtonHeight, rowHeight - verticalPadding * 2f);
            toggle.size = new Vector2(row.width - horizontalPadding * 2f, height);
            toggle.pivot = UIPivotPoint.TopLeft;
            toggle.relativePosition = new Vector3(
                horizontalPadding,
                (row.height - toggle.height) * 0.5f);
            toggle.atlas = UIView.GetAView().defaultAtlas;
            toggle.normalBgSprite = "LevelBarBackground";
            toggle.hoveredBgSprite = "LevelBarForeground";
            toggle.pressedBgSprite = "LevelBarForeground";
            toggle.eventClicked += (_, __) => _onToggleRestrictions();

            RestrictionsToggleButton = toggle;
        }

        private void CreateRows(
            float rowPanelHeight,
            float rowHeight,
            float horizontalPadding,
            float verticalPadding,
            float iconSize,
            float sliderWidth,
            float sliderHeight,
            Color32 residentsFillColor,
            Color32 workSchoolFillColor)
        {
            ResidentsRow = CreateSliderRow(
                CreateRowContainer("ResidentsRow", rowPanelHeight),
                ParkingRulesIconAtlas.ResidentsSpriteName,
                "R",
                rowHeight,
                horizontalPadding,
                verticalPadding,
                iconSize,
                sliderWidth,
                sliderHeight,
                residentsFillColor);
            WorkSchoolRow = CreateSliderRow(
                CreateRowContainer("WorkSchoolRow", rowPanelHeight),
                ParkingRulesIconAtlas.WorkSchoolSpriteName,
                "W",
                rowHeight,
                horizontalPadding,
                verticalPadding,
                iconSize,
                sliderWidth,
                sliderHeight,
                workSchoolFillColor);
            VisitorsRow = CreateToggleRow(
                CreateRowContainer("VisitorsRow", rowPanelHeight),
                ParkingRulesIconAtlas.VisitorsSpriteName,
                "V",
                rowHeight,
                horizontalPadding,
                verticalPadding,
                iconSize);
        }

        private void CreateFooter(float horizontalPadding, float verticalPadding, float rowPanelHeight)
        {
            FooterRow = CreateRowContainer("FooterRow", rowPanelHeight);
            CreateApplyButton(FooterRow, horizontalPadding, verticalPadding);
        }

        private ParkingRulesSliderRow CreateSliderRow(
            UIPanel rowPanel,
            string iconSpriteName,
            string fallbackText,
            float rowHeight,
            float horizontalPadding,
            float verticalPadding,
            float iconSize,
            float sliderWidth,
            float sliderHeight,
            Color32 fillColor
             )
        {
            var row = new ParkingRulesSliderRow();
            row.RowPanel = rowPanel;

            UISprite icon;
            UISprite disabledOverlay;
            UIButton button = CreateToggleButtonWithIcon(
                rowPanel,
                iconSpriteName,
                fallbackText,
                rowHeight,
                horizontalPadding,
                verticalPadding,
                iconSize,
                out icon,
                out disabledOverlay);

            float sliderX = horizontalPadding * 2f + iconSize;
            UISlider slider = CreateSlider(rowPanel, sliderWidth, sliderHeight, sliderX, rowHeight, verticalPadding);
            UISprite thumb = CreateSliderThumb(slider);
            UISlicedSprite fill = CreateSliderFill(slider, fillColor, thumb);
            UILabel valueLabel = CreateValueLabel(rowPanel, slider, sliderX, rowHeight, horizontalPadding, verticalPadding, sliderHeight);

            row.ToggleButton = button;
            row.IconSprite = icon;
            row.DisabledOverlay = disabledOverlay;
            row.Slider = slider;
            row.FillSprite = fill;
            row.FillColor = fillColor;
            row.Thumb = thumb;
            row.ValueLabel = valueLabel;
            row.LastNonZeroValue = _getDefaultSliderValue();

            button.eventClicked += (_, __) => _onToggleSlider(row);
            slider.eventValueChanged += (_, value) =>
            {
                _onSliderValueChanged(row, value);
                UpdateSliderFill(row);
            };

            if (Log.IsVerboseEnabled && thumb.spriteInfo == null)
                Log.Info("[UI] Slider thumb sprite missing: " + thumb.spriteName);

            UpdateSliderFill(row);
            return row;
        }

        private ParkingRulesToggleRow CreateToggleRow(
            UIPanel rowPanel,
            string iconSpriteName,
            string fallbackText,
            float rowHeight,
            float horizontalPadding,
            float verticalPadding,
            float iconSize)
        {
            var row = new ParkingRulesToggleRow();
            row.RowPanel = rowPanel;

            UISprite icon;
            UISprite disabledOverlay;
            UIButton button = CreateToggleButtonWithIcon(
                rowPanel,
                iconSpriteName,
                fallbackText,
                rowHeight,
                horizontalPadding,
                verticalPadding,
                iconSize,
                out icon,
                out disabledOverlay);

            row.ToggleButton = button;
            row.IconSprite = icon;
            row.DisabledOverlay = disabledOverlay;

            button.eventClicked += (_, __) => _onToggleVisitors();

            return row;
        }

        private UISlider CreateSlider(
            UIPanel rowPanel,
            float sliderWidth,
            float sliderHeight,
            float sliderX,
            float rowHeight,
            float verticalPadding)
        {
            UISlider slider = rowPanel.AddUIComponent<UISlider>();
            slider.orientation = UIOrientation.Horizontal;
            slider.minValue = _sliderMinValue;
            slider.maxValue = _sliderMaxValue;
            slider.stepSize = _sliderStep;
            slider.size = new Vector2(sliderWidth, sliderHeight);
            slider.pivot = UIPivotPoint.TopLeft;
            slider.relativePosition = new Vector3(sliderX, verticalPadding + (rowHeight - sliderHeight) * 0.5f);
            slider.backgroundSprite = "LevelBarBackground";
            slider.color = _theme.SliderTrackColor;
            return slider;
        }

        private UISprite CreateSliderThumb(UISlider slider)
        {
            UISprite thumb = slider.AddUIComponent<UISprite>();
            thumb.spriteName = "SliderBudget";
            thumb.size = new Vector2(_theme.SliderThumbSize, _theme.SliderThumbSize);
            thumb.color = _theme.ThumbColor;
            thumb.atlas = UIView.GetAView().defaultAtlas;
            thumb.zOrder = 5;
            slider.thumbObject = thumb;
            return thumb;
        }

        private UISlicedSprite CreateSliderFill(UISlider slider, Color32 fillColor, UISprite thumb)
        {
            UISlicedSprite fill = slider.AddUIComponent<UISlicedSprite>();
            fill.atlas = UIView.GetAView().defaultAtlas;
            fill.spriteName = "LevelBarForeground";
            fill.color = fillColor;
            fill.relativePosition = new Vector3(0f, 0f);
            fill.size = new Vector2(0f, slider.height);
            fill.isInteractive = false;
            fill.zOrder = 1;
            if (thumb != null && thumb.zOrder <= fill.zOrder)
                thumb.zOrder = fill.zOrder + 1;
            return fill;
        }

        private UILabel CreateValueLabel(
            UIPanel rowPanel,
            UISlider slider,
            float sliderX,
            float rowHeight,
            float horizontalPadding,
            float verticalPadding,
            float sliderHeight)
        {
            UILabel valueLabel = rowPanel.AddUIComponent<UILabel>();
            valueLabel.textScale = _theme.ValueLabelTextScale;
            valueLabel.autoSize = false;
            valueLabel.size = new Vector2(_theme.ValueLabelWidth, _theme.ValueLabelHeight);
            valueLabel.textColor = _theme.ValueLabelColor;
            valueLabel.backgroundSprite = "LevelBarBackground";
            valueLabel.color = _theme.SliderTrackColor;
            valueLabel.atlas = UIView.GetAView().defaultAtlas;
            float labelX = rowPanel.width - horizontalPadding - valueLabel.width;
            float sliderMaxWidth = labelX - sliderX - horizontalPadding;
            if (slider != null && sliderMaxWidth < slider.width)
                slider.size = new Vector2(Mathf.Max(_theme.MinSliderWidth, sliderMaxWidth), sliderHeight);
            valueLabel.relativePosition = new Vector3(
                labelX,
                verticalPadding + (rowHeight - valueLabel.height) * 0.5f);
            valueLabel.textAlignment = UIHorizontalAlignment.Center;
            valueLabel.verticalAlignment = UIVerticalAlignment.Bottom;
            valueLabel.pivot = UIPivotPoint.TopLeft;
            return valueLabel;
        }

        private UIButton CreateToggleButtonWithIcon(
            UIPanel rowPanel,
            string iconSpriteName,
            string fallbackText,
            float rowHeight,
            float horizontalPadding,
            float verticalPadding,
            float iconSize,
            out UISprite icon,
            out UISprite disabledOverlay)
        {
            UIButton button = rowPanel.AddUIComponent<UIButton>();
            button.text = fallbackText;
            button.textScale = _theme.ToggleTextScale;
            button.size = new Vector2(iconSize, iconSize);
            button.pivot = UIPivotPoint.TopLeft;
            button.relativePosition = new Vector3(
                horizontalPadding,
                verticalPadding + (rowHeight - iconSize) * 0.5f);
            button.normalBgSprite = "OptionBase";
            button.hoveredBgSprite = "OptionBaseHovered";
            button.pressedBgSprite = "OptionBasePressed";

            icon = TryAttachIcon(button, iconSpriteName, fallbackText);
            if (icon != null)
            {
                icon.isInteractive = false;
                icon.zOrder = 20;
                icon.BringToFront();
            }

            disabledOverlay = TryAttachDisabledOverlay(button);
            if (disabledOverlay != null)
            {
                disabledOverlay.zOrder = 21;
                disabledOverlay.BringToFront();
            }

            return button;
        }

        private UIPanel CreateRowContainer(string name, float rowHeight)
        {
            UIPanel rowPanel = _panel.AddUIComponent<UIPanel>();
            rowPanel.name = name;
            rowPanel.width = _panel.width;
            rowPanel.height = rowHeight;
            rowPanel.autoLayout = false;
            return rowPanel;
        }

        private void CreateHeaderRow(float rowPanelHeight, float rowHeight, float verticalPadding)
        {
            UIPanel headerRow = CreateRowContainer("HeaderRow", rowPanelHeight);
            UILabel title = headerRow.AddUIComponent<UILabel>();
            title.text = "Picky Parking Restrictions";
            title.textScale = _theme.HeaderTextScale;
            title.textColor = _theme.EnabledColor;
            title.autoSize = false;
            title.size = new Vector2(headerRow.width, rowHeight);
            title.textAlignment = UIHorizontalAlignment.Center;
            title.verticalAlignment = UIVerticalAlignment.Middle;
            title.relativePosition = new Vector3(0f, verticalPadding);
        }

        public void UpdateRestrictionsToggleVisuals(bool enabled)
        {
            if (RestrictionsToggleButton == null)
                return;

            RestrictionsToggleButton.text = enabled ? "Restrictions: On" : "Restrictions: Off";
            Color32 color = enabled ? _theme.EnabledColor : _theme.DisabledColor;
            RestrictionsToggleButton.color = color;
            RestrictionsToggleButton.textColor = color;
        }

        public void SetRestrictionsContentVisible(bool visible)
        {
            if (ResidentsRow != null && ResidentsRow.RowPanel != null)
                ResidentsRow.RowPanel.isVisible = visible;
            if (WorkSchoolRow != null && WorkSchoolRow.RowPanel != null)
                WorkSchoolRow.RowPanel.isVisible = visible;
            if (VisitorsRow != null && VisitorsRow.RowPanel != null)
                VisitorsRow.RowPanel.isVisible = visible;
            if (FooterRow != null)
                FooterRow.isVisible = visible;
        }

        private void CreateApplyButton(UIPanel footerRow, float horizontalPadding, float verticalPadding)
        {
            if (footerRow == null)
                return;
            if (_onApplyChanges == null)
                return;

            UIButton applyButton = footerRow.AddUIComponent<UIButton>();
            applyButton.text = "Apply";
            applyButton.textScale = _theme.ApplyButtonTextScale;
            float height = Mathf.Max(_theme.MinButtonHeight, footerRow.height - verticalPadding * 2f);
            applyButton.size = new Vector2(footerRow.width - horizontalPadding * 2f, height);
            applyButton.pivot = UIPivotPoint.TopLeft;
            applyButton.relativePosition = new Vector3(
                horizontalPadding,
                (footerRow.height - applyButton.height) * 0.5f);
            applyButton.atlas = UIView.GetAView().defaultAtlas;
            applyButton.normalBgSprite = "LevelBarBackground";
            applyButton.hoveredBgSprite = "LevelBarForeground";
            applyButton.pressedBgSprite = "LevelBarForeground";

            applyButton.eventClicked += (_, __) => _onApplyChanges();
        }

        

        private float GetRowDisplayValue(ParkingRulesSliderRow row)
        {
            return row.LastNonZeroValue > 0f ? row.LastNonZeroValue : _getDefaultSliderValue();
        }

        private string FormatDistanceDisplay(float t)
        {
            float meters = 0f;
            if (t > 0f && t < _distanceSliderMaxValue)
            {
                meters = DistanceSliderMapping.SliderToDistanceMeters(
                    t,
                    _distanceSliderMinValue,
                    _distanceSliderMaxValue,
                    _minDistanceMeters,
                    _midDistanceMeters,
                    _maxDistanceMeters,
                    _distanceMidpointT);
            }
            else if (t >= _distanceSliderMaxValue)
            {
                meters = _maxDistanceMeters;
            }

            return DistanceDisplayFormatter.FormatDisplay(
                t,
                _distanceSliderMaxValue,
                _minDistanceMeters,
                _maxDistanceMeters,
                meters);
        }

        private UISprite TryAttachIcon(UIButton button, string spriteName, string fallbackText)
        {
            var atlas = ParkingRulesIconAtlas.GetOrCreate();
            if (atlas == null)
                return null;

            var icon = button.AddUIComponent<UISprite>();
            icon.atlas = atlas;
            icon.spriteName = spriteName;
            float size = Mathf.Min(button.width, button.height) * _theme.IconScale;
            icon.size = new Vector2(size, size);
            icon.relativePosition = new Vector3(
                (button.width - size) * 0.5f,
                (button.height - size) * 0.5f);

            button.text = string.Empty;

            return icon;
        }

        private UISprite TryAttachDisabledOverlay(UIComponent parent)
        {
            var atlas = ParkingRulesIconAtlas.GetOrCreate();
            if (atlas == null)
                return null;

            var overlay = parent.AddUIComponent<UISprite>();
            overlay.atlas = atlas;
            overlay.spriteName = ParkingRulesIconAtlas.CrossedOutSpriteName;
            overlay.size = new Vector2(parent.width, parent.height);
            overlay.relativePosition = new Vector3(0f, 0f);
            overlay.isInteractive = false;
            overlay.isVisible = false;
            overlay.zOrder = 100;

            return overlay;
        }

        private void UpdateSliderFill(ParkingRulesSliderRow row)
        {
            if (row == null || row.Slider == null || row.FillSprite == null)
                return;

            float range = row.Slider.maxValue - row.Slider.minValue;
            float normalized = range <= 0f ? 0f : (row.Slider.value - row.Slider.minValue) / range;
            float width = row.Slider.width * Mathf.Clamp01(normalized);
            row.FillSprite.size = new Vector2(width, row.Slider.height);
        }
    }

    internal sealed class ParkingRulesSliderRow
    {
        public UIPanel RowPanel;
        public UIButton ToggleButton;
        public UISprite IconSprite;
        public UISprite DisabledOverlay;
        public UISlider Slider;
        public UISprite FillSprite;
        public Color32 FillColor;
        public UISprite Thumb;
        public UILabel ValueLabel;
        public float LastNonZeroValue;
        public bool IsEnabled;
    }

    internal sealed class ParkingRulesToggleRow
    {
        public UIPanel RowPanel;
        public UIButton ToggleButton;
        public UISprite IconSprite;
        public UISprite DisabledOverlay;
        public bool IsEnabled;
    }
}

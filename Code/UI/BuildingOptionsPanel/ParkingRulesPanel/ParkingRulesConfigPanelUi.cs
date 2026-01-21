using System;
using UnityEngine;
using ColossalFramework.UI;
using PickyParking.Features.Debug;
using PickyParking.Features.ParkingRules;
using PickyParking.Logging;
using PickyParking.UI.BuildingOptionsPanel;
using PickyParking.Settings;

namespace PickyParking.UI.BuildingOptionsPanel.ParkingRulesPanel
{
    internal sealed class ParkingRulesConfigPanelUi
    {
        private sealed class ParkingRulesConfigPanelUiArgs
        {
            public ParkingRulesConfigPanel Panel { get; set; }
            public ParkingPanelTheme Theme { get; set; }
            public ParkingRulesConfigUiConfig UiConfig { get; set; }
            public Func<float> GetDefaultSliderValue { get; set; }
            public Action OnToggleRestrictions { get; set; }
            public Action<ParkingRulesSliderRow> OnToggleSlider { get; set; }
            public Action<ParkingRulesSliderRow, float> OnSliderValueChanged { get; set; }
            public Action OnToggleVisitors { get; set; }
            public Action OnApplyChanges { get; set; }
            public Action OnCopyRule { get; set; }
            public Action OnPasteRule { get; set; }
            public Action OnResetChanges { get; set; }
        }

        private struct PanelLayout
        {
            public float RowHeight;
            public float HorizontalPadding;
            public float VerticalPadding;
            public float RowPanelHeight;
            public float IconSize;
            public float SliderHeight;
            public float SliderWidth;
            public Color32 ResidentsFillColor;
            public Color32 WorkSchoolFillColor;
        }

        private struct ToggleButtonWithIconArgs
        {
            public UIPanel RowPanel;
            public string IconSpriteName;
            public string FallbackText;
            public PanelLayout Layout;
        }

        private struct ToggleButtonWithIconResult
        {
            public UIButton Button;
            public UISprite Icon;
            public UISprite DisabledOverlay;
        }


        private readonly ParkingRulesConfigPanel _panel;
        private readonly ParkingPanelTheme _theme;
        private readonly ParkingRulesConfigUiConfig _uiConfig;
        private readonly Func<float> _getDefaultSliderValue;
        private readonly Action _onToggleRestrictions;
        private readonly Action<ParkingRulesSliderRow> _onToggleSlider;
        private readonly Action<ParkingRulesSliderRow, float> _onSliderValueChanged;
        private readonly Action _onToggleVisitors;
        private readonly Action _onApplyChanges;
        private readonly Action _onCopyRule;
        private readonly Action _onPasteRule;
        private readonly Action _onResetChanges;

        public UIButton RestrictionsToggleButton { get; private set; }
        public ParkingRulesSliderRow ResidentsRow { get; private set; }
        public ParkingRulesSliderRow WorkSchoolRow { get; private set; }
        public ParkingRulesToggleRow VisitorsRow { get; private set; }
        public UIPanel FooterRow { get; private set; }
        public UILabel ParkingSpacesLabel { get; private set; }
        public UIButton ApplyButton { get; private set; }
        public UIButton CopyButton { get; private set; }
        public UIButton PasteButton { get; private set; }
        public UIButton ResetButton { get; private set; }

        public static ParkingRulesConfigPanelUi Create(
            ParkingRulesConfigPanel panel,
            ParkingPanelTheme theme,
            ParkingRulesConfigUiConfig uiConfig,
            Func<float> getDefaultSliderValue,
            Action onToggleRestrictions,
            Action<ParkingRulesSliderRow> onToggleSlider,
            Action<ParkingRulesSliderRow, float> onSliderValueChanged,
            Action onToggleVisitors,
            Action onApplyChanges,
            Action onCopyRule,
            Action onPasteRule,
            Action onResetChanges)
        {
            var args = new ParkingRulesConfigPanelUiArgs
            {
                Panel = panel,
                Theme = theme,
                UiConfig = uiConfig,
                GetDefaultSliderValue = getDefaultSliderValue,
                OnToggleRestrictions = onToggleRestrictions,
                OnToggleSlider = onToggleSlider,
                OnSliderValueChanged = onSliderValueChanged,
                OnToggleVisitors = onToggleVisitors,
                OnApplyChanges = onApplyChanges,
                OnCopyRule = onCopyRule,
                OnPasteRule = onPasteRule,
                OnResetChanges = onResetChanges
            };

            return new ParkingRulesConfigPanelUi(args);
        }

        private ParkingRulesConfigPanelUi(ParkingRulesConfigPanelUiArgs args)
        {
            if (args == null) throw new ArgumentNullException(nameof(args));

            _panel = args.Panel;
            _theme = args.Theme;
            _uiConfig = args.UiConfig ?? ParkingRulesConfigUiConfig.Default;
            _getDefaultSliderValue = args.GetDefaultSliderValue;
            _onToggleRestrictions = args.OnToggleRestrictions;
            _onToggleSlider = args.OnToggleSlider;
            _onSliderValueChanged = args.OnSliderValueChanged;
            _onToggleVisitors = args.OnToggleVisitors;
            _onApplyChanges = args.OnApplyChanges;
            _onCopyRule = args.OnCopyRule;
            _onPasteRule = args.OnPasteRule;
            _onResetChanges = args.OnResetChanges;
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

        public void BuildUi()
        {
            PanelLayout layout = BuildLayout();

            SetupPanelLayout(layout);
            CreateHeader(layout);
            CreateRestrictionsToggleRow(layout);
            CreateRows(layout);
            CreateFooter(layout);
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

        public void UpdateParkingSpacesText(int totalSpaces, int freeSpaces)
        {
            if (ParkingSpacesLabel == null)
                return;

            ParkingSpacesLabel.text = "Spaces: " + totalSpaces + " total, " + freeSpaces + " free";
        }

        public void UpdateParkingSpacesUnavailable()
        {
            if (ParkingSpacesLabel == null)
                return;

            ParkingSpacesLabel.text = "Spaces: n/a";
        }

        public void UpdateApplyButtonState(bool hasUnappliedChanges)
        {
            if (ApplyButton == null)
                return;

            ApplyButton.isEnabled = hasUnappliedChanges;
            ApplyButton.text = hasUnappliedChanges ? "Apply" : "Applied";
        }

        public void UpdateCopyButtonState(bool show)
        {
            if (CopyButton == null)
                return;

            CopyButton.isVisible = show;
            CopyButton.isEnabled = show;
        }

        public void UpdatePasteButtonState(bool hasClipboardRule, bool canApply)
        {
            if (PasteButton == null)
                return;

            bool visible = hasClipboardRule;
            PasteButton.isVisible = visible;
            PasteButton.isEnabled = visible && canApply;
        }

        public void UpdateResetButtonState(bool show, bool canReset)
        {
            if (ResetButton == null)
                return;

            ResetButton.isVisible = show;
            ResetButton.isEnabled = show && canReset;
        }

        private PanelLayout BuildLayout()
        {
            float rowHeight = _theme.RowHeight;
            float horizontalPadding = _theme.HorizontalPadding;
            float verticalPadding = _theme.VerticalPadding;
            float rowPanelHeight = _theme.RowPanelHeight;
            float iconSize = _theme.IconSize;
            float sliderHeight = _theme.SliderHeight;
            float sliderWidth = Mathf.Max(_theme.MinSliderWidth, _panel.width - (horizontalPadding * 3f + iconSize));

            return new PanelLayout
            {
                RowHeight = rowHeight,
                HorizontalPadding = horizontalPadding,
                VerticalPadding = verticalPadding,
                RowPanelHeight = rowPanelHeight,
                IconSize = iconSize,
                SliderHeight = sliderHeight,
                SliderWidth = sliderWidth,
                ResidentsFillColor = _theme.ResidentsFillColor,
                WorkSchoolFillColor = _theme.WorkSchoolFillColor
            };
        }

        private void SetupPanelLayout(PanelLayout layout)
        {
            _panel.height = layout.RowPanelHeight * _theme.PanelRowCount + _theme.PanelExtraHeight;
            _panel.autoLayout = true;
            _panel.autoLayoutDirection = LayoutDirection.Vertical;
            _panel.autoLayoutStart = LayoutStart.TopLeft;
            _panel.autoLayoutPadding = new RectOffset(0, 0, 0, 0);
            _panel.autoFitChildrenVertically = true;
        }

        private void CreateHeader(PanelLayout layout)
        {
            CreateHeaderRow(layout);
            CreateParkingStatsRow(layout);
        }

        private void CreateRestrictionsToggleRow(PanelLayout layout)
        {
            UIPanel row = CreateRowContainer("RestrictionsToggleRow", layout.RowPanelHeight);
            UIButton toggle = CreateButton(row, true, string.Empty);
            toggle.textScale = _theme.RestrictionsToggleTextScale;
            float height = Mathf.Max(_theme.MinButtonHeight, layout.RowHeight - layout.VerticalPadding * 2f);
            toggle.size = new Vector2(row.width - layout.HorizontalPadding * 2f, height);
            toggle.pivot = UIPivotPoint.TopLeft;
            toggle.relativePosition = new Vector3(
                layout.HorizontalPadding,
                (row.height - toggle.height) * 0.5f);
            toggle.atlas = UIView.GetAView().defaultAtlas;
            toggle.normalBgSprite = "LevelBarBackground";
            toggle.hoveredBgSprite = "LevelBarForeground";
            toggle.pressedBgSprite = "LevelBarForeground";
            toggle.eventClicked += (_, __) => _onToggleRestrictions();

            RestrictionsToggleButton = toggle;
        }

        private void CreateRows(PanelLayout layout)
        {
            ResidentsRow = CreateSliderRow(
                CreateRowContainer("ResidentsRow", layout.RowPanelHeight),
                ParkingRulesIconAtlasUiValues.ResidentsSpriteName,
                "R",
                layout.ResidentsFillColor,
                layout);
            WorkSchoolRow = CreateSliderRow(
                CreateRowContainer("WorkSchoolRow", layout.RowPanelHeight),
                ParkingRulesIconAtlasUiValues.WorkSchoolSpriteName,
                "W",
                layout.WorkSchoolFillColor,
                layout);
            VisitorsRow = CreateToggleRow(
                CreateRowContainer("VisitorsRow", layout.RowPanelHeight),
                ParkingRulesIconAtlasUiValues.VisitorsSpriteName,
                "V",
                layout);
        }

        private void CreateFooter(PanelLayout layout)
        {
            FooterRow = CreateRowContainer("FooterRow", layout.RowPanelHeight);
            CreateFooterButtons(FooterRow, layout);
        }

        private ParkingRulesSliderRow CreateSliderRow(
            UIPanel rowPanel,
            string iconSpriteName,
            string fallbackText,
            Color32 fillColor,
            PanelLayout layout)
        {
            var row = new ParkingRulesSliderRow();
            row.RowPanel = rowPanel;

            ToggleButtonWithIconResult toggle = CreateToggleButtonWithIcon(new ToggleButtonWithIconArgs
            {
                RowPanel = rowPanel,
                IconSpriteName = iconSpriteName,
                FallbackText = fallbackText,
                Layout = layout
            });

            float sliderX = layout.HorizontalPadding * 2f + layout.IconSize;
            UISlider slider = CreateSlider(rowPanel, layout, sliderX);
            UISprite thumb = CreateSliderThumb(slider);
            UISlicedSprite fill = CreateSliderFill(slider, fillColor, thumb);
            UILabel valueLabel = CreateValueLabel(rowPanel, slider, layout, sliderX);

            row.ToggleButton = toggle.Button;
            row.IconSprite = toggle.Icon;
            row.DisabledOverlay = toggle.DisabledOverlay;
            row.Slider = slider;
            row.FillSprite = fill;
            row.FillColor = fillColor;
            row.Thumb = thumb;
            row.ValueLabel = valueLabel;
            row.LastNonZeroValue = _getDefaultSliderValue();

            toggle.Button.eventClicked += (_, __) => _onToggleSlider(row);
            slider.eventValueChanged += (_, value) =>
            {
                _onSliderValueChanged(row, value);
                UpdateSliderFill(row);
            };

            if (Log.Dev.IsEnabled(DebugLogCategory.RuleUi) && thumb.spriteInfo == null)
            {
                Log.Dev.Info(DebugLogCategory.RuleUi, LogPath.Any, "SliderThumbSpriteMissing", "sprite=" + thumb.spriteName);
            }

            UpdateSliderFill(row);
            return row;
        }

        private ParkingRulesToggleRow CreateToggleRow(
            UIPanel rowPanel,
            string iconSpriteName,
            string fallbackText,
            PanelLayout layout)
        {
            var row = new ParkingRulesToggleRow();
            row.RowPanel = rowPanel;

            ToggleButtonWithIconResult toggle = CreateToggleButtonWithIcon(new ToggleButtonWithIconArgs
            {
                RowPanel = rowPanel,
                IconSpriteName = iconSpriteName,
                FallbackText = fallbackText,
                Layout = layout
            });

            row.ToggleButton = toggle.Button;
            row.IconSprite = toggle.Icon;
            row.DisabledOverlay = toggle.DisabledOverlay;

            toggle.Button.eventClicked += (_, __) => _onToggleVisitors();

            return row;
        }

        private UISlider CreateSlider(UIPanel rowPanel, PanelLayout layout, float sliderX)
        {
            UISlider slider = rowPanel.AddUIComponent<UISlider>();
            slider.orientation = UIOrientation.Horizontal;
            slider.minValue = _uiConfig.SliderMinValue;
            slider.maxValue = _uiConfig.SliderMaxValue;
            slider.stepSize = _uiConfig.SliderStep;
            slider.size = new Vector2(layout.SliderWidth, layout.SliderHeight);
            slider.pivot = UIPivotPoint.TopLeft;
            slider.relativePosition = new Vector3(
                sliderX,
                layout.VerticalPadding + (layout.RowHeight - layout.SliderHeight) * 0.5f);
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

        private UILabel CreateValueLabel(UIPanel rowPanel, UISlider slider, PanelLayout layout, float sliderX)
        {
            UILabel valueLabel = rowPanel.AddUIComponent<UILabel>();
            valueLabel.textScale = _theme.ValueLabelTextScale;
            valueLabel.autoSize = false;
            valueLabel.size = new Vector2(_theme.ValueLabelWidth, _theme.ValueLabelHeight);
            valueLabel.textColor = _theme.ValueLabelColor;
            valueLabel.backgroundSprite = "LevelBarBackground";
            valueLabel.color = _theme.SliderTrackColor;
            valueLabel.atlas = UIView.GetAView().defaultAtlas;
            float labelX = rowPanel.width - layout.HorizontalPadding - valueLabel.width;
            float sliderMaxWidth = labelX - sliderX - layout.HorizontalPadding;
            if (slider != null && sliderMaxWidth < slider.width)
                slider.size = new Vector2(Mathf.Max(_theme.MinSliderWidth, sliderMaxWidth), layout.SliderHeight);
            valueLabel.relativePosition = new Vector3(
                labelX,
                layout.VerticalPadding + (layout.RowHeight - valueLabel.height) * 0.5f);
            valueLabel.textAlignment = UIHorizontalAlignment.Center;
            valueLabel.verticalAlignment = UIVerticalAlignment.Bottom;
            valueLabel.pivot = UIPivotPoint.TopLeft;
            return valueLabel;
        }

        private ToggleButtonWithIconResult CreateToggleButtonWithIcon(ToggleButtonWithIconArgs args)
        {
            UIButton button = CreateButton(args.RowPanel, false, args.FallbackText);
            button.textScale = _theme.ToggleTextScale;
            button.size = new Vector2(args.Layout.IconSize, args.Layout.IconSize);
            button.pivot = UIPivotPoint.TopLeft;
            button.relativePosition = new Vector3(
                args.Layout.HorizontalPadding,
                args.Layout.VerticalPadding + (args.Layout.RowHeight - args.Layout.IconSize) * 0.5f);
            button.normalBgSprite = "OptionBase";
            button.hoveredBgSprite = "OptionBaseHovered";
            button.pressedBgSprite = "OptionBasePressed";

            UISprite icon = TryAttachIcon(button, args.IconSpriteName, args.FallbackText);
            if (icon != null)
            {
                icon.isInteractive = false;
                icon.zOrder = 20;
                icon.BringToFront();
            }

            UISprite disabledOverlay = TryAttachDisabledOverlay(button);
            if (disabledOverlay != null)
            {
                disabledOverlay.zOrder = 21;
                disabledOverlay.BringToFront();
            }

            return new ToggleButtonWithIconResult
            {
                Button = button,
                Icon = icon,
                DisabledOverlay = disabledOverlay
            };
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

        private void CreateHeaderRow(PanelLayout layout)
        {
            UIPanel headerRow = CreateRowContainer("HeaderRow", layout.RowPanelHeight);
            UILabel title = headerRow.AddUIComponent<UILabel>();
            title.text = "Picky Parking Restrictions";
            title.textScale = _theme.HeaderTextScale;
            title.textColor = _theme.EnabledColor;
            title.autoSize = false;
            title.size = new Vector2(headerRow.width, layout.RowHeight);
            title.textAlignment = UIHorizontalAlignment.Center;
            title.verticalAlignment = UIVerticalAlignment.Middle;
            title.relativePosition = new Vector3(0f, layout.VerticalPadding);
        }

        private void CreateParkingStatsRow(PanelLayout layout)
        {
            UIPanel statsRow = CreateRowContainer("ParkingStatsRow", layout.RowPanelHeight);
            UILabel stats = statsRow.AddUIComponent<UILabel>();
            stats.text = "Spaces: n/a";
            stats.textScale = _theme.ParkingStatsTextScale;
            stats.textColor = _theme.EnabledColor;
            stats.autoSize = false;
            stats.size = new Vector2(statsRow.width, layout.RowHeight);
            stats.textAlignment = UIHorizontalAlignment.Center;
            stats.verticalAlignment = UIVerticalAlignment.Middle;
            stats.relativePosition = new Vector3(0f, layout.VerticalPadding);
            ParkingSpacesLabel = stats;
        }

        private void CreateFooterButtons(UIPanel footerRow, PanelLayout layout)
        {
            if (footerRow == null)
                return;

            const int buttonCount = 4;
            float buttonSpacing = layout.HorizontalPadding;
            float height = Mathf.Max(_theme.MinButtonHeight, footerRow.height - layout.VerticalPadding * 2f);
            float availableWidth = Mathf.Max(0f, footerRow.width - buttonSpacing * (buttonCount + 1));
            float buttonWidth = buttonCount > 0 ? availableWidth / buttonCount : 0f;
            float baselineY = (footerRow.height - height) * 0.5f;

            float x = buttonSpacing;

            CopyButton = CreateButton(footerRow, true, "Copy");
            CopyButton.textScale = _theme.ToggleTextScale;
            CopyButton.size = new Vector2(buttonWidth, height);
            CopyButton.pivot = UIPivotPoint.TopLeft;
            CopyButton.relativePosition = new Vector3(x, baselineY);
            CopyButton.isVisible = false;
            CopyButton.isEnabled = false;
            CopyButton.eventClicked += (_, __) => _onCopyRule?.Invoke();
            x += buttonWidth + buttonSpacing;

            PasteButton = CreateButton(footerRow, true, "Paste");
            PasteButton.textScale = _theme.ToggleTextScale;
            PasteButton.size = new Vector2(buttonWidth, height);
            PasteButton.pivot = UIPivotPoint.TopLeft;
            PasteButton.relativePosition = new Vector3(x, baselineY);
            PasteButton.isVisible = false;
            PasteButton.isEnabled = false;
            PasteButton.eventClicked += (_, __) => _onPasteRule?.Invoke();
            x += buttonWidth + buttonSpacing;

            ResetButton = CreateButton(footerRow, true, "Reset");
            ResetButton.textScale = _theme.ToggleTextScale;
            ResetButton.size = new Vector2(buttonWidth, height);
            ResetButton.pivot = UIPivotPoint.TopLeft;
            ResetButton.relativePosition = new Vector3(x, baselineY);
            ResetButton.isVisible = false;
            ResetButton.isEnabled = false;
            ResetButton.eventClicked += (_, __) => _onResetChanges?.Invoke();
            x += buttonWidth + buttonSpacing;

            ApplyButton = CreateButton(footerRow, true, "Apply");
            ApplyButton.textScale = _theme.ApplyButtonTextScale;
            ApplyButton.size = new Vector2(buttonWidth, height);
            ApplyButton.pivot = UIPivotPoint.TopLeft;
            ApplyButton.relativePosition = new Vector3(x, baselineY);
            ApplyButton.eventClicked += (_, __) => _onApplyChanges?.Invoke();
        }

        private static UIButton CreateButton(UIPanel parent,bool useDefaultSprites, string text ="")
        {
            UIButton newButton = parent.AddUIComponent<UIButton>();
            if (!string.IsNullOrEmpty(text))
                newButton.text = text;
            newButton.atlas = UIView.GetAView().defaultAtlas;
            if (useDefaultSprites)
            {
                newButton.normalBgSprite = "LevelBarBackground";
                newButton.hoveredBgSprite = "LevelBarForeground";
                newButton.pressedBgSprite = "LevelBarForeground";
                newButton.disabledBgSprite = "LevelBarDisabled";
            }

            newButton.playAudioEvents = true;
            newButton.pressedColor = new Color32(210, 210, 210, 255);
            return newButton;
        }

        private float GetRowDisplayValue(ParkingRulesSliderRow row)
        {
            return row.LastNonZeroValue > 0f ? row.LastNonZeroValue : _getDefaultSliderValue();
        }

        private string FormatDistanceDisplay(float t)
        {
            float meters = 0f;
            if (t > 0f && t < _uiConfig.DistanceSliderMaxValue)
            {
                meters = DistanceSliderMapping.SliderToDistanceMeters(t, _uiConfig);
            }
            else if (t >= _uiConfig.DistanceSliderMaxValue)
            {
                meters = ParkingRulesLimits.MaxRadiusMeters;
            }

            return DistanceDisplayFormatter.FormatDisplay(
                t,
                _uiConfig.DistanceSliderMaxValue,
                ParkingRulesLimits.MinRadiusMeters,
                ParkingRulesLimits.MaxRadiusMeters,
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
            overlay.spriteName = ParkingRulesIconAtlasUiValues.CrossedOutSpriteName;
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
}









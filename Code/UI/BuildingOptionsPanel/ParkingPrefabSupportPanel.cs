using UnityEngine;
using ColossalFramework.UI;
using PickyParking.Features.Debug;
using PickyParking.Features.ParkingLotPrefabs;
using PickyParking.Logging;
using PickyParking.UI;
using PickyParking.Settings;

namespace PickyParking.UI.BuildingOptionsPanel
{
    public sealed class ParkingPrefabSupportPanel : UIPanel
    {
        private ushort _buildingId;
        private string _prefabName;
        private PrefabKey _prefabKey;
        private bool _hasPrefabKey;
        private UILabel _messageLabel;
        private UIButton _actionButton;
        private UiServices _services;

        public void Initialize(UiServices services)
        {
            _services = services;
        }

        public override void Start()
        {
            base.Start();
            ConfigurePanel();
            BuildUi();
        }

        public void Bind(ushort buildingId, BuildingUiInfo info)
        {
            _buildingId = buildingId;
            _prefabName = info.PrefabName;
            _prefabKey = info.PrefabKey;
            _hasPrefabKey = info.HasPrefabKey;
            Refresh();
        }

        private void ConfigurePanel()
        {
            name = "PickyParkingSupportPanel";
            isVisible = false;
            if (parent != null)
                width = parent.width;
            else
                width = 300f;
            backgroundSprite = string.Empty;

            autoLayout = true;
            autoLayoutDirection = LayoutDirection.Vertical;
            autoLayoutStart = LayoutStart.TopLeft;
            autoLayoutPadding = new RectOffset(0, 0, 0, 0);
            autoFitChildrenVertically = true;
        }

        private void BuildUi()
        {
            CreateHeaderRow();
            CreateMessageRow();
            CreateActionRow();
        }

        private void CreateHeaderRow()
        {
            UIPanel row = CreateRowContainer("HeaderRow");
            UILabel title = row.AddUIComponent<UILabel>();
            title.text = "Picky Parking";
            title.textScale = 1f;
            title.textColor = new Color32(255, 255, 255, 255);
            title.autoSize = false;
            title.size = new Vector2(row.width, BuildingOptionsPanelUiValues.PanelTheme.RowHeight);
            title.textAlignment = UIHorizontalAlignment.Center;
            title.verticalAlignment = UIVerticalAlignment.Middle;
            title.relativePosition = new Vector3(0f, BuildingOptionsPanelUiValues.PanelTheme.VerticalPadding);
        }

        private void CreateMessageRow()
        {
            UIPanel row = CreateRowContainer("MessageRow");
            _messageLabel = row.AddUIComponent<UILabel>();
            _messageLabel.textScale = 0.8f;
            _messageLabel.textColor = new Color32(200, 200, 200, 255);
            _messageLabel.autoSize = false;
            _messageLabel.size = new Vector2(row.width - BuildingOptionsPanelUiValues.PanelTheme.HorizontalPadding * 2f, BuildingOptionsPanelUiValues.PanelTheme.RowHeight);
            _messageLabel.textAlignment = UIHorizontalAlignment.Center;
            _messageLabel.verticalAlignment = UIVerticalAlignment.Middle;
            _messageLabel.relativePosition = new Vector3(
                BuildingOptionsPanelUiValues.PanelTheme.HorizontalPadding,
                BuildingOptionsPanelUiValues.PanelTheme.VerticalPadding);
            _messageLabel.wordWrap = true;
        }

        private void CreateActionRow()
        {
            UIPanel row = CreateRowContainer("ActionRow");
            _actionButton = row.AddUIComponent<UIButton>();
            _actionButton.textScale = 0.9f;
            _actionButton.size = new Vector2(
                row.width - BuildingOptionsPanelUiValues.PanelTheme.HorizontalPadding * 2f,
                BuildingOptionsPanelUiValues.PanelTheme.RowHeight - BuildingOptionsPanelUiValues.PanelTheme.VerticalPadding * 2f);
            _actionButton.pivot = UIPivotPoint.TopLeft;
            _actionButton.relativePosition = new Vector3(
                BuildingOptionsPanelUiValues.PanelTheme.HorizontalPadding,
                (row.height - _actionButton.height) * 0.5f);
            _actionButton.atlas = UIView.GetAView().defaultAtlas;
            _actionButton.playAudioEvents = true;
            _actionButton.normalBgSprite = "LevelBarBackground";
            _actionButton.hoveredBgSprite = "LevelBarForeground";
            _actionButton.pressedBgSprite = "LevelBarForeground";
            _actionButton.eventClicked += (_, __) => HandleActionClicked();
        }

        private UIPanel CreateRowContainer(string name)
        {
            UIPanel row = AddUIComponent<UIPanel>();
            row.name = name;
            row.width = width;
            row.height = BuildingOptionsPanelUiValues.PanelTheme.RowHeight + BuildingOptionsPanelUiValues.PanelTheme.VerticalPadding * 2f;
            row.autoLayout = false;
            return row;
        }

        private void Refresh()
        {
            if (_messageLabel == null || _actionButton == null)
                return;

            string prefabName = !string.IsNullOrEmpty(_prefabName) ? _prefabName : "Unknown asset";

            _messageLabel.text = "Add Picky Parking to Asset " + prefabName + "?";
            _actionButton.text = "Add Picky Parking to asset";
            
            _actionButton.isEnabled = _buildingId != 0;
            UpdateActionButtonVisuals();
        }

        private void HandleActionClicked()
        {
            if (_buildingId == 0)
                return;

            TryAddSupportedPrefab();
        }

        private void TryAddSupportedPrefab()
        {
            if (!_hasPrefabKey)
                return;

            if (_services == null || _services.SupportedParkingLotRegistry == null)
                return;

            bool added = _services.SupportedParkingLotRegistry.Add(_prefabKey);
            if (!added)
                return;

            if (_services.SettingsController != null && _services.SettingsController.Current != null)
            {
                var settings = _services.SettingsController.Current;
                var list = settings.SupportedParkingLotPrefabs;
                if (list == null)
                {
                    list = new System.Collections.Generic.List<PrefabKey>();
                    settings.SupportedParkingLotPrefabs = list;
                }

                if (!list.Contains(_prefabKey))
                    list.Add(_prefabKey);

                _services.SettingsController.Save("UI.AddSupportedPrefab");
            }
            else
            {
                Log.Player.Warn(DebugLogCategory.RuleUi, LogPath.Any, "SupportedPrefabAddNotPersisted", "prefab=" + _prefabKey);
            }

            if (Log.Dev.IsEnabled(DebugLogCategory.RuleUi))
            {
                Log.Dev.Info(DebugLogCategory.RuleUi, LogPath.Any, "SupportedPrefabAdded", "prefab=" + _prefabKey);
            }
        }

        private void UpdateActionButtonVisuals()
        {
            if (_actionButton == null)
                return;

            _actionButton.color = Color.white;
            _actionButton.textColor = _actionButton.isEnabled
                ? BuildingOptionsPanelUiValues.PanelTheme.EnabledColor
                : BuildingOptionsPanelUiValues.PanelTheme.DisabledColor;
        }
    }
}








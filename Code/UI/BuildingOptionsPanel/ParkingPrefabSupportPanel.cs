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
        private ParkingPanelTheme _theme;

        public void Initialize(UiServices services)
        {
            _services = services;
            _theme = new ParkingPanelTheme(services);
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
            UIPanel row = _theme.CreateRowContainer(this, "HeaderRow");
            _theme.CreateLabel(
                row,
                "Picky Parking",
                new Vector2(row.width, _theme.RowHeight),
                1f,
                new Color32(255, 255, 255, 255),
                UIHorizontalAlignment.Center,
                UIVerticalAlignment.Middle,
                new Vector3(0f, _theme.VerticalPadding));
        }

        private void CreateMessageRow()
        {
            UIPanel row = _theme.CreateRowContainer(this, "MessageRow");
            _messageLabel = _theme.CreateLabel(
                row,
                string.Empty,
                new Vector2(row.width - _theme.HorizontalPadding * 2f, _theme.RowHeight),
                0.8f,
                new Color32(200, 200, 200, 255),
                UIHorizontalAlignment.Center,
                UIVerticalAlignment.Middle,
                new Vector3(_theme.HorizontalPadding, _theme.VerticalPadding),
                wordWrap: true);
        }

        private void CreateActionRow()
        {
            UIPanel row = _theme.CreateRowContainer(this, "ActionRow");
            _actionButton = _theme.CreateButton(row, useDefaultSprites: false, text: string.Empty);
            _actionButton.textScale = 0.9f;
            _actionButton.size = new Vector2(
                row.width - _theme.HorizontalPadding * 2f,
                _theme.RowHeight - _theme.VerticalPadding * 2f);
            _actionButton.pivot = UIPivotPoint.TopLeft;
            _actionButton.relativePosition = new Vector3(
                _theme.HorizontalPadding,
                (row.height - _actionButton.height) * 0.5f);
            _actionButton.normalBgSprite = "LevelBarBackground";
            _actionButton.hoveredBgSprite = "LevelBarForeground";
            _actionButton.pressedBgSprite = "LevelBarForeground";
            _actionButton.eventClicked += (_, __) => HandleActionClicked();
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
                ? _theme.EnabledColor
                : _theme.DisabledColor;
        }
    }
}








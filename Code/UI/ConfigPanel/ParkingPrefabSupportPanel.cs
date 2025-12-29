using UnityEngine;
using ColossalFramework.UI;
using PickyParking.Domain;
using PickyParking.Infrastructure;
using PickyParking.ModEntry;

namespace PickyParking.UI
{
    public sealed class ParkingPrefabSupportPanel : UIPanel
    {
        private const float RowHeight = 32f;
        private const float HorizontalPadding = 10f;
        private const float VerticalPadding = 4f;

        private ushort _buildingId;
        private BuildingInfo _buildingInfo;
        private UILabel _messageLabel;
        private UIButton _actionButton;

        public override void Start()
        {
            base.Start();
            ConfigurePanel();
            BuildUi();
        }

        public void Bind(ushort buildingId, BuildingInfo info)
        {
            _buildingId = buildingId;
            _buildingInfo = info;
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
            title.size = new Vector2(row.width, RowHeight);
            title.textAlignment = UIHorizontalAlignment.Center;
            title.verticalAlignment = UIVerticalAlignment.Middle;
            title.relativePosition = new Vector3(0f, VerticalPadding);
        }

        private void CreateMessageRow()
        {
            UIPanel row = CreateRowContainer("MessageRow");
            _messageLabel = row.AddUIComponent<UILabel>();
            _messageLabel.textScale = 0.8f;
            _messageLabel.textColor = new Color32(200, 200, 200, 255);
            _messageLabel.autoSize = false;
            _messageLabel.size = new Vector2(row.width - HorizontalPadding * 2f, RowHeight);
            _messageLabel.textAlignment = UIHorizontalAlignment.Center;
            _messageLabel.verticalAlignment = UIVerticalAlignment.Middle;
            _messageLabel.relativePosition = new Vector3(HorizontalPadding, VerticalPadding);
            _messageLabel.wordWrap = true;
        }

        private void CreateActionRow()
        {
            UIPanel row = CreateRowContainer("ActionRow");
            _actionButton = row.AddUIComponent<UIButton>();
            _actionButton.textScale = 0.9f;
            _actionButton.size = new Vector2(row.width - HorizontalPadding * 2f, RowHeight - VerticalPadding * 2f);
            _actionButton.pivot = UIPivotPoint.TopLeft;
            _actionButton.relativePosition = new Vector3(
                HorizontalPadding,
                (row.height - _actionButton.height) * 0.5f);
            _actionButton.atlas = UIView.GetAView().defaultAtlas;
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
            row.height = RowHeight + VerticalPadding * 2f;
            row.autoLayout = false;
            return row;
        }

        private void Refresh()
        {
            if (_messageLabel == null || _actionButton == null)
                return;

            string prefabName = _buildingInfo != null ? _buildingInfo.name : "Unknown asset";

            _messageLabel.text = "Add Picky Parking to Asset " + prefabName + "?";
            _actionButton.text = "Add Picky Parking to asset";

            _actionButton.isEnabled = _buildingId != 0;
        }

        private void HandleActionClicked()
        {
            if (_buildingId == 0)
                return;

            TryAddSupportedPrefab();
        }

        private void TryAddSupportedPrefab()
        {
            if (_buildingInfo == null)
                return;

            ModRuntime runtime = ModRuntime.Current;
            if (runtime == null || runtime.PrefabIdentity == null || runtime.SupportedParkingLotRegistry == null)
                return;

            PrefabKey key = runtime.PrefabIdentity.CreateKey(_buildingInfo);
            bool added = runtime.SupportedParkingLotRegistry.Add(key);
            if (!added)
                return;

            if (runtime.SettingsController != null && runtime.SettingsController.Current != null)
            {
                runtime.SettingsController.Current.SupportedParkingLotPrefabs =
                    new System.Collections.Generic.List<PrefabKey>(runtime.SupportedParkingLotRegistry.EnumerateKeys());
                runtime.SettingsController.Save("UI.AddSupportedPrefab");
            }
            else
            {
                Log.Warn("[UI] Settings controller missing; prefab add not persisted.");
            }

            if (Log.IsVerboseEnabled)
                Log.Info("[UI] Added supported prefab " + key);
        }
    }
}

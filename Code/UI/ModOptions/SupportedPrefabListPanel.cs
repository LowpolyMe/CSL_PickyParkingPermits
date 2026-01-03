using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ColossalFramework;
using ColossalFramework.UI;
using PickyParking.Logging;
using PickyParking.Features.ParkingLotPrefabs;
using PickyParking.ModEntry;
using PickyParking.Settings;

namespace PickyParking.UI
{
    internal sealed class SupportedPrefabListPanel : UIPanel
    {
        private const float HeaderHeight = 28f;
        private const float RowHeight = 36f;
        private const float ListHeight = 220f;
        private const float ThumbnailSize = 28f;
        private const float ThumbnailBackgroundPadding = 4f;
        private const float HorizontalPadding = 8f;
        private const float VerticalPadding = 6f;
        private const float ColumnSpacing = 8f;
        private const float InstancesColumnWidth = 140f;
        private const float RemoveButtonWidth = 90f;
        private const float ScrollbarWidth = 12f;
        private const float ScrollbarGap = 2f;
        private static readonly Color32 RowHoverOutlineColor = new Color32(255, 255, 255, 140);

        private UIScrollablePanel _rowsPanel;
        private bool _showThumbnails;
        private bool _showInstances;
        private List<PrefabKey> _prefabKeys;
        private bool _refreshQueued;
        private ModSettings _settings;
        private Action _saveSettings;

        public override void Start()
        {
            base.Start();
            _showThumbnails = PrefabCollection<BuildingInfo>.LoadedCount() > 0;
            _showInstances = _showThumbnails
                && ModRuntime.Current != null
                && ModRuntime.Current.ParkingRulesConfigRegistry != null
                && Singleton<BuildingManager>.exists;
            ConfigurePanel();
            BuildUi();
            PopulateFromKeysImmediate();
            eventVisibilityChanged += (_, isVisible) =>
            {
                if (isVisible)
                    RequestRefresh();
            };
        }

        public UIPanel AddRow(PrefabKey key, string displayName, string instancesText)
        {
            if (_rowsPanel == null)
                return null;

            string prefabId = key.PrefabName;
            ColumnLayout layout = GetColumnLayout(_rowsPanel.width, _showThumbnails, _showInstances);
            UIPanel row = _rowsPanel.AddUIComponent<UIPanel>();
            row.name = "PrefabRow";
            row.width = _rowsPanel.width;
            row.height = RowHeight;
            row.autoLayout = false;

            UISprite hoverOutline = CreateRowHoverOutline(row);

            if (_showThumbnails)
                CreateThumbnail(row, prefabId, layout);

            UILabel nameLabel = row.AddUIComponent<UILabel>();
            nameLabel.text = displayName ?? prefabId ?? string.Empty;
            nameLabel.textScale = 0.8f;
            nameLabel.autoSize = false;
            nameLabel.size = new Vector2(layout.NameWidth, RowHeight);
            nameLabel.textAlignment = UIHorizontalAlignment.Left;
            nameLabel.verticalAlignment = UIVerticalAlignment.Middle;
            nameLabel.relativePosition = new Vector3(layout.NameX, 0f);

            if (_showInstances)
            {
                UILabel instancesLabel = row.AddUIComponent<UILabel>();
                instancesLabel.text = instancesText ?? string.Empty;
                instancesLabel.textScale = 0.8f;
                instancesLabel.autoSize = false;
                instancesLabel.size = new Vector2(layout.InstancesWidth, RowHeight);
                instancesLabel.textAlignment = UIHorizontalAlignment.Right;
                instancesLabel.verticalAlignment = UIVerticalAlignment.Middle;
                instancesLabel.relativePosition = new Vector3(layout.InstancesX, 0f);
            }

            UIButton removeButton = row.AddUIComponent<UIButton>();
            removeButton.text = "Remove";
            removeButton.textScale = 0.8f;
            removeButton.color = new Color32(100, 0, 0, 255);
            removeButton.hoveredColor = new Color32(255, 0, 0, 255);
            removeButton.atlas = UIView.GetAView().defaultAtlas;
            removeButton.normalBgSprite = "LevelBarBackground";
            removeButton.hoveredBgSprite = "LevelBarForeground";
            removeButton.pressedBgSprite = "LevelBarForeground";
            removeButton.size = new Vector2(layout.RemoveWidth, RowHeight - VerticalPadding * 2f);
            removeButton.relativePosition = new Vector3(
                layout.RemoveX,
                VerticalPadding);
            removeButton.eventMouseEnter += (_, __) => hoverOutline.isVisible = true;
            removeButton.eventMouseLeave += (_, __) => hoverOutline.isVisible = false;
            removeButton.eventClicked += (_, __) => HandleRemoveClicked(key, displayName);

            return row;
        }

        public void Bind(ModSettings settings, Action saveSettings)
        {
            _settings = settings;
            _saveSettings = saveSettings;
            _prefabKeys = settings != null ? settings.SupportedParkingLotPrefabs : null;
            PopulateFromKeysImmediate();
        }

        private void ConfigurePanel()
        {
            name = "SupportedPrefabListPanel";
            backgroundSprite = string.Empty;
            autoLayout = false;

            if (parent != null)
                width = parent.width - 50f;
            else
                width = 600f;

            height = HeaderHeight + ListHeight + VerticalPadding * 2f;
        }

        private void BuildUi()
        {
            float listWidth = width - ScrollbarWidth - ScrollbarGap;

            UIPanel headerRow = AddUIComponent<UIPanel>();
            headerRow.name = "HeaderRow";
            headerRow.size = new Vector2(listWidth, HeaderHeight);
            headerRow.relativePosition = new Vector3(0f, VerticalPadding);
            headerRow.atlas = UIView.GetAView().defaultAtlas;
            headerRow.backgroundSprite = "SubcategoriesPanel";
            headerRow.color = new Color32(50, 50, 50, 255);

            CreateHeaderLabels(headerRow, listWidth, _showThumbnails, _showInstances);

            _rowsPanel = AddUIComponent<UIScrollablePanel>();
            _rowsPanel.name = "RowsPanel";
            _rowsPanel.atlas = UIView.GetAView().defaultAtlas;
            _rowsPanel.backgroundSprite = "SubcategoriesPanel";
            _rowsPanel.color = new Color32(60, 60, 60, 255);
            _rowsPanel.size = new Vector2(listWidth, ListHeight);
            _rowsPanel.relativePosition = new Vector3(0f, HeaderHeight + VerticalPadding * 2f);
            _rowsPanel.autoLayout = true;
            _rowsPanel.autoLayoutDirection = LayoutDirection.Vertical;
            _rowsPanel.autoLayoutPadding = new RectOffset(0, 0, 2, 2);
            _rowsPanel.clipChildren = true;
            _rowsPanel.scrollWheelDirection = UIOrientation.Vertical;

            UIScrollbar scrollbar = CreateScrollbar(ListHeight);
            scrollbar.relativePosition = new Vector3(listWidth + ScrollbarGap, HeaderHeight + VerticalPadding * 2f);
            _rowsPanel.verticalScrollbar = scrollbar;
        }

        private void CreateHeaderLabels(UIPanel headerRow, float contentWidth, bool showThumbnails, bool showInstances)
        {
            ColumnLayout layout = GetColumnLayout(contentWidth, showThumbnails, showInstances);

            UILabel prefabLabel = headerRow.AddUIComponent<UILabel>();
            prefabLabel.text = "Prefab";
            prefabLabel.textScale = 0.8f;
            prefabLabel.autoSize = false;
            prefabLabel.size = new Vector2(layout.NameWidth, HeaderHeight);
            prefabLabel.textAlignment = UIHorizontalAlignment.Left;
            prefabLabel.verticalAlignment = UIVerticalAlignment.Middle;
            prefabLabel.relativePosition = new Vector3(layout.NameX, 0f);

            if (showInstances)
            {
                UILabel instancesLabel = headerRow.AddUIComponent<UILabel>();
                instancesLabel.text = "Rules in city";
                instancesLabel.textScale = 0.8f;
                instancesLabel.autoSize = false;
                instancesLabel.size = new Vector2(layout.InstancesWidth, HeaderHeight);
                instancesLabel.textAlignment = UIHorizontalAlignment.Right;
                instancesLabel.verticalAlignment = UIVerticalAlignment.Middle;
                instancesLabel.relativePosition = new Vector3(layout.InstancesX, 0f);
            }

            UILabel removeLabel = headerRow.AddUIComponent<UILabel>();
            removeLabel.text = "Remove";
            removeLabel.textScale = 0.8f;
            removeLabel.autoSize = false;
            removeLabel.size = new Vector2(layout.RemoveWidth, HeaderHeight);
            removeLabel.textAlignment = UIHorizontalAlignment.Center;
            removeLabel.verticalAlignment = UIVerticalAlignment.Middle;
            removeLabel.relativePosition = new Vector3(layout.RemoveX, 0f);
        }

        private UIScrollbar CreateScrollbar(float height)
        {
            UIScrollbar scrollbar = AddUIComponent<UIScrollbar>();
            scrollbar.name = "PrefabListScrollbar";
            scrollbar.width = ScrollbarWidth;
            scrollbar.height = height;
            scrollbar.orientation = UIOrientation.Vertical;
            scrollbar.minValue = 0f;
            scrollbar.value = 0f;
            scrollbar.incrementAmount = RowHeight;

            UISlicedSprite track = scrollbar.AddUIComponent<UISlicedSprite>();
            track.atlas = UIView.GetAView().defaultAtlas;
            track.spriteName = "ScrollbarTrack";
            track.relativePosition = Vector3.zero;
            track.size = new Vector2(ScrollbarWidth, height);
            scrollbar.trackObject = track;

            UISlicedSprite thumb = track.AddUIComponent<UISlicedSprite>();
            thumb.atlas = UIView.GetAView().defaultAtlas;
            thumb.spriteName = "ScrollbarThumb";
            thumb.relativePosition = Vector3.zero;
            thumb.size = new Vector2(ScrollbarWidth, 40f);
            scrollbar.thumbObject = thumb;

            return scrollbar;
        }

        private void CreateThumbnail(UIPanel row, string prefabId, ColumnLayout layout)
        {
            UISprite background = row.AddUIComponent<UISprite>();
            background.atlas = UIView.GetAView().defaultAtlas;
            background.spriteName = "GenericTab";
            float backgroundSize = ThumbnailSize + ThumbnailBackgroundPadding * 2f;
            background.size = new Vector2(backgroundSize, backgroundSize);
            background.relativePosition = new Vector3(
                layout.ThumbnailX - ThumbnailBackgroundPadding,
                (RowHeight - backgroundSize) * 0.5f);

            UITextureAtlas atlas;
            string spriteName;

            if (TryGetPrefabThumbnailSprite(prefabId, out atlas, out spriteName))
            {
                UISprite thumbnail = row.AddUIComponent<UISprite>();
                thumbnail.atlas = atlas;
                thumbnail.spriteName = spriteName;
                thumbnail.size = new Vector2(ThumbnailSize, ThumbnailSize);
                thumbnail.relativePosition = new Vector3(
                    layout.ThumbnailX,
                    (RowHeight - ThumbnailSize) * 0.5f);
                return;
            }

            UISprite fallback = row.AddUIComponent<UISprite>();
            fallback.atlas = UIView.GetAView().defaultAtlas;
            fallback.spriteName = "ThumbnailBuildingDefault";
            if (fallback.atlas == null || fallback.atlas[fallback.spriteName] == null)
                fallback.spriteName = "OptionBase";
            fallback.size = new Vector2(ThumbnailSize, ThumbnailSize);
            fallback.relativePosition = new Vector3(
                layout.ThumbnailX,
                (RowHeight - ThumbnailSize) * 0.5f);
        }

        private UISprite CreateRowHoverOutline(UIPanel row)
        {
            UISprite outline = row.AddUIComponent<UISprite>();
            outline.atlas = UIView.GetAView().defaultAtlas;
            outline.spriteName = "GenericTab";
            outline.size = new Vector2(row.width - 2f, row.height - 2f);
            outline.relativePosition = new Vector3(1f, 1f);
            outline.color = RowHoverOutlineColor;
            outline.isVisible = false;
            outline.isInteractive = false;
            outline.zOrder = 0;
            return outline;
        }

        private static bool TryGetPrefabThumbnailSprite(string prefabId, out UITextureAtlas atlas, out string spriteName)
        {
            string _ = null;
            return TryGetPrefabThumbnailSprite(prefabId, out atlas, out spriteName, out _);
        }

        private static bool TryGetPrefabThumbnailSprite(
            string prefabId,
            out UITextureAtlas atlas,
            out string spriteName,
            out string reason)
        {
            atlas = null;
            spriteName = null;
            reason = null;

            if (string.IsNullOrEmpty(prefabId))
            {
                reason = "prefab id is empty";
                return false;
            }

            BuildingInfo info = PrefabCollection<BuildingInfo>.FindLoaded(prefabId);
            if (info == null)
            {
                reason = "prefab not loaded";
                return false;
            }

            atlas = info.m_Atlas;
            spriteName = info.m_Thumbnail;
            if (atlas == null)
            {
                reason = "atlas is null";
                return false;
            }

            if (string.IsNullOrEmpty(spriteName))
            {
                reason = "thumbnail name empty";
                return false;
            }

            if (atlas[spriteName] == null)
            {
                reason = "sprite missing in atlas";
                return false;
            }

            return true;
        }

        private void HandleRemoveClicked(PrefabKey key, string displayName)
        {
            string name = !string.IsNullOrEmpty(displayName) ? displayName : key.PrefabName;
            string title = "Remove supported prefab?";
            string message = "Remove " + name + " from supported prefabs?";

            ConfirmPanel.ShowModal(
                title,
                message,
                (_, result) =>
                {
                    if (result != 1)
                        return;

                    if (!RemoveSupportedPrefab(key))
                        return;

                    RequestRefresh();
                });
        }

        private void PopulateFromKeysImmediate()
        {
            if (_rowsPanel == null)
                return;

            if (EnsureContextFresh())
                RebuildUi();
            if (Log.IsVerboseEnabled && Log.IsUiDebugEnabled)
                Log.Info("[UI] Populate list start: showThumbnails=" + _showThumbnails + " showInstances=" + _showInstances);

            ClearRows();

            if (_prefabKeys == null || _prefabKeys.Count == 0)
                return;

            Dictionary<string, int> rulesByPrefabName = null;
            bool hasCounts = _showInstances && TryBuildRulesCountByPrefabName(out rulesByPrefabName);
            if (Log.IsVerboseEnabled && Log.IsUiDebugEnabled && !hasCounts)
            {
                string reason = _showInstances
                    ? "registry or building manager unavailable"
                    : "instances hidden (not in level)";
                Log.Info("[UI] Instances column disabled: " + reason);
            }

            for (int i = 0; i < _prefabKeys.Count; i++)
            {
                PrefabKey key = _prefabKeys[i];
                string prefabName = key.PrefabName;
                string displayName = GetDisplayName(prefabName);
                int ruleCount = 0;
                if (hasCounts && !string.IsNullOrEmpty(prefabName) && rulesByPrefabName != null)
                    rulesByPrefabName.TryGetValue(prefabName, out ruleCount);

                string instancesText = hasCounts ? FormatRulesCount(ruleCount) : string.Empty;
                AddRow(key, displayName, instancesText);
            }
        }

        private void ClearRows()
        {
            if (_rowsPanel == null)
                return;

            int count = _rowsPanel.components.Count;
            for (int i = count - 1; i >= 0; i--)
            {
                var component = _rowsPanel.components[i];
                if (component != null)
                    Destroy(component.gameObject);
            }
        }

        private static bool TryBuildRulesCountByPrefabName(out Dictionary<string, int> counts)
        {
            counts = null;

            ModRuntime runtime = ModRuntime.Current;
            if (runtime == null || runtime.ParkingRulesConfigRegistry == null)
            {
                if (Log.IsVerboseEnabled && Log.IsUiDebugEnabled)
                    Log.Info("[UI] Rules count build skipped: runtime or registry missing.");
                return false;
            }

            if (!Singleton<BuildingManager>.exists)
            {
                if (Log.IsVerboseEnabled && Log.IsUiDebugEnabled)
                    Log.Info("[UI] Rules count build skipped: BuildingManager missing.");
                return false;
            }

            var map = new Dictionary<string, int>();
            int totalRules = 0;
            foreach (var pair in runtime.ParkingRulesConfigRegistry.Enumerate())
            {
                ushort buildingId = pair.Key;
                if (buildingId == 0)
                    continue;

                BuildingInfo info = Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingId].Info;
                if (info == null)
                    continue;

                string prefabName = info.name;
                if (string.IsNullOrEmpty(prefabName))
                    continue;

                int count;
                map.TryGetValue(prefabName, out count);
                map[prefabName] = count + 1;
                totalRules++;
            }

            counts = map;
            if (Log.IsVerboseEnabled && Log.IsUiDebugEnabled)
                Log.Info("[UI] Rules count build ok: rules=" + totalRules + " prefabs=" + map.Count);
            return true;
        }

        private static string FormatRulesCount(int count)
        {
            return count == 1 ? "1 rule" : count + " rules";
        }

        private string GetDisplayName(string prefabId)
        {
            if (string.IsNullOrEmpty(prefabId))
                return string.Empty;

            if (!_showThumbnails)
                return prefabId;

            BuildingInfo info = PrefabCollection<BuildingInfo>.FindLoaded(prefabId);
            if (info == null)
                return prefabId;

            string name = info.GetLocalizedTitle();
            return string.IsNullOrEmpty(name) ? prefabId : name;
        }

        private bool RemoveSupportedPrefab(PrefabKey key)
        {
            bool removed = false;

            if (_prefabKeys != null)
                removed = _prefabKeys.Remove(key);

            ModRuntime runtime = ModRuntime.Current;
            if (runtime != null && runtime.SupportedParkingLotRegistry != null)
                runtime.SupportedParkingLotRegistry.Remove(key);

            if (removed && _saveSettings != null)
                _saveSettings();

            if (removed && Log.IsVerboseEnabled && Log.IsUiDebugEnabled)
                Log.Info("[UI] Removed supported prefab " + key);

            return removed;
        }

        private bool EnsureContextFresh()
        {
            bool previousThumbnails = _showThumbnails;
            bool previousInstances = _showInstances;

            _showThumbnails = PrefabCollection<BuildingInfo>.LoadedCount() > 0;
            _showInstances = _showThumbnails
                && ModRuntime.Current != null
                && ModRuntime.Current.ParkingRulesConfigRegistry != null
                && Singleton<BuildingManager>.exists;

            return previousThumbnails != _showThumbnails || previousInstances != _showInstances;
        }

        private void RebuildUi()
        {
            ClearPanelChildren();
            _rowsPanel = null;
            BuildUi();
        }

        private void ClearPanelChildren()
        {
            int count = components.Count;
            for (int i = count - 1; i >= 0; i--)
            {
                UIComponent component = components[i];
                if (component != null)
                    Destroy(component.gameObject);
            }
        }

        private void RequestRefresh()
        {
            if (_refreshQueued)
                return;

            _refreshQueued = true;
            UIView view = UIView.GetAView();
            if (view != null)
            {
                view.StartCoroutine(RefreshNextFrame());
            }
            else
            {
                _refreshQueued = false;
                PopulateFromKeysImmediate();
            }
        }

        private IEnumerator RefreshNextFrame()
        {
            yield return null;
            _refreshQueued = false;
            PopulateFromKeysImmediate();
        }

        private ColumnLayout GetColumnLayout(float contentWidth, bool showThumbnails, bool showInstances)
        {
            float thumbnailColumn = showThumbnails ? ThumbnailSize + ColumnSpacing : 0f;
            float instancesColumn = showInstances ? InstancesColumnWidth + ColumnSpacing : 0f;
            float nameWidth = contentWidth - (HorizontalPadding * 2f + thumbnailColumn + instancesColumn + RemoveButtonWidth + ColumnSpacing * 2f);
            if (nameWidth < 120f)
                nameWidth = 120f;

            float x = HorizontalPadding;
            var layout = new ColumnLayout();
            layout.ThumbnailX = x;
            layout.ThumbnailWidth = ThumbnailSize;

            if (showThumbnails)
                x += ThumbnailSize + ColumnSpacing;
            layout.NameX = x;
            layout.NameWidth = nameWidth;

            x += nameWidth + ColumnSpacing;
            if (showInstances)
            {
                layout.InstancesX = x;
                layout.InstancesWidth = InstancesColumnWidth;
                x += InstancesColumnWidth + ColumnSpacing;
            }
            else
            {
                layout.InstancesX = 0f;
                layout.InstancesWidth = 0f;
            }
            layout.RemoveX = x;
            layout.RemoveWidth = RemoveButtonWidth;

            return layout;
        }

        private struct ColumnLayout
        {
            public float ThumbnailX;
            public float ThumbnailWidth;
            public float NameX;
            public float NameWidth;
            public float InstancesX;
            public float InstancesWidth;
            public float RemoveX;
            public float RemoveWidth;
        }
    }
}

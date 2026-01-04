using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ColossalFramework;
using ColossalFramework.UI;
using PickyParking.Logging;
using PickyParking.Features.ParkingLotPrefabs;
using PickyParking.Settings;

namespace PickyParking.UI
{
    internal sealed class SupportedPrefabListPanel : UIPanel
    {
        private struct LabelOptions
        {
            public string Text;
            public float Width;
            public float Height;
            public UIHorizontalAlignment Alignment;
            public float X;
        }

        private UIScrollablePanel _rowsPanel;
        private bool _showThumbnails;
        private bool _showInstances;
        private List<PrefabKey> _prefabKeys;
        private bool _refreshQueued;
        private Action _saveSettings;
        private UITextureAtlas _defaultAtlas;
        private UiServices _services;
        private bool _started;

        public void Initialize(UiServices services)
        {
            _services = services;
            if (_started)
                RequestRefreshNextFrame();
        }

        public override void Start()
        {
            base.Start();
            _started = true;
            RefreshVisibilityFlags();
            _defaultAtlas = GetDefaultAtlas();
            ConfigureRootPanel();
            BuildPanelUi();
            PopulateRows();
            eventVisibilityChanged += (_, isVisible) =>
            {
                if (isVisible)
                    RequestRefreshNextFrame();
            };
        }

        public UIPanel AddRow(PrefabKey key, string displayName, string instancesText)
        {
            if (_rowsPanel == null)
                return null;

            string prefabId = key.PrefabName;
            ColumnLayout layout = BuildColumnLayout(_rowsPanel.width, _showThumbnails, _showInstances);
            UIPanel row = _rowsPanel.AddUIComponent<UIPanel>();
            row.name = "PrefabRow";
            row.width = _rowsPanel.width;
            row.height = ModOptionsUiValues.SupportedPrefabList.RowHeight;
            row.autoLayout = false;

            UISprite hoverOutline = CreateRowHoverOutline(row);

            if (_showThumbnails)
                CreateThumbnail(row, prefabId, layout);

            AddLabel(row, new LabelOptions
            {
                Text = displayName ?? prefabId ?? string.Empty,
                Width = layout.NameWidth,
                Height = ModOptionsUiValues.SupportedPrefabList.RowHeight,
                Alignment = UIHorizontalAlignment.Left,
                X = layout.NameX
            });

            if (_showInstances)
                AddLabel(row, new LabelOptions
                {
                    Text = instancesText ?? string.Empty,
                    Width = layout.InstancesWidth,
                Height = ModOptionsUiValues.SupportedPrefabList.RowHeight,
                    Alignment = UIHorizontalAlignment.Right,
                    X = layout.InstancesX
                });

            UIButton removeButton = row.AddUIComponent<UIButton>();
            removeButton.text = "Remove";
            removeButton.textScale = ModOptionsUiValues.SupportedPrefabList.LabelTextScale;
            removeButton.color = new Color32(100, 0, 0, 255);
            removeButton.hoveredColor = new Color32(255, 0, 0, 255);
            removeButton.atlas = GetAtlas();
            removeButton.normalBgSprite = "LevelBarBackground";
            removeButton.hoveredBgSprite = "LevelBarForeground";
            removeButton.pressedBgSprite = "LevelBarForeground";
            removeButton.size = new Vector2(
                layout.RemoveWidth,
                ModOptionsUiValues.SupportedPrefabList.RowHeight - ModOptionsUiValues.SupportedPrefabList.VerticalPadding * 2f);
            removeButton.relativePosition = new Vector3(
                layout.RemoveX,
                ModOptionsUiValues.SupportedPrefabList.VerticalPadding);
            removeButton.eventMouseEnter += (_, __) => hoverOutline.isVisible = true;
            removeButton.eventMouseLeave += (_, __) => hoverOutline.isVisible = false;
            removeButton.eventClicked += (_, __) => HandleRemoveClicked(key, displayName);

            return row;
        }

        public void Bind(ModSettings settings, Action saveSettings)
        {
            _saveSettings = saveSettings;
            _prefabKeys = settings != null ? settings.SupportedParkingLotPrefabs : null;
            PopulateRows();
            if (_started && isVisible)
                RequestRefreshNextFrame();
        }

        private void ConfigureRootPanel()
        {
            name = "SupportedPrefabListPanel";
            backgroundSprite = string.Empty;
            autoLayout = false;

            if (parent != null)
                width = parent.width - ModOptionsUiValues.OptionsPanel.WidthOffset;
            else
                width = ModOptionsUiValues.OptionsPanel.DefaultWidth;

            height = ModOptionsUiValues.SupportedPrefabList.HeaderHeight
                + ModOptionsUiValues.SupportedPrefabList.ListHeight
                + ModOptionsUiValues.SupportedPrefabList.VerticalPadding * 2f;
        }

        private void BuildPanelUi()
        {
            float listWidth = width
                - ModOptionsUiValues.SupportedPrefabList.ScrollbarWidth
                - ModOptionsUiValues.SupportedPrefabList.ScrollbarGap;

            UIPanel headerRow = AddUIComponent<UIPanel>();
            headerRow.name = "HeaderRow";
            headerRow.size = new Vector2(listWidth, ModOptionsUiValues.SupportedPrefabList.HeaderHeight);
            headerRow.relativePosition = new Vector3(0f, ModOptionsUiValues.SupportedPrefabList.VerticalPadding);
            headerRow.atlas = GetAtlas();
            headerRow.backgroundSprite = "SubcategoriesPanel";
            headerRow.color = new Color32(50, 50, 50, 255);

            CreateHeaderLabels(headerRow, listWidth, _showThumbnails, _showInstances);

            _rowsPanel = AddUIComponent<UIScrollablePanel>();
            _rowsPanel.name = "RowsPanel";
            _rowsPanel.atlas = GetAtlas();
            _rowsPanel.backgroundSprite = "SubcategoriesPanel";
            _rowsPanel.color = new Color32(60, 60, 60, 255);
            _rowsPanel.size = new Vector2(listWidth, ModOptionsUiValues.SupportedPrefabList.ListHeight);
            _rowsPanel.relativePosition = new Vector3(
                0f,
                ModOptionsUiValues.SupportedPrefabList.HeaderHeight
                + ModOptionsUiValues.SupportedPrefabList.VerticalPadding * 2f);
            _rowsPanel.autoLayout = true;
            _rowsPanel.autoLayoutDirection = LayoutDirection.Vertical;
            _rowsPanel.autoLayoutPadding = new RectOffset(
                0,
                0,
                ModOptionsUiValues.SupportedPrefabList.RowsPanelVerticalPadding,
                ModOptionsUiValues.SupportedPrefabList.RowsPanelVerticalPadding);
            _rowsPanel.clipChildren = true;
            _rowsPanel.scrollWheelDirection = UIOrientation.Vertical;

            UIScrollbar scrollbar = CreateScrollbar(ModOptionsUiValues.SupportedPrefabList.ListHeight);
            scrollbar.relativePosition = new Vector3(
                listWidth + ModOptionsUiValues.SupportedPrefabList.ScrollbarGap,
                ModOptionsUiValues.SupportedPrefabList.HeaderHeight
                + ModOptionsUiValues.SupportedPrefabList.VerticalPadding * 2f);
            _rowsPanel.verticalScrollbar = scrollbar;
        }

        private void CreateHeaderLabels(UIPanel headerRow, float contentWidth, bool showThumbnails, bool showInstances)
        {
            ColumnLayout layout = BuildColumnLayout(contentWidth, showThumbnails, showInstances);

            AddLabel(headerRow, new LabelOptions
            {
                Text = "Prefab",
                Width = layout.NameWidth,
                Height = ModOptionsUiValues.SupportedPrefabList.HeaderHeight,
                Alignment = UIHorizontalAlignment.Left,
                X = layout.NameX
            });

            if (showInstances)
                AddLabel(headerRow, new LabelOptions
                {
                    Text = "Rules in city",
                    Width = layout.InstancesWidth,
                    Height = ModOptionsUiValues.SupportedPrefabList.HeaderHeight,
                    Alignment = UIHorizontalAlignment.Right,
                    X = layout.InstancesX
                });

            AddLabel(headerRow, new LabelOptions
            {
                Text = "Remove",
                Width = layout.RemoveWidth,
                Height = ModOptionsUiValues.SupportedPrefabList.HeaderHeight,
                Alignment = UIHorizontalAlignment.Center,
                X = layout.RemoveX
            });
        }

        private UIScrollbar CreateScrollbar(float height)
        {
            UIScrollbar scrollbar = AddUIComponent<UIScrollbar>();
            scrollbar.name = "PrefabListScrollbar";
            scrollbar.width = ModOptionsUiValues.SupportedPrefabList.ScrollbarWidth;
            scrollbar.height = height;
            scrollbar.orientation = UIOrientation.Vertical;
            scrollbar.minValue = 0f;
            scrollbar.value = 0f;
            scrollbar.incrementAmount = ModOptionsUiValues.SupportedPrefabList.RowHeight;

            UISlicedSprite track = scrollbar.AddUIComponent<UISlicedSprite>();
            track.atlas = GetAtlas();
            track.spriteName = "ScrollbarTrack";
            track.relativePosition = Vector3.zero;
            track.size = new Vector2(ModOptionsUiValues.SupportedPrefabList.ScrollbarWidth, height);
            scrollbar.trackObject = track;

            UISlicedSprite thumb = track.AddUIComponent<UISlicedSprite>();
            thumb.atlas = GetAtlas();
            thumb.spriteName = "ScrollbarThumb";
            thumb.relativePosition = Vector3.zero;
            thumb.size = new Vector2(
                ModOptionsUiValues.SupportedPrefabList.ScrollbarWidth,
                ModOptionsUiValues.SupportedPrefabList.ScrollbarThumbHeight);
            scrollbar.thumbObject = thumb;

            return scrollbar;
        }

        private void CreateThumbnail(UIPanel row, string prefabId, ColumnLayout layout)
        {
            UISprite background = row.AddUIComponent<UISprite>();
            background.atlas = GetAtlas();
            background.spriteName = "GenericTab";
            float backgroundSize = ModOptionsUiValues.SupportedPrefabList.ThumbnailSize
                + ModOptionsUiValues.SupportedPrefabList.ThumbnailBackgroundPadding * 2f;
            background.size = new Vector2(backgroundSize, backgroundSize);
            background.relativePosition = new Vector3(
                layout.ThumbnailX - ModOptionsUiValues.SupportedPrefabList.ThumbnailBackgroundPadding,
                (ModOptionsUiValues.SupportedPrefabList.RowHeight - backgroundSize) * 0.5f);

            UITextureAtlas atlas;
            string spriteName;

            if (TryGetPrefabThumbnailSprite(prefabId, out atlas, out spriteName))
            {
                UISprite thumbnail = row.AddUIComponent<UISprite>();
                thumbnail.atlas = atlas;
                thumbnail.spriteName = spriteName;
                thumbnail.size = new Vector2(
                    ModOptionsUiValues.SupportedPrefabList.ThumbnailSize,
                    ModOptionsUiValues.SupportedPrefabList.ThumbnailSize);
                thumbnail.relativePosition = new Vector3(
                    layout.ThumbnailX,
                    (ModOptionsUiValues.SupportedPrefabList.RowHeight - ModOptionsUiValues.SupportedPrefabList.ThumbnailSize) * 0.5f);
                return;
            }

            UISprite fallback = row.AddUIComponent<UISprite>();
            fallback.atlas = GetAtlas();
            fallback.spriteName = "ThumbnailBuildingDefault";
            if (fallback.atlas == null || fallback.atlas[fallback.spriteName] == null)
                fallback.spriteName = "OptionBase";
            fallback.size = new Vector2(
                ModOptionsUiValues.SupportedPrefabList.ThumbnailSize,
                ModOptionsUiValues.SupportedPrefabList.ThumbnailSize);
            fallback.relativePosition = new Vector3(
                layout.ThumbnailX,
                (ModOptionsUiValues.SupportedPrefabList.RowHeight - ModOptionsUiValues.SupportedPrefabList.ThumbnailSize) * 0.5f);
        }

        private UISprite CreateRowHoverOutline(UIPanel row)
        {
            UISprite outline = row.AddUIComponent<UISprite>();
            outline.atlas = GetAtlas();
            outline.spriteName = "GenericTab";
            outline.size = new Vector2(
                row.width - ModOptionsUiValues.SupportedPrefabList.HoverOutlineInset * 2f,
                row.height - ModOptionsUiValues.SupportedPrefabList.HoverOutlineInset * 2f);
            outline.relativePosition = new Vector3(
                ModOptionsUiValues.SupportedPrefabList.HoverOutlineInset,
                ModOptionsUiValues.SupportedPrefabList.HoverOutlineInset);
            outline.color = ModOptionsUiValues.SupportedPrefabList.RowHoverOutlineColor;
            outline.isVisible = false;
            outline.isInteractive = false;
            outline.zOrder = 0;
            return outline;
        }

        private static bool TryGetPrefabThumbnailSprite(string prefabId, out UITextureAtlas atlas, out string spriteName)
        {
            string unusedReason;
            return TryGetPrefabThumbnailSprite(prefabId, out atlas, out spriteName, out unusedReason);
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

                    RequestRefreshNextFrame();
                });
        }

        private void PopulateRows()
        {
            if (_rowsPanel == null)
                return;

            if (RefreshVisibilityFlags())
                RebuildPanelUi();
            if (Log.IsVerboseEnabled && Log.IsUiDebugEnabled)
                Log.Info("[UI] Populate list start: showThumbnails=" + _showThumbnails + " showInstances=" + _showInstances);

            ClearRowComponents();

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
                string displayName = GetPrefabDisplayName(prefabName);
                int ruleCount = 0;
                if (hasCounts && !string.IsNullOrEmpty(prefabName) && rulesByPrefabName != null)
                    rulesByPrefabName.TryGetValue(prefabName, out ruleCount);

                string instancesText = hasCounts ? FormatRulesCount(ruleCount) : string.Empty;
                AddRow(key, displayName, instancesText);
            }
        }

        private void ClearRowComponents()
        {
            if (_rowsPanel == null)
                return;

            int count = _rowsPanel.components.Count;
            for (int i = count - 1; i >= 0; i--)
            {
                var component = _rowsPanel.components[i];
                DisableAndDestroy(component);
            }
        }

        private bool TryBuildRulesCountByPrefabName(out Dictionary<string, int> counts)
        {
            counts = null;

            if (_services == null || _services.ParkingRulesConfigRegistry == null)
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
            foreach (var pair in _services.ParkingRulesConfigRegistry.Enumerate())
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

        private string GetPrefabDisplayName(string prefabId)
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

            if (_services != null && _services.SupportedParkingLotRegistry != null)
                _services.SupportedParkingLotRegistry.Remove(key);

            if (removed && _saveSettings != null)
                _saveSettings();

            if (removed && Log.IsVerboseEnabled && Log.IsUiDebugEnabled)
                Log.Info("[UI] Removed supported prefab " + key);

            return removed;
        }

        private bool RefreshVisibilityFlags()
        {
            bool previousThumbnails = _showThumbnails;
            bool previousInstances = _showInstances;

            _showThumbnails = ShouldShowThumbnails();
            _showInstances = ShouldShowInstances(_showThumbnails);

            return previousThumbnails != _showThumbnails || previousInstances != _showInstances;
        }

        private static bool ShouldShowThumbnails()
        {
            return PrefabCollection<BuildingInfo>.LoadedCount() > 0;
        }

        private bool ShouldShowInstances(bool showThumbnails)
        {
            return showThumbnails
                && _services != null
                && _services.ParkingRulesConfigRegistry != null
                && Singleton<BuildingManager>.exists;
        }

        private void RebuildPanelUi()
        {
            ClearPanelChildren();
            _rowsPanel = null;
            BuildPanelUi();
        }

        private void ClearPanelChildren()
        {
            int count = components.Count;
            for (int i = count - 1; i >= 0; i--)
            {
                UIComponent component = components[i];
                DisableAndDestroy(component);
            }
        }

        private void RequestRefreshNextFrame()
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
                PopulateRows();
            }
        }

        private IEnumerator RefreshNextFrame()
        {
            yield return null;
            _refreshQueued = false;
            PopulateRows();
        }

        private ColumnLayout BuildColumnLayout(float contentWidth, bool showThumbnails, bool showInstances)
        {
            float thumbnailColumn = showThumbnails
                ? ModOptionsUiValues.SupportedPrefabList.ThumbnailSize + ModOptionsUiValues.SupportedPrefabList.ColumnSpacing
                : 0f;
            float instancesColumn = showInstances
                ? ModOptionsUiValues.SupportedPrefabList.InstancesColumnWidth + ModOptionsUiValues.SupportedPrefabList.ColumnSpacing
                : 0f;
            float nameWidth = contentWidth - (ModOptionsUiValues.SupportedPrefabList.HorizontalPadding * 2f
                + thumbnailColumn
                + instancesColumn
                + ModOptionsUiValues.SupportedPrefabList.RemoveButtonWidth
                + ModOptionsUiValues.SupportedPrefabList.ColumnSpacing * 2f);
            if (nameWidth < ModOptionsUiValues.SupportedPrefabList.MinNameWidth)
                nameWidth = ModOptionsUiValues.SupportedPrefabList.MinNameWidth;

            float x = ModOptionsUiValues.SupportedPrefabList.HorizontalPadding;
            var layout = new ColumnLayout();
            layout.ThumbnailX = x;
            layout.ThumbnailWidth = ModOptionsUiValues.SupportedPrefabList.ThumbnailSize;

            if (showThumbnails)
                x += ModOptionsUiValues.SupportedPrefabList.ThumbnailSize + ModOptionsUiValues.SupportedPrefabList.ColumnSpacing;
            layout.NameX = x;
            layout.NameWidth = nameWidth;

            x += nameWidth + ModOptionsUiValues.SupportedPrefabList.ColumnSpacing;
            if (showInstances)
            {
                layout.InstancesX = x;
                layout.InstancesWidth = ModOptionsUiValues.SupportedPrefabList.InstancesColumnWidth;
                x += ModOptionsUiValues.SupportedPrefabList.InstancesColumnWidth + ModOptionsUiValues.SupportedPrefabList.ColumnSpacing;
            }
            else
            {
                layout.InstancesX = 0f;
                layout.InstancesWidth = 0f;
            }
            layout.RemoveX = x;
            layout.RemoveWidth = ModOptionsUiValues.SupportedPrefabList.RemoveButtonWidth;

            return layout;
        }

        private static UILabel AddLabel(UIPanel parent, LabelOptions options)
        {
            UILabel label = parent.AddUIComponent<UILabel>();
            label.text = options.Text ?? string.Empty;
            label.textScale = ModOptionsUiValues.SupportedPrefabList.LabelTextScale;
            label.autoSize = false;
            label.size = new Vector2(options.Width, options.Height);
            label.textAlignment = options.Alignment;
            label.verticalAlignment = UIVerticalAlignment.Middle;
            label.relativePosition = new Vector3(options.X, 0f);
            return label;
        }

        private UITextureAtlas GetAtlas()
        {
            if (_defaultAtlas != null)
                return _defaultAtlas;

            _defaultAtlas = GetDefaultAtlas();
            return _defaultAtlas;
        }

        private static UITextureAtlas GetDefaultAtlas()
        {
            UIView view = UIView.GetAView();
            return view != null ? view.defaultAtlas : null;
        }

        private static void DisableAndDestroy(UIComponent component)
        {
            if (component == null)
                return;

            component.isVisible = false;
            component.isInteractive = false;
            component.isEnabled = false;
            Destroy(component.gameObject);
        }

    }
}

using UnityEngine;

namespace PickyParking.UI.ModOptions
{
    internal static class ModOptionsUiValues
    {
        internal static class OptionsPanel
        {
            public const float WidthOffset = 50f;
            public const float DefaultWidth = 600f;
        }

        internal static class SupportedPrefabList
        {
            public const float HeaderHeight = 28f;
            public const float RowHeight = 36f;
            public const float ListHeight = 220f;
            public const float ThumbnailSize = 28f;
            public const float ThumbnailBackgroundPadding = 4f;
            public const float HorizontalPadding = 8f;
            public const float VerticalPadding = 6f;
            public const float ColumnSpacing = 8f;
            public const float InstancesColumnWidth = 140f;
            public const float RemoveButtonWidth = 90f;
            public const float ScrollbarWidth = 12f;
            public const float ScrollbarGap = 2f;
            public const float MinNameWidth = 120f;
            public const float HoverOutlineInset = 1f;
            public const float LabelTextScale = 0.8f;
            public const int RowsPanelVerticalPadding = 2;
            public const float ScrollbarThumbHeight = 40f;
            public static readonly Color32 RowHoverOutlineColor = new Color32(255, 255, 255, 140);
        }

        internal static class HueSliders
        {
            public const float Min = 0f;
            public const float Max = 1f;
            public const float Step = 0.01f;
        }
    }
}


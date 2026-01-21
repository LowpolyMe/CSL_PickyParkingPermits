using UnityEngine;

namespace PickyParking.UI.BuildingOptionsPanel
{
    internal static class BuildingOptionsPanelUiValues
    {
        internal static class AttachPanel
        {
            public const string CityServicePanelName = "CityServiceWorldInfoPanel";
            public const string CityServicePanelLibraryName = "(Library) CityServiceWorldInfoPanel";
            public const string WrapperContainerPath = "Wrapper";
            public const float InjectionRetrySeconds = 1f;
            public const float WrapperPadding = 25f;
        }

        internal static class RulesPanel
        {
            public const float SliderAllThreshold = 0.99f;
            public const ushort DefaultNewRuleRadiusMeters = 500;
            public const float ParkingStatsUpdateIntervalSeconds = 0.5f;
        }

        internal static class PanelTheme
        {
            public const float HeaderTextScale = 1f;
            public const float ParkingStatsTextScale = 0.75f;
            
            public const float RowHeight = 35f;
            public const float HorizontalPadding = 10f;
            public const float VerticalPadding = 2f;
            
            public const float SliderHeight = 15f;
            public const float MinSliderWidth = 10f;
            public const float SliderThumbSize = 16f;
            
            public const float MinIconSize = 18f;
            public const float IconScale = 0.72f;
            public const float MinButtonHeight = 18f;



            public const float ValueLabelWidth = 44f;
            public const float ValueLabelHeight = 15f;
            public const float ValueLabelTextScale = 0.7f;
            
            public const float ToggleTextScale = 0.7f;
            public const float RestrictionsToggleTextScale = 0.85f;
            public const float ApplyButtonTextScale = 1f;
            public const float PanelRowCount = 7f;
            public const float PanelExtraHeight = 30f;
            public const float EnabledOpacity = 1f;
            public const float DisabledOpacity = 0.7f;
            
            public const float FooterIconEnabledAlpha = 0.6f;
            public const float FooterIconHoverAlpha = 0.8f;
            public const float FooterIconPressedAlpha = 1f;
            public const float FooterIconDisabledAlpha = 0.3f;

            public const float DefaultResidentsHue = 0.35f;
            public const float DefaultWorkSchoolHue = 0.1f;

            public static readonly Color ThumbColor = Color.white;
            public static readonly Color32 EnabledColor = new Color32(255, 255, 255, 255);
            public static readonly Color32 DisabledColor = new Color32(150, 150, 150, 255);
            public static readonly Color32 ValueLabelColor = new Color32(185, 221, 254, 255);
            public static readonly Color32 SliderTrackColor = new Color32(150, 150, 150, 255);
            
            
            public const string ButtonsNormalBgSprite = "LevelBarBackground";
            public const string ButtonsHoveredBgSprite = "LevelBarForeground";
            public const string ButtonsPressedBgSprite = "LevelBarForeground";
            public const string ButtonsDisabledBgSprite = "LevelBarDisabled";
        }
        

        internal static class DistanceDisplay
        {
            public const string InfiniteLabel = "\u221E";
        }

        internal static class DistanceSliderMapping
        {
            public const float MidPoint = 0.5f;
        }
    }
}







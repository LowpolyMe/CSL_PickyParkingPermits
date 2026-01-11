using ColossalFramework.UI;
using UnityEngine;

namespace PickyParking.UI.BuildingOptionsPanel.ParkingRulesPanel
{
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
}







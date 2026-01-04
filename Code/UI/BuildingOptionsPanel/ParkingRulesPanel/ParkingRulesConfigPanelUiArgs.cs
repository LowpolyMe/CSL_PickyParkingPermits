using System;

namespace PickyParking.UI.BuildingOptionsPanel.ParkingRulesPanel
{
    internal sealed class ParkingRulesConfigPanelUiArgs
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
    }
}







using System;

namespace PickyParking.UI.BuildingOptionsPanel.ParkingRulesPanel
{
    internal sealed class SliderRowRuleArgs
    {
        public ParkingRulesSliderRow Row { get; set; }
        public bool Enabled { get; set; }
        public ushort RadiusMeters { get; set; }
        public Func<ushort, float> ConvertRadiusToSliderValue { get; set; }
        public Action<ParkingRulesSliderRow, float> SetSliderValue { get; set; }
    }
}







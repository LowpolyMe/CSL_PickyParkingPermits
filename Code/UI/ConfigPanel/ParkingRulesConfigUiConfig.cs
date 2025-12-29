using PickyParking.Features.ParkingRules;

namespace PickyParking.UI
{
    public sealed class ParkingRulesConfigUiConfig
    {
        public static readonly ParkingRulesConfigUiConfig Default =
            new ParkingRulesConfigUiConfig(
                sliderMinValue: 0f,
                sliderMaxValue: 1f,
                sliderStep: 0.005f,
                distanceSliderMinValue: 0.01f,
                distanceSliderMaxValue: 0.95f,
                minDistanceMeters: ParkingRulesLimits.MinRadiusMeters,
                midDistanceMeters: ParkingRulesLimits.MidRadiusMeters,
                maxDistanceMeters: ParkingRulesLimits.MaxRadiusMeters,
                distanceMidpointT: 0.74f,
                defaultRadiusMeters: ParkingRulesLimits.DefaultRadiusMeters);

        public float SliderMinValue { get; private set; }
        public float SliderMaxValue { get; private set; }
        public float SliderStep { get; private set; }
        public float DistanceSliderMinValue { get; private set; }
        public float DistanceSliderMaxValue { get; private set; }
        public ushort MinDistanceMeters { get; private set; }
        public ushort MidDistanceMeters { get; private set; }
        public ushort MaxDistanceMeters { get; private set; }
        public float DistanceMidpointT { get; private set; }
        public ushort DefaultRadiusMeters { get; private set; }

        public ParkingRulesConfigUiConfig(
            float sliderMinValue,
            float sliderMaxValue,
            float sliderStep,
            float distanceSliderMinValue,
            float distanceSliderMaxValue,
            ushort minDistanceMeters,
            ushort midDistanceMeters,
            ushort maxDistanceMeters,
            float distanceMidpointT,
            ushort defaultRadiusMeters)
        {
            SliderMinValue = sliderMinValue;
            SliderMaxValue = sliderMaxValue;
            SliderStep = sliderStep;
            DistanceSliderMinValue = distanceSliderMinValue;
            DistanceSliderMaxValue = distanceSliderMaxValue;
            MinDistanceMeters = minDistanceMeters;
            MidDistanceMeters = midDistanceMeters;
            MaxDistanceMeters = maxDistanceMeters;
            DistanceMidpointT = distanceMidpointT;
            DefaultRadiusMeters = defaultRadiusMeters;
        }
    }
}

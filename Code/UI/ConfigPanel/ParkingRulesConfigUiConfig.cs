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
                distanceMidpointT: 0.74f);

        public float SliderMinValue { get; private set; }
        public float SliderMaxValue { get; private set; }
        public float SliderStep { get; private set; }
        public float DistanceSliderMinValue { get; private set; }
        public float DistanceSliderMaxValue { get; private set; }
        public float DistanceMidpointT { get; private set; }

        public ParkingRulesConfigUiConfig(
            float sliderMinValue,
            float sliderMaxValue,
            float sliderStep,
            float distanceSliderMinValue,
            float distanceSliderMaxValue,
            float distanceMidpointT)
        {
            SliderMinValue = sliderMinValue;
            SliderMaxValue = sliderMaxValue;
            SliderStep = sliderStep;
            DistanceSliderMinValue = distanceSliderMinValue;
            DistanceSliderMaxValue = distanceSliderMaxValue;
            DistanceMidpointT = distanceMidpointT;
        }
    }
}

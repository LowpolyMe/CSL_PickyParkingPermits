using UnityEngine;
using PickyParking.Features.ParkingRules;
using PickyParking.UI.BuildingOptionsPanel;

namespace PickyParking.UI.BuildingOptionsPanel.ParkingRulesPanel
{
    internal static class DistanceSliderMapping
    {
        public static float SliderToDistanceMeters(float sliderValue, ParkingRulesConfigUiConfig uiConfig)
        {
            if (uiConfig.DistanceSliderMaxValue <= uiConfig.DistanceSliderMinValue)
                return ParkingRulesLimits.MinRadiusMeters;

            float min = Mathf.Max(ParkingRulesLimits.MinRadiusMeters, 0.0001f);
            float max = Mathf.Max(ParkingRulesLimits.MaxRadiusMeters, min + 0.0001f);
            float mid = Mathf.Clamp(ParkingRulesLimits.MidRadiusMeters, min + 0.0001f, max - 0.0001f);

            if (sliderValue <= uiConfig.DistanceSliderMinValue)
                return min;
            if (sliderValue >= uiConfig.DistanceSliderMaxValue)
                return max;

            float t = Mathf.InverseLerp(uiConfig.DistanceSliderMinValue, uiConfig.DistanceSliderMaxValue, sliderValue);
            float curvePower = ComputeCurvePower(min, mid, max);
            float distanceRatio = max / min;
            float exponent = Mathf.Pow(t, curvePower);
            return min * Mathf.Pow(distanceRatio, exponent);
        }

        public static float DistanceMetersToSlider(float distanceMeters, ParkingRulesConfigUiConfig uiConfig)
        {
            if (uiConfig.DistanceSliderMaxValue <= uiConfig.DistanceSliderMinValue)
                return uiConfig.DistanceSliderMinValue;

            float min = Mathf.Max(ParkingRulesLimits.MinRadiusMeters, 0.0001f);
            float max = Mathf.Max(ParkingRulesLimits.MaxRadiusMeters, min + 0.0001f);
            float mid = Mathf.Clamp(ParkingRulesLimits.MidRadiusMeters, min + 0.0001f, max - 0.0001f);

            if (distanceMeters <= min)
                return uiConfig.DistanceSliderMinValue;
            if (distanceMeters >= max)
                return uiConfig.DistanceSliderMaxValue;

            float distanceRatio = max / min;
            if (distanceRatio <= 1.0001f)
                return Mathf.Lerp(
                    uiConfig.DistanceSliderMinValue,
                    uiConfig.DistanceSliderMaxValue,
                    Mathf.InverseLerp(min, max, distanceMeters));

            float curvePower = Mathf.Max(0.0001f, ComputeCurvePower(min, mid, max));
            float targetExponent = Mathf.Log(distanceMeters / min) / Mathf.Log(distanceRatio);
            targetExponent = Mathf.Clamp01(targetExponent);
            float t = Mathf.Pow(targetExponent, 1f / curvePower);
            return Mathf.Lerp(uiConfig.DistanceSliderMinValue, uiConfig.DistanceSliderMaxValue, t);
        }

        private static float ComputeCurvePower(
            float minDistanceMeters,
            float midDistanceMeters,
            float maxDistanceMeters)
        {
            float distanceRatio = maxDistanceMeters / minDistanceMeters;
            if (distanceRatio <= 1.0001f)
                return 1f;
            float targetExponent = Mathf.Log(midDistanceMeters / minDistanceMeters) / Mathf.Log(distanceRatio);
            targetExponent = Mathf.Clamp(targetExponent, 0.0001f, 0.9999f);
            return Mathf.Log(targetExponent) / Mathf.Log(BuildingOptionsPanelUiValues.DistanceSliderMapping.MidPoint);
        }
    }
}











using UnityEngine;

namespace PickyParking.UI
{
    internal static class DistanceSliderMapping
    {
        public static float SliderToDistanceMeters(
            float sliderValue,
            float sliderMinValue,
            float sliderMaxValue,
            float minDistanceMeters,
            float midDistanceMeters,
            float maxDistanceMeters)
        {
            if (sliderMaxValue <= sliderMinValue)
                return minDistanceMeters;

            float min = Mathf.Max(minDistanceMeters, 0.0001f);
            float max = Mathf.Max(maxDistanceMeters, min + 0.0001f);
            float mid = Mathf.Clamp(midDistanceMeters, min + 0.0001f, max - 0.0001f);

            if (sliderValue <= sliderMinValue)
                return min;
            if (sliderValue >= sliderMaxValue)
                return max;

            float t = Mathf.InverseLerp(sliderMinValue, sliderMaxValue, sliderValue);
            float curvePower = ComputeCurvePower(min, mid, max);
            float distanceRatio = max / min;
            float exponent = Mathf.Pow(t, curvePower);
            return min * Mathf.Pow(distanceRatio, exponent);
        }

        public static float DistanceMetersToSlider(
            float distanceMeters,
            float sliderMinValue,
            float sliderMaxValue,
            float minDistanceMeters,
            float midDistanceMeters,
            float maxDistanceMeters)
        {
            if (sliderMaxValue <= sliderMinValue)
                return sliderMinValue;

            float min = Mathf.Max(minDistanceMeters, 0.0001f);
            float max = Mathf.Max(maxDistanceMeters, min + 0.0001f);
            float mid = Mathf.Clamp(midDistanceMeters, min + 0.0001f, max - 0.0001f);

            if (distanceMeters <= min)
                return sliderMinValue;
            if (distanceMeters >= max)
                return sliderMaxValue;

            float distanceRatio = max / min;
            if (distanceRatio <= 1.0001f)
                return Mathf.Lerp(sliderMinValue, sliderMaxValue, Mathf.InverseLerp(min, max, distanceMeters));

            float curvePower = Mathf.Max(0.0001f, ComputeCurvePower(min, mid, max));
            float targetExponent = Mathf.Log(distanceMeters / min) / Mathf.Log(distanceRatio);
            targetExponent = Mathf.Clamp01(targetExponent);
            float t = Mathf.Pow(targetExponent, 1f / curvePower);
            return Mathf.Lerp(sliderMinValue, sliderMaxValue, t);
        }

        private static float ComputeCurvePower(
            float minDistanceMeters,
            float midDistanceMeters,
            float maxDistanceMeters)
        {
            float distanceRatio = maxDistanceMeters / minDistanceMeters;
            if (distanceRatio <= 1.0001f)
                return 1f;
            const float midPoint = 0.5f;
            float targetExponent = Mathf.Log(midDistanceMeters / minDistanceMeters) / Mathf.Log(distanceRatio);
            targetExponent = Mathf.Clamp(targetExponent, 0.0001f, 0.9999f);
            return Mathf.Log(targetExponent) / Mathf.Log(midPoint);
        }
    }
}

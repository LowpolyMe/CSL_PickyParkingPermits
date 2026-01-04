using System;
using System.Globalization;
using PickyParking.UI.BuildingOptionsPanel;

namespace PickyParking.UI.BuildingOptionsPanel.ParkingRulesPanel
{
    internal static class DistanceDisplayFormatter
    {
        public static string FormatDisplay(
            float sliderValue,
            float distanceSliderMaxValue,
            float minMeters,
            float maxMeters,
            float mappedMeters)
        {
            if (sliderValue >= 1f)
                return BuildingOptionsPanelUiValues.DistanceDisplay.InfiniteLabel;

            if (sliderValue <= 0f)
                return "0 m";

            if (sliderValue >= distanceSliderMaxValue)
                return FormatDistanceLabel(maxMeters);

            float clampedMeters = Clamp(mappedMeters, minMeters, maxMeters);
            return FormatDistanceLabel(clampedMeters);
        }

        public static string FormatDistanceLabel(float meters)
        {
            if (meters < 1000f)
            {
                int roundedMeters;
                if (meters < 100f)
                    roundedMeters = RoundToInt(meters);
                else if (meters < 500f)
                    roundedMeters = RoundToInt(meters / 5f) * 5;
                else
                    roundedMeters = RoundToInt(meters / 10f) * 10;

                return roundedMeters + "m";
            }

            float km = meters / 1000f;
            if (km < 10f)
                return string.Format(CultureInfo.InvariantCulture, "{0:0.0}km", km);

            int roundedKm = RoundToInt(km);
            return roundedKm + "km";
        }

        private static int RoundToInt(float value)
        {
            return (int)Math.Floor(value + 0.5f);
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }
    }
}









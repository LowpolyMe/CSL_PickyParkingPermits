using UnityEngine;

namespace PickyParking.UI
{
    public static class ColorConversion
    {
        public static float ToHue(Color color)
        {
            Color.RGBToHSV(color, out float h, out _, out _);
            return h;
        }

        public static Color FromHue(float hue, float strength)
        {
            var color = Color.HSVToRGB(hue, 1f, 1f);
            color.a = Mathf.Clamp01(strength);
            return color;
        }
        
        public static Color FromHue(float hue)
        {
            var color = Color.HSVToRGB(hue, 1f, 1f);
            return color;
        }
    }
}

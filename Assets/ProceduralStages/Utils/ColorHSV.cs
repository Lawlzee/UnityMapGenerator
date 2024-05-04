using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProceduralStages
{
    public struct ColorHSV
    {
        public float hue;
        public float saturation;
        public float value;

        public ColorHSV(float hue, float saturation, float value)
        {
            this.hue = hue;
            this.saturation = saturation;
            this.value = value;
        }

        public static ColorHSV FromRGB(Color color)
        {
            Color.RGBToHSV(color, out float h, out float s, out float v);
            return new ColorHSV(h, s, v);
        }

        public Color ToRGB()
        {
            return Color.HSVToRGB(hue, saturation, value);
        }

        public static ColorHSV GetRandom(ColorHSV minColor, ColorHSV maxColor, Xoroshiro128Plus rng)
        {
            if (maxColor.hue < minColor.hue)
            {
                maxColor.hue++;
            }

            if (maxColor.saturation < minColor.saturation)
            {
                (minColor.saturation, maxColor.saturation) = (maxColor.saturation, minColor.saturation);
            }

            if (maxColor.value < minColor.value)
            {
                (minColor.value, maxColor.value) = (maxColor.value, minColor.value);
            }

            return new ColorHSV(
                rng.RangeFloat(minColor.hue, maxColor.hue) % 1,
                rng.RangeFloat(minColor.saturation, maxColor.saturation),
                rng.RangeFloat(minColor.value, maxColor.value));
        }

        public static float ClampHue(float hue, float min, float max)
        {
            if (max < min)
            {
                max++;
            }

            return Mathf.Clamp(hue, min, max) % 1;
        }
    }

    public static class ColorExtensions
    {
        public static ColorHSV ToHSV(this Color color)
        {
            return ColorHSV.FromRGB(color);
        }
    }
}

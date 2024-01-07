using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts
{
    [Serializable]
    public class ColorPatelette
    {
        public int size = 256;
        public int transitionSize = 50;
        public Palette floor = new Palette();
        public Palette walls = new Palette();
        public Palette ceilling = new Palette();

        public Palette light = new Palette();

        [Range(0, 1)]
        public float minNoise = 0.1f;
        [Range(0, 1)]
        public float maxNoise = 0.25f;

        public Texture2D Create(System.Random rng)
        {
            Color[] colors = new Color[8];

            var palettes = new Palette[]
            {
                floor,
                walls,
                ceilling
            };

            for (int i = 0; i < 3; i++)
            {
                Palette palette = palettes[i];

                float hue1 = (float)rng.NextDouble();
                colors[i * 2] = Color.HSVToRGB(hue1, palette.saturation, palette.value);

                float noise = (float)rng.NextDouble() * (maxNoise - minNoise) + minNoise;

                float hue2 = (hue1 + noise + 1) % 1;
                colors[i * 2 + 1] = Color.HSVToRGB(hue2, palette.saturation, palette.value);
            }

            Texture2D texture = new Texture2D(size * 2 + transitionSize, size);

            float factor = 1f / (size - 1);
            float transitionFactor = 1f / (transitionSize - 1);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        Color a = Color.Lerp(colors[i * 2 + 0], colors[i * 2 + 2], x * factor);
                        Color b = Color.Lerp(colors[i * 2 + 1], colors[i * 2 + 3], x * factor);

                        Color color = Color.Lerp(a, b, y * factor);

                        texture.SetPixel(i * (size + transitionSize) + x, y, color);
                    }

                }

                for (int i = 0; i < transitionSize; i++)
                {
                    int x = i + size;

                    Color a = Color.Lerp(colors[1], colors[4], i * transitionFactor);
                    Color b = Color.Lerp(colors[3], colors[6], i * transitionFactor);

                    Color color = Color.Lerp(a, b, y * factor);

                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            texture.wrapMode = TextureWrapMode.Clamp;

            return texture;
        }

        public Color AverageColor(Texture2D texture)
        {
            Color32[] colors = texture.GetPixels32();
            int total = colors.Length;
            var r = 0; 
            var g = 0; 
            var b = 0;
            for (var i = 0; i < total; i++)
            {
                r += colors[i].r;
                g += colors[i].g;
                b += colors[i].b;
            }

            return new Color32((byte)(r / total), (byte)(g / total), (byte)(b / total), 0);
        }

        [Serializable]
        public class Palette
        {
            [Range(0,1 )]
            public float saturation;
            [Range(0, 1)]
            public float value;
        }
    }
}

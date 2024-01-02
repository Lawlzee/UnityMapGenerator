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
        public Palette floor;
        public Palette walls;
        public Palette ceilling;

        public Texture2D Create(System.Random rng)
        {
            const float goldenRatio = 0.618033988749895f;


            float hue = (float)rng.NextDouble();
            Color[] colors = new Color[8];
            for (int i = 0; i < 4; i++)
            {
                float b = i / 2 == 0
                    ? 0.25f
                    : 0.25f;

                float a = i / 2 == 0
                    ? 0.75f
                    : 0.25f;

                Palette palette1 = i / 2 == 0
                    ? floor
                    : walls; 

                hue = (float)rng.NextDouble();
                //hue = (hue + goldenRatio) % 1;
                colors[i * 2] = Color.HSVToRGB(hue, palette1.saturation, palette1.value);


                Palette palette2 = i / 2 == 0
                    ? walls
                    : ceilling;
                hue = (float)rng.NextDouble();
                //hue = (hue + goldenRatio) % 1;
                colors[i * 2 + 1] = Color.HSVToRGB(hue, palette2.saturation, palette2.value);
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
                        Color a = Color.Lerp(colors[i * 4 + 0], colors[i * 4 + 1], x * factor);
                        Color b = Color.Lerp(colors[i * 4 + 2], colors[i * 4 + 3], x * factor);

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

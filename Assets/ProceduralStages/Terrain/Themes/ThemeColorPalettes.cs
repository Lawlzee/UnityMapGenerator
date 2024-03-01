using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProceduralStages
{
    [CreateAssetMenu(fileName = "fog", menuName = "ProceduralStages/ColorPalette", order = 6)]
    public class ThemeColorPalettes : ScriptableObject
    {
        public int size = 256;
        public SurfaceColor floor;
        public SurfaceColor walls;
        public SurfaceColor ceilling;

        public SurfaceColor light;
        public FogColorPalette fog;

        [Range(0, 1)]
        public float minNoise = 0.1f;
        [Range(0, 1)]
        public float maxNoise = 0.25f;

        public Texture2D CreateTexture()
        {
            Color[] colors = new Color[6];

            var palettes = new SurfaceColor[]
            {
                floor,
                walls,
                ceilling
            };

            float hue1 = MapGenerator.rng.nextNormalizedFloat;
            float noise1 = MapGenerator.rng.nextNormalizedFloat * (maxNoise - minNoise) + minNoise;
            float hue1Variation = (hue1 + noise1 + 1) % 1;

            float hue2 = MapGenerator.rng.nextNormalizedFloat;
            float noise2 = MapGenerator.rng.nextNormalizedFloat * (maxNoise - minNoise) + minNoise;
            float hue2Variation = (hue2 + noise2 + 1) % 1;

            colors[0] = Color.HSVToRGB(hue1, floor.saturation, floor.value);
            colors[1] = Color.HSVToRGB(hue1Variation, floor.saturation, floor.value);

            colors[2] = Color.HSVToRGB(hue2, walls.saturation, walls.value);
            colors[3] = Color.HSVToRGB(hue2Variation, walls.saturation, walls.value);

            colors[4] = Color.HSVToRGB(hue2, ceilling.saturation, ceilling.value);
            colors[5] = Color.HSVToRGB(hue2Variation, ceilling.saturation, ceilling.value);

            Texture2D texture = new Texture2D(size * 2, size);

            float factor = 1f / (size - 1);

            Color[] pixels = new Color[2 * size * size];

            Vector2Int seed = new Vector2Int(
                MapGenerator.rng.RangeInt(0, short.MaxValue),
                MapGenerator.rng.RangeInt(0, short.MaxValue));

            Parallel.For(0, size, y =>
            {
                for (int x = 0; x < size; x++)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        Color a = Color.Lerp(colors[i * 2 + 0], colors[i * 2 + 2], x * factor);
                        Color b = Color.Lerp(colors[i * 2 + 1], colors[i * 2 + 3], x * factor);

                        Color color = Color.Lerp(a, b, y * factor);

                        int posX = i * size + x;
                        int pixelIndex = y * 2 * size + posX;
                        //float noise = heightMapPixels[pixelIndex].r;

                        //float amplitude = i == 0 ? floorPerlinAmplitude : this.amplitude;
                        //float noise = (1 - amplitude) + heightColor * amplitude;

                        pixels[pixelIndex] = color;
                    }

                }
            });

            texture.SetPixels(pixels);
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
    }
}

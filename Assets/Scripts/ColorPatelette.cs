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
        public Palette floor = new Palette();
        public Palette walls = new Palette();
        public Palette ceilling = new Palette();

        public Palette light = new Palette();
        public Fog fog = new Fog();

        [Range(0, 1)]
        public float perlinFrequency;

        [Range(0, 1000)]
        public int xSquareSize = 10;
        [Range(0, 1000)]
        public int ySquareSize = 10;


        [Range(0, 1)]
        public float minNoise = 0.1f;
        [Range(0, 1)]
        public float maxNoise = 0.25f;

        public Texture2D CreateTexture(System.Random rng, Texture2D heightMap)
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

            Texture2D texture = new Texture2D(size * 2, size);

            float factor = 1f / (size - 1);

            Color[] pixels = new Color[2 * size * size];
            Color[] heightMapPixels = heightMap.GetPixels();

            Vector2Int seed = new Vector2Int(rng.Next() % short.MaxValue, rng.Next() % short.MaxValue);
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
                        float noise = heightMapPixels[pixelIndex].r;

                        //float amplitude = i == 0 ? floorPerlinAmplitude : this.amplitude;
                        //float noise = (1 - amplitude) + heightColor * amplitude;

                        color = new Color(noise, noise, noise) * color;
                        pixels[pixelIndex] = color;
                    }

                }
            });

            texture.SetPixels(pixels);
            texture.Apply();
            texture.wrapMode = TextureWrapMode.Clamp;

            return texture;
        }

        public Texture2D CreateHeightMap(System.Random rng)
        {
            var palettes = new Palette[]
            {
                floor,
                walls,
                ceilling
            };

            float factor = 1f / (size - 1);

            Texture2D texture = new Texture2D(size * 2, size);
            Color[] pixels = new Color[2 * size * size];

            int baseSeed = rng.Next() % short.MaxValue;
            int detailSeed = rng.Next() % short.MaxValue;
            Parallel.For(0, size, y =>
            {
                for (int x = 0; x < size; x++)
                {
                    for (int i = 0; i < 2; i++)
                    {

                        float baseAmplitude = Mathf.Lerp(palettes[i].perlinAmplitude, palettes[i + 1].perlinAmplitude, x * factor);
                        int baseNoiseY = (y + baseSeed) / ySquareSize;
                        float baseNoise = (1 - baseAmplitude) + ((Mathf.PerlinNoise(0, baseNoiseY / perlinFrequency) + 1) / 2) * baseAmplitude;

                        float detailAmplitude = Mathf.Lerp(palettes[i].detailPerlinAmplitude, palettes[i + 1].detailPerlinAmplitude, x * factor);
                        int noiseDetailY = y + detailSeed;
                        float detailNoise = detailAmplitude * Mathf.PerlinNoise(0, noiseDetailY / perlinFrequency) / 2;

                        float noise = Mathf.Clamp01(baseNoise + detailNoise);

                        pixels[y * 2 * size + i * size + x] = new Color(noise, noise, noise);
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

        [Serializable]
        public class Palette
        {
            [Range(0, 1)]
            public float saturation;
            [Range(0, 1)]
            public float value;
            [Range(0, 1)]
            public float perlinAmplitude;
            [Range(0, 1)]
            public float detailPerlinAmplitude;
        }

        [Serializable]
        public class Fog
        {
            [Range(0, 1)]
            public float saturation;
            [Range(0, 1)]
            public float value;

            [Range(0, 1)]
            public float colorStartAlpha;
            [Range(0, 1)]
            public float colorMidAlpha;
            [Range(0, 1)]
            public float colorEndAlpha;

            [Range(0, 1)]
            public float zero;
            [Range(0, 1)]
            public float one;

            [Range(0, 1)]
            public float intensity;
            [Range(0, 1)]
            public float power;
        }
    }
}

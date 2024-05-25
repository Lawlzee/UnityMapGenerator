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
        public SurfaceColor grass;
        public SurfaceColor walls;
        public SurfaceColor ceilling;

        public SurfaceColor light;
        public FogColorPalette fog;

        [Range(0, 1)]
        public float minNoise = 0.1f;
        [Range(0, 1)]
        public float maxNoise = 0.25f;

        public Texture2D CreateGrassTexture()
        {
            return CreateTexture(grass);
        }

        public Texture2D CreateTerrainTexture()
        {
            return CreateTexture(floor);
        }

        private Texture2D CreateTexture(SurfaceColor floor)
        {
            Color[] colors = new Color[6];

            var palettes = new SurfaceColor[]
            {
                floor,
                walls,
                ceilling
            };

            ColorHSV minFloorColor = floor.minColor.ToHSV();
            ColorHSV maxFloorColor = floor.maxColor.ToHSV();

            ColorHSV minWallColor = floor.minColor.ToHSV();
            ColorHSV maxWallColor = floor.maxColor.ToHSV();

            ColorHSV minCeilColor = floor.minColor.ToHSV();
            ColorHSV maxCeilColor = floor.maxColor.ToHSV();

            ColorHSV floorColor = ColorHSV.GetRandom(floor.minColor.ToHSV(), floor.maxColor.ToHSV(), MapGenerator.rng);
            ColorHSV wallColor = ColorHSV.GetRandom(walls.minColor.ToHSV(), walls.maxColor.ToHSV(), MapGenerator.rng);
            ColorHSV ceilColor = ColorHSV.GetRandom(ceilling.minColor.ToHSV(), ceilling.maxColor.ToHSV(), MapGenerator.rng);

            float floorNoise = MapGenerator.rng.RangeFloat(minNoise, maxNoise);
            float floorHueVariation = ColorHSV.ClampHue((floorColor.hue + floorNoise + 1) % 1, minFloorColor.hue, maxFloorColor.hue);

            float wallNoise = MapGenerator.rng.RangeFloat(minNoise, maxNoise);
            float wallHueVariation = ColorHSV.ClampHue((wallColor.hue + wallNoise + 1) % 1, minWallColor.hue, maxWallColor.hue);

            colors[0] = floorColor.ToRGB();
            colors[1] = Color.HSVToRGB(floorHueVariation, floorColor.saturation, floorColor.value);

            colors[2] = wallColor.ToRGB();
            colors[3] = Color.HSVToRGB(wallHueVariation, wallColor.saturation, wallColor.value);

            colors[4] = Color.HSVToRGB(wallColor.hue, ceilColor.saturation, ceilColor.value);
            colors[5] = Color.HSVToRGB(wallHueVariation, ceilColor.saturation, ceilColor.value);

            Texture2D texture = new Texture2D(size * 2, size);

            float factor = 1f / (size - 1);

            Color[] pixels = new Color[2 * size * size];

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
    }
}

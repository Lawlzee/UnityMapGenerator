using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProceduralStages
{
    [Serializable]
    public class Map2dGenerator
    {
        public float frequency;
        [Range(0, 1)]
        public float wallSurface = 0.5f;

        public float curveFrequency;
        public float curveVerticalScale;
        [Range(0, 1)]
        public float curveMinNoise;

        public float[,,] Create(int width, int height, int depth, System.Random rng)
        {
            int wallSeedX = rng.Next(short.MaxValue);
            int wallSeedY = rng.Next(short.MaxValue);
            int wallSeedZ = rng.Next(short.MaxValue);

            int curveSeedX = rng.Next(short.MaxValue);
            int curveSeedY = rng.Next(short.MaxValue);
            int curveSeedZ = rng.Next(short.MaxValue);

            float[,,] map = new float[width, height, depth];

            Parallel.For(0, width, x =>
            {
                for (int z = 0; z < depth; z++)
                {
                    float wallnoise = Mathf.PerlinNoise(x / frequency + wallSeedX, z / frequency + wallSeedZ);
                    float scaledWallNoise = Mathf.Clamp01(wallnoise + 0.5f - wallSurface);

                    for (int y = 0; y < height; y++)
                    {
                        float curveNoise = (PerlinNoise.Get(new Vector3(x + curveSeedX, y * curveVerticalScale + curveSeedY, z + curveSeedZ), curveFrequency) + 1) / 2;
                        float scaledCurveNoise = curveMinNoise + (curveNoise * (1 - curveMinNoise));

                        map[x, y, z] = Mathf.Clamp01(scaledWallNoise * scaledCurveNoise);
                    }
                }
            });

            return map;
        }
    }
}

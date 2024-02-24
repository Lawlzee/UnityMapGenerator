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

        public float[,,] Create(Vector3Int size)
        {
            int wallSeedX = MapGenerator.rng.RangeInt(0, short.MaxValue);
            int wallSeedY = MapGenerator.rng.RangeInt(0, short.MaxValue);
            int wallSeedZ = MapGenerator.rng.RangeInt(0, short.MaxValue);

            int curveSeedX = MapGenerator.rng.RangeInt(0, short.MaxValue);
            int curveSeedY = MapGenerator.rng.RangeInt(0, short.MaxValue);
            int curveSeedZ = MapGenerator.rng.RangeInt(0, short.MaxValue);

            float[,,] map = new float[size.x, size.y, size.z];

            Parallel.For(0, size.x, x =>
            {
                for (int z = 0; z < size.z; z++)
                {
                    float wallnoise = Mathf.PerlinNoise(x / frequency + wallSeedX, z / frequency + wallSeedZ);
                    float scaledWallNoise = Mathf.Clamp01(wallnoise + 0.5f - wallSurface);

                    for (int y = 0; y < size.y; y++)
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

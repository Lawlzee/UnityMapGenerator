using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts
{
    [Serializable]
    public class Carver
    {
        [Range(0f, 1f)]
        public float frequency = 0.03f;
        [Range(0f, 5f)]
        public float verticalScale = 0.5f;
        [Range(0f, 1f)]
        public float maxNoise;

        public void CarveWalls(bool[,,] map, System.Random rng)
        {
            int seedX = rng.Next(short.MaxValue);
            int seedY = rng.Next(short.MaxValue);
            int seedZ = rng.Next(short.MaxValue);

            int width3d = map.GetLength(0);
            int height3d = map.GetLength(1);
            int depth3d = map.GetLength(2);

            Parallel.For(1, width3d - 1, x =>
            {
                for (int y = 1; y < height3d - 1; y++)
                {
                    for (int z = 1; z < depth3d - 1; z++)
                    {
                        if (!map[x, y, z])
                        {
                            continue;
                        }

                        float noise = (PerlinNoise.Get(new Vector3(x + seedX, y * verticalScale + seedY, z + seedZ), frequency) + 1) / 2;
                        if (noise < maxNoise)
                        {
                            map[x, y, z] = !map[x, y, z];
                        }
                    }
                }
            });
        }
    }
}

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

            for (int x = 0; x < width3d; x++)
            {
                for (int y = 0; y < height3d; y++)
                {
                    for (int z = 0; z < depth3d; z++)
                    {
                        if (x == 0 || y == 0 || z == 0 || x == width3d - 1 || z == depth3d - 1)
                        {
                            continue;
                        }

                        float noise = (PerlinNoise.Get(new Vector3(x + seedX, y * verticalScale + seedY, z + seedZ), frequency) + 1) / 2;
                        if (map[x, y, z] && noise < maxNoise)
                        {
                            map[x, y, z] = !map[x, y, z];
                        }
                    }
                }
            }
        }
    }
}

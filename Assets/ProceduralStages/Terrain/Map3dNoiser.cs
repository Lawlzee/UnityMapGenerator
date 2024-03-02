using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProceduralStages
{
    [Serializable]
    public class Map3dNoiser
    {
        public bool enabled = true;
        public float frequency;
        [Range(0, 1)]
        public float amplitude;

        public float[,,] AddNoise(float[,,] map)
        {
            if (!enabled)
            {
                return map;
            }

            int seedX = MapGenerator.rng.RangeInt(0, short.MaxValue);
            int seedY = MapGenerator.rng.RangeInt(0, short.MaxValue);
            int seedZ = MapGenerator.rng.RangeInt(0, short.MaxValue);

            int width3d = map.GetLength(0);
            int height3d = map.GetLength(1);
            int depth3d = map.GetLength(2);

            float[,,] result = new float[width3d, height3d, depth3d];

            Parallel.For(0, width3d, x =>
            {
                for (int y = 0; y < height3d; y++)
                {
                    for (int z = 0; z < depth3d; z++)
                    {
                        float noise = amplitude * PerlinNoise.Get(new Vector3(x + seedX, y + seedY, z + seedZ), frequency);
                        result[x, y, z] = Mathf.Clamp01(map[x, y, z] + noise);
                    }
                }
            });

            return result;
        }
    }
}

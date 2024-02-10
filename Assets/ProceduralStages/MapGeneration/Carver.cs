using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProceduralStages
{
    [Serializable]
    public class Carver
    {
        public bool enabled = true;

        [Range(0f, 1f)]
        public float frequency = 0.03f;
        [Range(0f, 5f)]
        public float verticalScale = 0.5f;
        
        public void CarveWalls(float[,,] map)
        {
            if (!enabled)
            {
                return;
            }

            int seedX = MapGenerator.rng.RangeInt(0, short.MaxValue);
            int seedY = MapGenerator.rng.RangeInt(0, short.MaxValue);
            int seedZ = MapGenerator.rng.RangeInt(0, short.MaxValue);

            int width3d = map.GetLength(0);
            int height3d = map.GetLength(1);
            int depth3d = map.GetLength(2);

            Parallel.For(1, width3d - 1, x =>
            {
                for (int y = 1; y < height3d - 1; y++)
                {
                    for (int z = 1; z < depth3d - 1; z++)
                    {
                        float mapNoise = map[x, y, z];
                        float noise = (PerlinNoise.Get(new Vector3(x + seedX, y * verticalScale + seedY, z + seedZ), frequency) + 1) / 2;

                        map[x, y, z] = Mathf.Min(mapNoise, noise);
                    }
                }
            });
        }
    }
}

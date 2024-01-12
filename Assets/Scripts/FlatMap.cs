using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts
{
    [Serializable]
    public class FlatMap
    {
        public int sampleRadius = 6;

        [Range(0f, 1f)]
        public float minInteractableFlatness = 0.25f;
        [Range(0f, 1f)]
        public float minCharacterFlatness = 0.25f;
        [Range(0f, 1f)]
        public float minTeleporterFlatness = 0.55f;

        public float[,,] Create(bool[,,] map)
        {
            int width = map.GetLength(0);
            int height = map.GetLength(1);
            int depth = map.GetLength(2);

            float[,,] flatMap = new float[width, height, depth];

            float totalDistance = 0;

            for (int dx = -sampleRadius; dx <= sampleRadius; dx++)
            {
                for (int dz = -sampleRadius; dz <= sampleRadius; dz++)
                {
                    totalDistance += Math.Abs(dx) + Math.Abs(dz);
                }
            }

            float min = float.MaxValue;
            float max = float.MinValue;

            Parallel.For(0, width, posX =>
            {
                for (int posY = 1; posY < height; posY++)
                {
                    for (int posZ = 0; posZ < depth; posZ++)
                    {
                        if (map[posX, posY, posZ] || !map[posX, posY - 1, posZ])
                        {
                            flatMap[posX, posY, posZ] = 0;
                        }
                        else
                        {
                            int wallNess = 0;

                            for (int dx = -sampleRadius; dx <= sampleRadius; dx++)
                            {
                                int x = posX + dx;

                                for (int dz = -sampleRadius; dz <= sampleRadius; dz++)
                                {
                                    int z = posZ + dz;

                                    int distance = Math.Abs(dx) + Math.Abs(dz);

                                    if (x < 0
                                        || x >= width
                                        || z < 0
                                        || z >= depth
                                        || map[x, posY, z]
                                        || !map[x, posY - 1, z])
                                    {
                                        wallNess += distance;
                                    }
                                }
                            }

                            float value = 1 - (wallNess / totalDistance);
                            flatMap[posX, posY, posZ] = value;

                            min = Mathf.Min(min, value);
                            max = Mathf.Max(max, value);
                        }
                    }
                }
            });

            Debug.Log(min);
            Debug.Log(max);

            return flatMap;
        }
    }
}

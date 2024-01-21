using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProceduralStages
{
    [Serializable]
    public class CellularAutomata3d
    {
        public bool enabled = true;
        public int smoothingInterations;

        public float[,,] SmoothMap(float[,,] map)
        {
            if (!enabled)
            {
                return map;
            }

            int width = map.GetLength(0);
            int height = map.GetLength(1);
            int depth = map.GetLength(2);

            var buffer = new float[width, height, depth];

            for (int i = 0; i < smoothingInterations; i++)
            {
                var newMap = buffer;
                Parallel.For(0, width, x =>
                {
                    for (int y = 0; y < height; y++)
                    {
                        for (int z = 0; z < depth; z++)
                        {
                            if (x == 0 || y == 0 || z == 0 || x == width - 1 || y == height - 1 || z == depth - 1)
                            {
                                newMap[x, y, z] = 1f;
                            }
                            else
                            {
                                float average = GetNeighbourCount(map, width, height, depth, x, y, z);

                                newMap[x, y, z] = average;

                                //if (neighbourCount > 13)
                                //{
                                //    newMap[x, y, z] = true;
                                //}
                                //else if (neighbourCount < 13)
                                //{
                                //    newMap[x, y, z] = false;
                                //}
                                //else
                                //{
                                //    newMap[x, y, z] = map[x, y, z];
                                //}
                            }
                        }
                    }
                });

                buffer = map;
                map = newMap;
            }

            return map;
        }

        private float GetNeighbourCount(float[,,] map, int width, int height, int depth, int posX, int posY, int posZ)
        {
            float average = 0;

            for (int dx = -1; dx <= 1; dx++)
            {
                int x = posX + dx;
                if (x < 0 || x >= width)
                {
                    continue;
                }

                for (int dy = -1; dy <= 1; dy++)
                {
                    int y = posY + dy;
                    if (y < 0 || y >= height)
                    {
                        continue;
                    }

                    for (int dz = -1; dz <= 1; dz++)
                    {
                        int z = posZ + dz;
                        if (z < 0 || z >= depth)
                        {
                            continue;
                        }

                        average += map[x, y, z] / 27f;
                    }
                }
            }

            return average;
        }
    }
}

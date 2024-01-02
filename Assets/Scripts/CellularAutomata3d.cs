using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts
{
    [Serializable]
    public class CellularAutomata3d
    {
        public int smoothingInterations;

        public bool[,,] SmoothMap(bool[,,] map)
        {
            int width = map.GetLength(0);
            int height = map.GetLength(1);
            int depth = map.GetLength(2);

            var buffer = new bool[width, height, depth];

            for (int i = 0; i < smoothingInterations; i++)
            {
                var newMap = buffer;
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        for (int z = 0; z < depth; z++)
                        {
                            if (x == 0 || y == 0 || z == 0 || x == width - 1 || y == height - 1 || z == depth - 1)
                            {
                                newMap[x, y, z] = true;
                            }
                            else
                            {
                                int neighbourCount = GetNeighbourCount(map, width, height, depth, x, y, z);

                                if (neighbourCount > 13)
                                {
                                    newMap[x, y, z] = true;
                                }
                                else if (neighbourCount < 13)
                                {
                                    newMap[x, y, z] = false;
                                }
                                else
                                {
                                    newMap[x, y, z] = map[x, y, z];
                                }
                            }
                        }
                    }
                }

                buffer = map;
                map = newMap;
            }

            return map;
        }

        private int GetNeighbourCount(bool[,,] map, int width, int height, int depth, int posX, int posY, int posZ)
        {
            int count = 0;

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

                        if (dx == 0 && dy == 0 && dz == 0)
                        {
                            continue;
                        }

                        if (map[x, y, z])
                        {
                            count++;
                        }
                    }
                }
            }

            return count;
        }
    }
}

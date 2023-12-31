using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts
{
    public class CellularAutomata2d
    {
        public static bool[,] Create(
            int width, 
            int height, 
            int randomFillPercent, 
            int smoothingInterations, 
            System.Random rng)
        {
            var map = RandomFillMap(width, height, randomFillPercent, rng);
            var smoothMap = SmoothMap(map, width, height, smoothingInterations);
            return smoothMap;
        }

        private static bool[,] RandomFillMap(int width, int height, int randomFillPercent, System.Random rng)
        {
            var map = new bool[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (x == 0 || y == 0 || x == width - 1 || y == height - 1)
                    {
                        map[x, y] = true;
                    }
                    else
                    {
                        map[x, y] = rng.Next(100) > randomFillPercent;
                    }

                }
            }

            return map;
        }

        private static bool[,] SmoothMap(bool[,] map, int width, int height, int smoothingInterations)
        {
            var buffer = new bool[width, height];

            for (int i = 0; i < smoothingInterations; i++)
            {
                var newMap = buffer;
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        int neighbourCount = GetNeighbourCount(map, width, height, x, y);

                        if (x == 0 || y == 0 || x == width - 1 || y == height - 1)
                        {
                            newMap[x, y] = true;
                        }
                        else
                        {
                            if (neighbourCount > 4)
                            {
                                newMap[x, y] = true;
                            }
                            else if (neighbourCount < 4)
                            {
                                newMap[x, y] = false;
                            }
                            else
                            {
                                newMap[x, y] = map[x, y];
                            }
                        }
                    }
                }

                buffer = map;
                map = newMap;
            }

            return map;
        }

        private static int GetNeighbourCount(bool[,] map, int width, int height, int posX, int posY)
        {
            int count = 0;

            for (int dx = -1; dx <= 1; dx++)
            {
                int x = posX + dx;
                if (x >= 0 && x < width)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int y = posY + dy;

                        if (dx == 0 && dy == 0)
                        {
                            continue;
                        }

                        if (y >= 0
                            && y < height
                            && map[x, y])
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

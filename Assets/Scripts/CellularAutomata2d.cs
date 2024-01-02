using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts
{
    [Serializable]
    public class CellularAutomata2d
    {
        [Range(0, 1)]
        public float randomFillPercent = 0.45f;
        [Range(0, 25)]
        public int iterations = 10;

        public bool[,] Create(
            int width, 
            int height, 
            System.Random rng)
        {
            var map = RandomFillMap(width, height, rng);
            var smoothMap = SmoothMap(map, width, height);
            return smoothMap;
        }

        private bool[,] RandomFillMap(int width, int height, System.Random rng)
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
                        map[x, y] = rng.NextDouble() > randomFillPercent;
                    }

                }
            }

            return map;
        }

        private bool[,] SmoothMap(bool[,] map, int width, int height)
        {
            var buffer = new bool[width, height];

            for (int i = 0; i < iterations; i++)
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

        private int GetNeighbourCount(bool[,] map, int width, int height, int posX, int posY)
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts
{
    [Serializable]
    public class Map2dToMap3d
    {
        [Range(0, 10)]
        public int squareScale;

        public bool[,,] Convert(bool[,] map, int height)
        {
            //int seedX = rng.Next(Int16.MaxValue);
            //int seedY = rng.Next(Int16.MaxValue);

            int width = map.GetLength(0);
            int depth = map.GetLength(1);

            int width3d = width * squareScale;
            int depth3d = depth * squareScale;


            bool[,,] result = new bool[width3d, height, depth3d];

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    //int heigth = map[x, z] ? height / 4 : height;

                    if (x == 0 || z == 0 || x == width - 1 || z == depth - 1)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            for (int i = 0; i < squareScale; i++)
                            {
                                for (int j = 0; j < squareScale; j++)
                                {
                                    result[x * squareScale + i, y, z * squareScale + j] = true;
                                }
                            }
                        }

                        continue;
                    }

                    //float noise = Mathf.PerlinNoise(x / noiseLevel + seedX, z / noiseLevel + seedY);

                    float blockHeight;
                    if (map[x, z])
                    {
                        blockHeight = height * 0.1f;
                        //blockHeight = height * 0.1f + noise * (height * 0.25f);
                    }
                    else
                    {
                        blockHeight = height * 1;
                        //blockHeight = height * 0.5f + noise * (height * 0.5f);
                    }

                    for (int i = 0; i < squareScale; i++)
                    {
                        for (int j = 0; j < squareScale; j++)
                        {
                            for (int y = 0; y < height && y < blockHeight; y++)
                            {
                                result[x * squareScale + i, y, z * squareScale + j] = true;
                            }

                            result[x * squareScale + i, 0, z * squareScale + j] = true;
                        }
                    }




                    //for (int y = 0; y < height; y++)
                    //{
                    //    if (map[x, z])
                    //    {
                    //        result[x, y, z] = true;
                    //    }
                    //}
                }
            }

            return result;
        }
    }
}

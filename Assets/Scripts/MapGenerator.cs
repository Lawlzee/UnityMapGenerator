using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Generator.Assets.Scripts
{
    public class MapGenerator : MonoBehaviour
    {
        public int width;
        public int height;

        [Range(0, 100)]
        public float scale = 1f;

        public string seed;

        [Range(0, 100)]
        public int randomFillPercent;

        public int smoothingInterations;

        [Range(0, 100)]
        public float noiseLevel = 0.01f;
        [Range(0, 100)]
        public float maxAmplitude = 5f;


        private float[,] _map;

        private void Start()
        {
            GenerateMap();
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                GenerateMap();
            }
        }

        private void OnValidate()
        {
            GenerateMap();
        }

        private void GenerateMap()
        {
            var map = RandomFillMap();
            var smoothMap = SmoothMap(map);
            _map = CreateHeightMap(smoothMap);
        }

        private bool[,] RandomFillMap()
        {
            var map = new bool[width, height];

            int currentSeed = string.IsNullOrEmpty(seed)
                ? Time.time.GetHashCode()
                : seed.GetHashCode();
            System.Random rng = new System.Random(currentSeed);

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

        private bool[,] SmoothMap(bool[,] map)
        {
            var buffer = new bool[width, height];

            for (int i = 0; i < smoothingInterations; i++)
            {
                var newMap = buffer;
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        int neighbourCount = GetNeighbourCount(map, x, y);

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

        private int GetNeighbourCount(bool[,] map, int posX, int posY)
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

        private float[,] CreateHeightMap(bool[,] map)
        {
            float[,] result = new float[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (!map[x, y])
                    {
                        result[x, y] = Mathf.PerlinNoise(x / noiseLevel, y / noiseLevel);
                    }
                    else
                    {
                        result[x, y] = -1;
                    }
                }
            }

            return result;
        }

        private void OnDrawGizmos()
        {
            if (_map != null)
            {
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        float posX = -width / 2 + x + 0.5f;
                        float posY = -height / 2 + y + 0.5f;


                        if (_map[x, y] < 0)
                        {
                            Gizmos.color = Color.black;
                            Vector3 pos = new Vector3(posX, 0, posY);
                            Gizmos.DrawCube(pos * scale, new Vector3(1, 10, 1) * scale);
                        }
                        else
                        {
                            Gizmos.color = Color.white;
                            Vector3 pos = new Vector3(posX, _map[x, y] * maxAmplitude, posY);
                            Gizmos.DrawCube(pos * scale, Vector3.one * scale);
                        }
                    }
                }
            }

        }
    }
}
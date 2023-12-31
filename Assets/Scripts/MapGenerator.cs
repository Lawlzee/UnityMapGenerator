using Assets.Scripts;
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
        public int depth;

        [Range(0, 100)]
        public float scale = 1f;

        public string seed;

        [Range(0, 100)]
        public int randomFillPercent;

        [Range(0, 25)]
        public int smoothingInterations;
        [Range(0, 25)]
        public int smoothingInterations3d;



        [Range(0, 100)]
        public float noiseLevel = 0.01f;
        [Range(0, 1)]
        public float noiseLevel3d = 0.1f;
        [Range(0, 1)]
        public float noiseRandomLevel3d = 0.5f;
        [Range(0, 100)]
        public float maxAmplitude = 5f;


        //private float[,] _map;
        private bool[,,] _map;

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
            int currentSeed = string.IsNullOrEmpty(seed)
                ? Time.time.GetHashCode()
                : seed.GetHashCode();
            System.Random rng = new System.Random(currentSeed);

            var map2D = CellularAutomata2d.Create(width, depth, randomFillPercent, smoothingInterations, rng);

            bool[,,] map3d = To3DMap(map2D, rng);
            AddNoise(map3d, rng);
            bool[,,] smoothMap3d = CellularAutomata3d.SmoothMap(map3d, smoothingInterations3d);

            var mesh = MarchingCubes.CreateMesh(smoothMap3d);

            GetComponent<MeshFilter>().mesh = mesh;

            _map = smoothMap3d;
        }

        private bool[,,] To3DMap(bool[,] map, System.Random rng)
        {
            int seedX = rng.Next(Int16.MaxValue);
            int seedY = rng.Next(Int16.MaxValue);

            bool[,,] result = new bool[width, height, depth];

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    //int heigth = map[x, z] ? height / 4 : height;

                    
                    float blockHeight;
                    if (map[x, z])
                    {
                        blockHeight = height * 0.25f;
                    }
                    else
                    {
                        float noise = Mathf.PerlinNoise(x / noiseLevel + seedX, z / noiseLevel + seedY);
                        blockHeight = height * 0.5f + noise * (height * 0.5f);
                    }

                    for (int y = 0; y < height && y < blockHeight; y++)
                    {
                        result[x, y, z] = true;
                    }
                }
            }

            return result;
        }

        private void AddNoise(bool[,,] map, System.Random rng)
        {
            int seedX = rng.Next(short.MaxValue);
            int seedY = rng.Next(short.MaxValue);
            int seedZ = rng.Next(short.MaxValue);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        float noise = PerlinNoise.Get(new Vector3(x + seedX, y + seedY, z + seedZ), noiseRandomLevel3d);
                        if (map[x, y, z] && noise < noiseLevel3d)
                        {
                            map[x, y, z] = !map[x, y, z];
                        }
                    }
                }
            }
        }

        private float[,] CreateHeightMap(bool[,] map, System.Random rng)
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

        //private void OnDrawGizmos()
        //{
        //    if (_map != null)
        //    {
        //        for (int x = 0; x < width; x++)
        //        {
        //            float posX = -width / 2 + x + 0.5f;
        //            for (int z = 0; z < depth; z++)
        //            {
        //                float posZ = -depth / 2 + z + 0.5f;
        //
        //                int? maxY = null;
        //                for (int y = height - 1; y >= 0; y--)
        //                {
        //                    float posY = -height / 2 + y + 0.5f;
        //
        //                    if (_map[x, y, z])
        //                    {
        //                        maxY = maxY ?? y;
        //
        //                        float intensity = maxY.Value / (float)height;
        //
        //                        Gizmos.color = new Color(intensity, intensity, intensity);
        //                        Vector3 pos = new Vector3(posX, posY, posZ);
        //                        Gizmos.DrawCube(pos * scale, Vector3.one * scale);
        //                    }
        //                }
        //            }
        //        }
        //    }
        //
        //}

        //private void OnDrawGizmos()
        //{
        //    if (_map != null)
        //    {
        //        for (int x = 0; x < width; x++)
        //        {
        //            for (int y = 0; y < height; y++)
        //            {
        //                float posX = -width / 2 + x + 0.5f;
        //                float posY = -height / 2 + y + 0.5f;
        //
        //
        //                if (_map[x, y] < 0)
        //                {
        //                    Gizmos.color = Color.black;
        //                    Vector3 pos = new Vector3(posX, 0, posY);
        //                    Gizmos.DrawCube(pos * scale, new Vector3(1, 10, 1) * scale);
        //                }
        //                else
        //                {
        //                    Gizmos.color = Color.white;
        //                    Vector3 pos = new Vector3(posX, _map[x, y] * maxAmplitude, posY);
        //                    Gizmos.DrawCube(pos * scale, Vector3.one * scale);
        //                }
        //            }
        //        }
        //    }
        //
        //}
    }
}
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

        public string seed;

        private int width3d => width * squareScale;
        private int height3d => height;
        private int depth3d => depth * squareScale;


        [Range(0, 100)]
        public float mapScale = 1f;

        [Range(0, 100)]
        public int randomFillPercent;

        [Range(1, 10)]
        public int squareScale = 3;

        [Range(0, 25)]
        public int smoothingInterations;
        [Range(0, 25)]
        public int smoothingInterations3d;

        [Range(0, 100)]
        public float noiseLevel = 0.01f;

        [Range(0, 1)]
        public float carverLevel3d = 0.1f;
        [Range(0, 1)]
        public float carverFrequency3d = 0.5f;

        [Range(0, 100)]
        public int maxFloorLevel = 10;
        [Range(0, 100)]
        public int maxWallThickness = 10;

        [Range(0, 100)]
        public int wallRoundingFactor = 10;

        //private float[,] _map;
        private bool[,,] _map;

        private void Start()
        {
            GenerateMap();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                GenerateMap();
            }
        }

        private void OnValidate()
        {
            if (Application.IsPlaying(this))
            {
                GenerateMap();
            }
        }

        private void GenerateMap()
        {
            int currentSeed = string.IsNullOrEmpty(seed)
                ? Time.time.GetHashCode()
                : seed.GetHashCode();
            System.Random rng = new System.Random(currentSeed);

            var map2D = CellularAutomata2d.Create(width, depth, randomFillPercent, smoothingInterations, rng);

            bool[,,] map3d = To3DMap(map2D, rng);
            CarveWalls(map3d, rng);
            AddFloorAndCeilling(map3d, rng);
            AddWalls(map3d, rng);

            bool[,,] smoothMap3d = CellularAutomata3d.SmoothMap(map3d, smoothingInterations3d);

            var mesh = MarchingCubes.CreateMesh(smoothMap3d);

            GetComponent<MeshFilter>().mesh = mesh;

            _map = smoothMap3d;
        }

        private bool[,,] To3DMap(bool[,] map, System.Random rng)
        {
            int seedX = rng.Next(Int16.MaxValue);
            int seedY = rng.Next(Int16.MaxValue);

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

        private void CarveWalls(bool[,,] map, System.Random rng)
        {
            int seedX = rng.Next(short.MaxValue);
            int seedY = rng.Next(short.MaxValue);
            int seedZ = rng.Next(short.MaxValue);

            for (int x = 0; x < width3d; x++)
            {
                for (int y = 0; y < height3d; y++)
                {
                    for (int z = 0; z < depth3d; z++)
                    {
                        if (x == 0 || y == 0 || z == 0 || x == width3d - 1 || z == depth3d - 1)
                        {
                            continue;
                        }

                        float noise = PerlinNoise.Get(new Vector3(x + seedX, y + seedY, z + seedZ), carverFrequency3d);
                        if (map[x, y, z] && noise < carverLevel3d)
                        {
                            map[x, y, z] = !map[x, y, z];
                        }
                    }
                }
            }
        }

        private void AddFloorAndCeilling(bool[,,] map, System.Random rng)
        {
            int floorSeedX = rng.Next(short.MaxValue);
            int floorSeedZ = rng.Next(short.MaxValue);
            int ceillingSeedX = rng.Next(short.MaxValue);
            int ceillingSeedZ = rng.Next(short.MaxValue);

            for (int x = 0; x < width3d; x++)
            {
                for (int z = 0; z < depth3d; z++)
                {
                    float floorNoise = Mathf.PerlinNoise(x / noiseLevel + floorSeedX, z / noiseLevel + floorSeedZ);

                    float floorHeight = 1 + floorNoise * maxFloorLevel;

                    for (int y = 0; y < height3d && y < floorHeight; y++)
                    {
                        map[x, y, z] = true;
                    }

                    float ceillingNoise = Mathf.PerlinNoise(x / noiseLevel + ceillingSeedX, z / noiseLevel + ceillingSeedZ);

                    float ceillingHeigth = 1 + ceillingNoise * maxFloorLevel;

                    for (int y = 0; y < height3d && y < ceillingHeigth; y++)
                    {
                        map[x, height3d - y - 1, z] = true;
                    }
                }
            }
        }

        public void AddWalls(bool[,,] map, System.Random rng)
        {
            int wall1SeedX = rng.Next(short.MaxValue);
            int wall1SeedY = rng.Next(short.MaxValue);
            int wall2SeedX = rng.Next(short.MaxValue);
            int wall2SeedY = rng.Next(short.MaxValue);
            int wall3SeedY = rng.Next(short.MaxValue);
            int wall3SeedZ = rng.Next(short.MaxValue);
            int wall4SeedY = rng.Next(short.MaxValue);
            int wall4SeedZ = rng.Next(short.MaxValue);

            float halfWidth = width3d / 2f;
            float maxTicknessX = (halfWidth * halfWidth + height3d * height3d);

            for (int y = 0; y < height3d; y++)
            {
                for (int x = 0; x < width3d; x++)
                {
                    float dx = (x - halfWidth);
                    float bonusTickness = wallRoundingFactor * (dx * dx + y * y) / maxTicknessX;

                    float wall1Noise = Mathf.PerlinNoise(x / noiseLevel + wall1SeedX, y / noiseLevel + wall1SeedY);

                    float wall1Tickness = 1 + wall1Noise * maxWallThickness * bonusTickness;

                    for (int z = 0; z < depth3d && z < wall1Tickness; z++)
                    {
                        map[x, y, z] = true;
                    }

                    float wall2Noise = Mathf.PerlinNoise(x / noiseLevel + wall2SeedX, y / noiseLevel + wall2SeedY);

                    float wall2Tickness = 1 + wall2Noise * maxWallThickness * bonusTickness;

                    for (int z = 0; z < depth3d && z < wall2Tickness; z++)
                    {
                        map[x, y, depth3d - z - 1] = true;
                    }
                }

                for (int z = 0; z < depth3d; z++)
                {
                    float dz = (z - halfWidth);
                    float bonusTickness = wallRoundingFactor * (dz * dz + y * y) / maxTicknessX;

                    float wall3Noise = Mathf.PerlinNoise(y / noiseLevel + wall3SeedY, z / noiseLevel + wall3SeedZ);

                    float wall3Tickness = 1 + wall3Noise * maxWallThickness * bonusTickness;

                    for (int x = 0; x < width3d && x < wall3Tickness; x++)
                    {
                        map[x, y, z] = true;
                    }

                    float wall4Noise = Mathf.PerlinNoise(y / noiseLevel + wall4SeedY, z / noiseLevel + wall4SeedZ);

                    float wall4Tickness = 1 + wall4Noise * maxWallThickness * bonusTickness;

                    for (int x = 0; x < width3d && x < wall4Tickness; x++)
                    {
                        map[width3d - x - 1, y, z] = true;
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
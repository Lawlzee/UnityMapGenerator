﻿using System;
using System.Threading.Tasks;
using UnityEngine;

namespace ProceduralStages
{
    [Serializable]
    public class Waller
    {
        public Wall floor = new Wall();
        public Wall walls = new Wall();
        public float wallRoundingFactor;
        public float blendFactor = 0.1f;
        public bool roundWallsToCeilling = false;

        public float[,,] AddFloor(float[,,] map)
        {
            if (!floor.enabled)
            {
                return (float[,,])map.Clone();
            }

            int seedX = MapGenerator.rng.RangeInt(0, short.MaxValue);
            int seedZ = MapGenerator.rng.RangeInt(0, short.MaxValue);

            int width3d = map.GetLength(0);
            int height3d = map.GetLength(1);
            int depth3d = map.GetLength(2);

            float[,,] floorMap = new float[width3d, height3d, depth3d];

            Parallel.For(0, width3d, x =>
            {
                for (int z = 0; z < depth3d; z++)
                {
                    float floorNoise = Mathf.Clamp01(Mathf.PerlinNoise(x / floor.noise + seedX, z / floor.noise + seedZ));

                    float floorHeight = floor.minThickness + floorNoise * (floor.maxThickness - floor.minThickness);

                    int y = 0;
                    for (; y < height3d; y++)
                    {
                        float noise = Mathf.Clamp01((floorHeight - y) * blendFactor + 0.5f);
                        if (noise == 0f)
                        {
                            break;
                        }
                        floorMap[x, y, z] = Mathf.Max(noise, map[x, y, z]);
                    }

                    for (; y < height3d; y++)
                    {
                        floorMap[x, y, z] = map[x, y, z];
                    }
                }
            });

            return floorMap;
        }

        [Serializable]
        public class Wall
        {
            public bool enabled = true;
            public float noise;
            public float minThickness = 1;
            public float maxThickness;
        }

        public void AddWalls(float[,,] map)
        {
            if (!walls.enabled)
            {
                return;
            }

            int wall1SeedX = MapGenerator.rng.RangeInt(0, short.MaxValue);
            int wall1SeedY = MapGenerator.rng.RangeInt(0, short.MaxValue);
            int wall2SeedX = MapGenerator.rng.RangeInt(0, short.MaxValue);
            int wall2SeedY = MapGenerator.rng.RangeInt(0, short.MaxValue);
            int wall3SeedY = MapGenerator.rng.RangeInt(0, short.MaxValue);
            int wall3SeedZ = MapGenerator.rng.RangeInt(0, short.MaxValue);
            int wall4SeedY = MapGenerator.rng.RangeInt(0, short.MaxValue);
            int wall4SeedZ = MapGenerator.rng.RangeInt(0, short.MaxValue);

            int width3d = map.GetLength(0);
            int height3d = map.GetLength(1);
            int depth3d = map.GetLength(2);

            float halfWidth = width3d / 2f;
            float maxTicknessX = (halfWidth * halfWidth + height3d * height3d);

            Parallel.For(0, height3d, y =>
            {
                float wallsToCeillingRoundingBonus = roundWallsToCeilling
                    ? y * y
                    : 0;

                for (int x = 0; x < width3d; x++)
                {
                    float dx = (x - halfWidth);
                    float bonusTickness = wallRoundingFactor * (dx * dx + wallsToCeillingRoundingBonus) / maxTicknessX;

                    float wall1Noise = Mathf.Clamp01(Mathf.PerlinNoise(x / walls.noise + wall1SeedX, y / walls.noise + wall1SeedY));

                    float wall1Tickness = walls.minThickness + wall1Noise * (walls.maxThickness - walls.minThickness) * bonusTickness;

                    for (int z = 0; z < depth3d; z++)
                    {
                        float noise = Mathf.Clamp01((wall1Tickness - z) * blendFactor + 0.5f);
                        if (noise == 0f)
                        {
                            break;
                        }
                        map[x, y, z] = Mathf.Max(noise, map[x, y, z]);
                    }


                    float wall2Noise = Mathf.Clamp01(Mathf.PerlinNoise(x / walls.noise + wall2SeedX, y / walls.noise + wall2SeedY));

                    float wall2Tickness = walls.minThickness + wall2Noise * (walls.maxThickness - walls.minThickness) * bonusTickness;

                    for (int z = 0; z < depth3d; z++)
                    {
                        float noise = Mathf.Clamp01((wall2Tickness - z) * blendFactor + 0.5f);
                        if (noise == 0f)
                        {
                            break;
                        }

                        int posZ = depth3d - z - 1;
                        map[x, y, posZ] = Mathf.Max(noise, map[x, y, posZ]);
                    }
                }

                for (int z = 0; z < depth3d; z++)
                {
                    float dz = (z - halfWidth);
                    float bonusTickness = wallRoundingFactor * (dz * dz + wallsToCeillingRoundingBonus) / maxTicknessX;

                    float wall3Noise = Mathf.Clamp01(Mathf.PerlinNoise(y / walls.noise + wall3SeedY, z / walls.noise + wall3SeedZ));

                    float wall3Tickness = walls.minThickness + wall3Noise * (walls.maxThickness - walls.minThickness) * bonusTickness;

                    for (int x = 0; x < width3d; x++)
                    {
                        float noise = Mathf.Clamp01((wall3Tickness - x) * blendFactor + 0.5f);
                        if (noise == 0f)
                        {
                            break;
                        }
                        map[x, y, z] = Mathf.Max(noise, map[x, y, z]);
                    }

                    float wall4Noise = Mathf.Clamp01(Mathf.PerlinNoise(y / walls.noise + wall4SeedY, z / walls.noise + wall4SeedZ));

                    float wall4Tickness = walls.minThickness + wall4Noise * (walls.maxThickness - walls.minThickness) * bonusTickness;

                    for (int x = 0; x < width3d; x++)
                    {
                        float noise = Mathf.Clamp01((wall4Tickness - x) * blendFactor + 0.5f);
                        if (noise == 0f)
                        {
                            break;
                        }

                        int posX = width3d - x - 1;
                        map[posX, y, z] = Mathf.Max(noise, map[posX, y, z]);
                    }
                }
            });
        }
    }
}
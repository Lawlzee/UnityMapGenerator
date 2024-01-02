using System;
using UnityEngine;

namespace Generator.Assets.Scripts
{
    [Serializable]
    public class Waller
    {
        public Wall floor = new Wall();
        public Wall ceilling = new Wall();
        public Wall walls = new Wall();
        public float wallRoundingFactor;

        public void AddFloorAndCeilling(bool[,,] map, System.Random rng)
        {
            int floorSeedX = rng.Next(short.MaxValue);
            int floorSeedZ = rng.Next(short.MaxValue);
            int ceillingSeedX = rng.Next(short.MaxValue);
            int ceillingSeedZ = rng.Next(short.MaxValue);

            int width3d = map.GetLength(0);
            int height3d = map.GetLength(1);
            int depth3d = map.GetLength(2);

            for (int x = 0; x < width3d; x++)
            {
                for (int z = 0; z < depth3d; z++)
                {
                    float floorNoise = Mathf.PerlinNoise(x / floor.noise + floorSeedX, z / floor.noise + floorSeedZ);

                    float floorHeight = 1 + floorNoise * floor.maxThickness;

                    for (int y = 0; y < height3d && y < floorHeight; y++)
                    {
                        map[x, y, z] = true;
                    }

                    float ceillingNoise = Mathf.PerlinNoise(x / ceilling.noise + ceillingSeedX, z / ceilling.noise + ceillingSeedZ);

                    float ceillingHeigth = 1 + ceillingNoise * ceilling.maxThickness;

                    for (int y = 0; y < height3d && y < ceillingHeigth; y++)
                    {
                        map[x, height3d - y - 1, z] = true;
                    }
                }
            }
        }

        [Serializable]
        public struct Wall
        {
            public float noise;
            public float maxThickness;
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

            int width3d = map.GetLength(0);
            int height3d = map.GetLength(1);
            int depth3d = map.GetLength(2);

            float halfWidth = width3d / 2f;
            float maxTicknessX = (halfWidth * halfWidth + height3d * height3d);

            for (int y = 0; y < height3d; y++)
            {
                for (int x = 0; x < width3d; x++)
                {
                    float dx = (x - halfWidth);
                    float bonusTickness = wallRoundingFactor * (dx * dx + y * y) / maxTicknessX;

                    float wall1Noise = Mathf.PerlinNoise(x / walls.noise + wall1SeedX, y / walls.noise + wall1SeedY);

                    float wall1Tickness = 1 + wall1Noise * walls.maxThickness * bonusTickness;

                    for (int z = 0; z < depth3d && z < wall1Tickness; z++)
                    {
                        map[x, y, z] = true;
                    }

                    float wall2Noise = Mathf.PerlinNoise(x / walls.noise + wall2SeedX, y / walls.noise + wall2SeedY);

                    float wall2Tickness = 1 + wall2Noise * walls.maxThickness * bonusTickness;

                    for (int z = 0; z < depth3d && z < wall2Tickness; z++)
                    {
                        map[x, y, depth3d - z - 1] = true;
                    }
                }

                for (int z = 0; z < depth3d; z++)
                {
                    float dz = (z - halfWidth);
                    float bonusTickness = wallRoundingFactor * (dz * dz + y * y) / maxTicknessX;

                    float wall3Noise = Mathf.PerlinNoise(y / walls.noise + wall3SeedY, z / walls.noise + wall3SeedZ);

                    float wall3Tickness = 1 + wall3Noise * walls.maxThickness * bonusTickness;

                    for (int x = 0; x < width3d && x < wall3Tickness; x++)
                    {
                        map[x, y, z] = true;
                    }

                    float wall4Noise = Mathf.PerlinNoise(y / walls.noise + wall4SeedY, z / walls.noise + wall4SeedZ);

                    float wall4Tickness = 1 + wall4Noise * walls.maxThickness * bonusTickness;

                    for (int x = 0; x < width3d && x < wall4Tickness; x++)
                    {
                        map[width3d - x - 1, y, z] = true;
                    }
                }
            }
        }
    }
}
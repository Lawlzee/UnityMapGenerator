﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProceduralStages
{
    [Serializable]
    public class Map3dNoiser
    {
        public bool enabled = true;
        public float frequency;
        [Range(0, 1)]
        public float amplitude;

        public float[,,] ToNoiseMap(bool[,,] map, System.Random rng)
        {
            int seedX = rng.Next(short.MaxValue);
            int seedY = rng.Next(short.MaxValue);
            int seedZ = rng.Next(short.MaxValue);

            int width3d = map.GetLength(0);
            int height3d = map.GetLength(1);
            int depth3d = map.GetLength(2);

            float[,,] result = new float[width3d, height3d, depth3d];

            Parallel.For(0, width3d, x =>
            {
                for (int y = 0; y < height3d; y++)
                {
                    for (int z = 0; z < depth3d; z++)
                    {
                        if (map[x, y, z])
                        {
                            result[x, y, z] = (PerlinNoise.Get(new Vector3(x + seedX, y + seedY, z + seedZ), frequency) + 1) / 2;
                        }
                        else
                        {
                            result[x, y, z] = 0;
                        }
                    }
                }
            });

            return result;
        }

        public float[,,] AddNoise(float[,,] map, System.Random rng)
        {
            if (!enabled)
            {
                return map;
            }

            int seedX = rng.Next(short.MaxValue);
            int seedY = rng.Next(short.MaxValue);
            int seedZ = rng.Next(short.MaxValue);

            int width3d = map.GetLength(0);
            int height3d = map.GetLength(1);
            int depth3d = map.GetLength(2);

            float[,,] result = new float[width3d, height3d, depth3d];

            Parallel.For(0, width3d, x =>
            {
                for (int y = 0; y < height3d; y++)
                {
                    for (int z = 0; z < depth3d; z++)
                    {
                        if (x == 0 || y == 0 || z == 0 || x == width3d - 1 || y == height3d - 1 || z == depth3d - 1)
                        {
                            result[x, y, z] = map[x, y, z];
                        }
                        else
                        {
                            float noise = amplitude * PerlinNoise.Get(new Vector3(x + seedX, y + seedY, z + seedZ), frequency);
                            result[x, y, z] = Mathf.Clamp01(map[x, y, z] + noise);
                        }
                    }
                }
            });

            return result;
        }
    }
}
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProceduralStages
{
    [Serializable]
    public class FloorWallsMixer
    {
        public float wallsBlendFrequency;
        public float wallsBlendVerticalScale;
        public float wallsCarvingRelativeMinDistance = 0.5f;
        public float wallsBlendNoiseBonus = 0;

        public float wallsFrequency;
        [Range(0, 1)]
        public float wallSurface = 0.5f;
        public float wallsVerticalScale;

        public float roofFrequency;
        public float roofVerticalScale;
        public float roofCarvingRelativeMinHeight = 0.5f;

        public float[,,] Mix(float[,,] floor, float[,,] walls)
        {
            Vector3Int size = new Vector3Int(
                floor.GetLength(0),
                floor.GetLength(1),
                floor.GetLength(2));

            int wallsSeedX = MapGenerator.rng.RangeInt(0, short.MaxValue);
            int wallsSeedY = MapGenerator.rng.RangeInt(0, short.MaxValue);
            int wallsSeedZ = MapGenerator.rng.RangeInt(0, short.MaxValue);

            int wallsBlendSeedX = MapGenerator.rng.RangeInt(0, short.MaxValue);
            int wallsBlendSeedY = MapGenerator.rng.RangeInt(0, short.MaxValue);
            int wallsBlendSeedZ = MapGenerator.rng.RangeInt(0, short.MaxValue);

            int roofSeedX = MapGenerator.rng.RangeInt(0, short.MaxValue);
            int roofSeedY = MapGenerator.rng.RangeInt(0, short.MaxValue);
            int roofSeedZ = MapGenerator.rng.RangeInt(0, short.MaxValue);

            float[,,] resultMap = new float[size.x, size.y, size.z];

            Vector2 center = new Vector3(0.5f, 0.5f);

            Parallel.For(0, size.x, x =>
            {
                for (int y = 0; y < size.y; y++)
                {
                    float relativeHeight = (float)y / size.y;

                    for (int z  = 0; z < size.z; z++)
                    {
                        //Vector3 position = new Vector3((float)x / size.x, (float)y / size.y, (float)z / size.z);

                        float wallNoise = walls[x, y, z];
                        

                        
                        float outerWallBlendNoise = (PerlinNoise.Get(new Vector3(x + wallsBlendSeedX, y * wallsBlendVerticalScale + wallsBlendSeedY, z + wallsBlendSeedZ), wallsBlendFrequency) + 1) / 2;


                        var wallDistance = (new Vector2((float)x / size.x, (float)z / size.z) - center).magnitude / Mathf.Sqrt(0.5f);

                        var minWallNoise = (wallDistance - wallsCarvingRelativeMinDistance) / (1 - wallsCarvingRelativeMinDistance);

                        if (minWallNoise > 0 && outerWallBlendNoise - wallsBlendNoiseBonus < minWallNoise)
                        {
                            resultMap[x, y, z] = wallNoise > 0.5f ? 1 - wallNoise : wallNoise;

                            //float outerWallNoise = (PerlinNoise.Get(new Vector3(x + wallsSeedX, y * wallsVerticalScale + wallsSeedY, z + wallsSeedZ), wallsFrequency) + 1) / 2;
                            //
                            //if (outerWallNoise > wallSurface)
                            //{
                            //    resultMap[x, y, z] = Math.Max(wallNoise, floorNoise);
                            //}
                            //else
                            //{
                            //    resultMap[x, y, z] = wallNoise > 0.5f ? 1 - wallNoise : wallNoise;
                            //}

                        }
                        else
                        {
                            //float roofNoise = (PerlinNoise.Get(new Vector3(x + roofSeedX, y * roofVerticalScale + roofSeedY, z + roofSeedZ), roofFrequency) + 1) / 2;

                            float maxHeight = Mathf.PerlinNoise(x / roofFrequency + roofSeedX, z / roofFrequency + roofSeedZ);
                            var scaledMaxHeight = roofCarvingRelativeMinHeight + maxHeight * (1 - roofCarvingRelativeMinHeight);

                            if (scaledMaxHeight < relativeHeight)
                            {
                                resultMap[x, y, z] = wallNoise > 0.5f ? 1 - wallNoise : wallNoise;
                            }
                            else
                            {
                                float floorNoise = floor[x, y, z];
                                resultMap[x, y, z] = Math.Max(wallNoise, floorNoise);
                            }
                            //if (roofNoise > (1 - relativeHeight) + roofCarvingRelativeMinHeight)
                            //{
                            //    resultMap[x, y, z] = wallNoise > 0.5f ? 1 - wallNoise : wallNoise;
                            //}
                            //else
                            //{
                            //    resultMap[x, y, z] = Math.Max(wallNoise, floorNoise);
                            //}
                        }
                    }
                }
            });

            return resultMap;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProceduralStages
{
    [CreateAssetMenu(fileName = "VoronoiWallGenerator", menuName = "ProceduralStages/VoronoiWallGenerator", order = 2)]
    public class VoronoiWallGenerator : ScriptableObject
    {
        public FBM wallNoiseFBM;
        [Range(0, 1)]
        public float wallSurfaceLevel = 0.5f;
        public ThreadSafeCurve wallCurve;

        public FBM carverNoiseFBM;
        [Range(0, 1)]
        public float carverSurfaceLevel = 0.5f;
        public ThreadSafeCurve carverCurve;
        public float carverVerticalScale;

        public ThreadSafeCurve finalCurve;

        public float voronoiHorizontalScale;
        public float voronoiVerticalScale;

        public float[,,] Create(Vector3Int size)
        {
            int wallSeedX = MapGenerator.rng.RangeInt(0, short.MaxValue);
            int wallSeedZ = MapGenerator.rng.RangeInt(0, short.MaxValue);

            int carverSeedX = MapGenerator.rng.RangeInt(0, short.MaxValue);
            int carverSeedY = MapGenerator.rng.RangeInt(0, short.MaxValue);
            int carverSeedZ = MapGenerator.rng.RangeInt(0, short.MaxValue);

            float voronoiHorizontalScaleReciprocal = 1 / voronoiHorizontalScale;
            float voronoiVerticalScaleReciprocal = 1 / voronoiVerticalScale;

            float[,,] map = new float[size.x, size.y, size.z];

            Parallel.For(0, size.x, x =>
            {
                for (int y = 0; y < size.y; y++)
                {
                    for (int z = 0; z < size.z; z++)
                    {
                        Vector3 uvw = new Vector3(
                            x * voronoiHorizontalScale,
                            y * voronoiVerticalScale,
                            z * voronoiHorizontalScale);

                        Vector3 uvwIntegral = new Vector3Int(
                            Mathf.FloorToInt(uvw.x), 
                            Mathf.FloorToInt(uvw.y), 
                            Mathf.FloorToInt(uvw.z));

                        Vector3 uvwFractional = uvw - uvwIntegral;

                        float minDistance1 = float.MaxValue;
                        Vector3 minDistancePos1 = default;

                        float minDistance2 = float.MaxValue;
                        Vector3 minDistancePos2 = default;

                        for (int i = -1; i <= 1; i++)
                        {
                            for (int j = -1; j <= 1; j++)
                            {
                                for (int k = -1; k <= 1; k++)
                                {
                                    Vector3 neighbor = new Vector3(i, j, k);
                                    Vector3 pos = uvwIntegral + neighbor;
                                    Vector3 displacement = new Vector3(
                                        pos.x * RandomPG._randomScale.x % 1,
                                        pos.y * RandomPG._randomScale.y % 1,
                                        pos.z * RandomPG._randomScale.z % 1);
                                    Vector3 diff = neighbor + displacement - uvwFractional;
                                    float dist = diff.sqrMagnitude;
                                    if (dist < minDistance1)
                                    {
                                        minDistancePos2 = minDistancePos1;
                                        minDistance2 = minDistance1;

                                        minDistancePos1 = pos + displacement;
                                        minDistance1 = dist;
                                    }
                                    else if (dist < minDistance2)
                                    {
                                        minDistancePos2 = pos + displacement;
                                        minDistance2 = dist;
                                    }
                                }
                            }
                        }

                        minDistancePos1.x *= voronoiHorizontalScaleReciprocal;
                        minDistancePos1.y *= voronoiVerticalScaleReciprocal;
                        minDistancePos1.z *= voronoiHorizontalScaleReciprocal;

                        minDistancePos2.x *= voronoiHorizontalScaleReciprocal;
                        minDistancePos2.y *= voronoiVerticalScaleReciprocal;
                        minDistancePos2.z *= voronoiHorizontalScaleReciprocal;

                        float wallNoise1 = 0.5f * (wallNoiseFBM.Evaluate(minDistancePos1.x + wallSeedX, minDistancePos1.z + wallSeedZ) + 1);
                        bool isWall1 = wallNoise1 > wallSurfaceLevel;
                        if (isWall1)
                        {
                            float carverNoise1 = 0.5f * (carverNoiseFBM.Evaluate(minDistancePos1.x + carverSeedX, carverVerticalScale * minDistancePos1.y + carverSeedY, minDistancePos1.z + carverSeedZ) + 1);
                            isWall1 = carverNoise1 > carverSurfaceLevel;
                        }
                        
                        float wallNoise2 = 0.5f * (wallNoiseFBM.Evaluate(minDistancePos2.x + wallSeedX, minDistancePos2.z + wallSeedZ) + 1);
                        bool isWall2 = wallNoise2 > wallSurfaceLevel;
                        if (isWall2)
                        {
                            float carverNoise2 = 0.5f * (carverNoiseFBM.Evaluate(minDistancePos2.x + carverSeedX, carverVerticalScale * minDistancePos2.y + carverSeedY, minDistancePos2.z + carverSeedZ) + 1);
                            isWall2 = carverNoise2 > carverSurfaceLevel;
                        }
                        
                        if (isWall1 && isWall2)
                        {
                            map[x, y, z] = 1;
                        }
                        else if (isWall1)
                        {
                            map[x, y, z] = minDistance2 / (minDistance1 + minDistance2);
                        }
                        else if (isWall2)
                        {
                            map[x, y, z] = minDistance1 / (minDistance1 + minDistance2);
                        }

                        //map[x, y, z] = finalCurve.Evaluate(Math.Min(wallNoise1, carverNoise1));
                    }
                }
            });

            return map;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityMeshSimplifier;

namespace ProceduralStages
{
    [CreateAssetMenu(fileName = "BasaltGenerator", menuName = "ProceduralStages/BasaltGenerator", order = 2)]
    public class BasaltGenerator : TerrainGenerator
    {
        public float voronoiScale;
        public FBM heightMap;
        public float derivativeDisplacementStrength;
        public float derivativeMax;
        public float voronoirBlendFactor;
        public ThreadSafeCurve heightMapCurve;
        public float floorMinHeigth;
        public float floorMaxHeigth;
        [Range(0, 1)]
        public float ellipsisDistancePower = 0.5f;
        public ThreadSafeCurve floorMinNoiseByEclipseDistanceCurve;
        public ThreadSafeCurve floorMaxNoiseByEclipseDistanceCurve;

        public FBM peaksHeightMap;
        public ThreadSafeCurve peaksHeightCurve;
        public ThreadSafeCurve peaksHeightBonusByDistanceCurve;

        public FBM volcanoHeightMap;
        public ThreadSafeCurve volcanoHeightCurve;
        public ThreadSafeCurve volcanoMinHeightByDistanceCurve;
        public ThreadSafeCurve volcanoMaxHeightByDistanceCurve;

        public FBM volcanoRoomHeightMap;
        public ThreadSafeCurve volcanoRoomHeightCurve;
        public ThreadSafeCurve volcanoRoomHeightByDistanceCurve;
        public float volcanoMinRoomHeight;
        public float volcanoMinColumnHeight;

        public override Terrain Generate()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            //float[,,] map3d = RandomWalk(MapGenerator.instance.stageSize);
            //float[,,] map3d = GenerateVoronoi(MapGenerator.instance.stageSize);
            (float[,,] map3d, float[,,] floorlessMap) = GenerateIsland(MapGenerator.instance.stageSize);
            LogStats("GenerateVoronoi");
            //float[,,] map3d = AngleRandomWalk(MapGenerator.instance.stageSize);

            //map3d = smoother.SmoothMap(map3d);
            LogStats("smoother.SmoothMap");

            var meshResult = MarchingCubes.CreateMesh(map3d, MapGenerator.instance.mapScale);
            LogStats("marchingCubes");

            //MeshSimplifier simplifier = new MeshSimplifier(unOptimisedMesh);
            //simplifier.SimplifyMesh(MapGenerator.instance.meshQuality);
            //var optimisedMesh = simplifier.ToMesh();
            //LogStats("MeshSimplifier");

            return new Terrain
            {
                meshResult = meshResult,
                floorlessDensityMap = floorlessMap,
                densityMap = map3d,
                maxGroundheight = float.MaxValue
            };

            void LogStats(string name)
            {
                Log.Debug($"{name}: {stopwatch.Elapsed}");
                stopwatch.Restart();
            }
        }

        private (float[,,] map, float[,,] floorlessMap) GenerateIsland(Vector3Int size)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            float[,,] map = new float[size.x, size.y, size.z];
            bool[,] wallMap = new bool[size.x, size.z];

            int heightMapSeedX = MapGenerator.rng.RangeInt(0, short.MaxValue);
            int heightMapSeedZ = MapGenerator.rng.RangeInt(0, short.MaxValue);

            int peekSeedX = MapGenerator.rng.RangeInt(0, short.MaxValue);
            int peekSeedZ = MapGenerator.rng.RangeInt(0, short.MaxValue);

            int volcanoSeedX = MapGenerator.rng.RangeInt(0, short.MaxValue);
            int volcanoSeedZ = MapGenerator.rng.RangeInt(0, short.MaxValue);

            int volcanoRoomSeedX = MapGenerator.rng.RangeInt(0, short.MaxValue);
            int volcanoRoomSeedZ = MapGenerator.rng.RangeInt(0, short.MaxValue);

            Vector2 center = new Vector3(size.x / 2f, size.z / 2f);

            Parallel.For(0, size.x, x =>
            {
                for (int z = 0; z < size.z; z++)
                {
                    var islandFloor = ComputeIslandFloor(new Vector2(x, z));

                    float islandScaledFloorHeight = floorMinHeigth + islandFloor.floorHeight * (floorMaxHeigth - floorMinHeigth);

                    Vector2 uv = new Vector2(
                        x * voronoiScale,
                        z * voronoiScale);

                    Vector2 uvIntegral = new Vector2Int(Mathf.FloorToInt(uv.x), Mathf.FloorToInt(uv.y));
                    Vector2 uvFractional = uv - uvIntegral;

                    float minDistance = float.MaxValue;
                    Vector2 minPeakPos = new Vector2();

                    for (int j = -1; j <= 1; j++)
                    {
                        for (int i = -1; i <= 1; i++)
                        {
                            Vector2 neighbor = new Vector2(i, j);
                            Vector2 pos = uvIntegral + neighbor;
                            Vector2 displacement = RandomPG.Random2(pos);
                            Vector2 diff = neighbor + displacement - uvFractional;
                            float dist = diff.sqrMagnitude;
                            if (dist < minDistance)
                            {
                                minPeakPos = pos + displacement;
                                minDistance = dist;
                            }
                        }
                    }

                    Vector2 peakPosition = minPeakPos / voronoiScale;

                    var peakFloorHeight = ComputeIslandFloor(peakPosition);

                    float peaksHeightScale = peakFloorHeight.ellipsisDistance * peakFloorHeight.floorHeight + peaksHeightBonusByDistanceCurve.Evaluate(peakFloorHeight.ellipsisDistance);
                    float peaksScaledFloorHeight = size.y * peaksHeightCurve.Evaluate(peaksHeightScale * 0.5f * (peaksHeightMap.Evaluate(peakPosition.x + peekSeedX, peakPosition.y + peekSeedZ) + 1));

                    float volcanoNoise = volcanoHeightCurve.Evaluate(0.5f * (volcanoHeightMap.Evaluate(peakPosition.x + volcanoSeedX, peakPosition.y + volcanoSeedZ) + 1));
                    float volcanoScaledNoise = Mathf.Lerp(
                        volcanoMinHeightByDistanceCurve.Evaluate(peakFloorHeight.ellipsisDistance),
                        volcanoMaxHeightByDistanceCurve.Evaluate(peakFloorHeight.ellipsisDistance),
                        volcanoNoise);

                    float volcanoScaledFloorHeight = size.y * volcanoScaledNoise;

                    float basaltScaledFloorHeight = Math.Max(peaksScaledFloorHeight, volcanoScaledFloorHeight);

                    float roomNoise = 0.5f * (volcanoRoomHeightMap.Evaluate(peakPosition.x + volcanoRoomSeedX, peakPosition.y + volcanoRoomSeedZ) + 1);
                    float roomDistanceCoefficient = volcanoRoomHeightByDistanceCurve.Evaluate(peakFloorHeight.ellipsisDistance);
                    float basaltScaledCeilHeight = size.y * roomDistanceCoefficient * volcanoRoomHeightCurve.Evaluate(roomNoise);

                    if (basaltScaledCeilHeight < volcanoMinRoomHeight)
                    {
                        basaltScaledCeilHeight = 0;
                    }

                    if (basaltScaledFloorHeight - basaltScaledCeilHeight < volcanoMinColumnHeight)
                    {
                        basaltScaledFloorHeight = 0;
                    }

                    bool isWall;
                    if (basaltScaledFloorHeight < islandScaledFloorHeight || basaltScaledCeilHeight > basaltScaledFloorHeight)
                    {
                        isWall = false;

                        for (int y = 0; y < size.y - 1; y++)
                        {
                            float floorNoise = Mathf.Clamp01((islandScaledFloorHeight - y) * voronoirBlendFactor + 0.5f);
                            if (floorNoise == 0f)
                            {
                                break;
                            }
                            map[x, y, z] = floorNoise;
                        }
                    }
                    else
                    {
                        isWall = basaltScaledCeilHeight < islandScaledFloorHeight;
                        if (isWall)
                        {
                            for (int y = 0; y < size.y - 1; y++)
                            {
                                float basaltNoise = Mathf.Clamp01((basaltScaledFloorHeight - y) * voronoirBlendFactor + 0.5f);
                                if (basaltNoise == 0f)
                                {
                                    break;
                                }
                                map[x, y, z] = basaltNoise;
                            }
                        }
                        else
                        {
                            float airCenter = (islandScaledFloorHeight + basaltScaledCeilHeight) / 2f;
                            float basaltCenterY = (basaltScaledCeilHeight + basaltScaledFloorHeight) / 2f;

                            for (int y = 0; y < size.y - 1; y++)
                            {
                                if (y < airCenter)
                                {
                                    map[x, y, z] = Mathf.Clamp01((islandScaledFloorHeight - y) * voronoirBlendFactor + 0.5f);
                                }
                                else if (y < basaltCenterY)
                                {
                                    map[x, y, z] = Mathf.Clamp01((y - basaltScaledCeilHeight) * voronoirBlendFactor + 0.5f);
                                }
                                else
                                {
                                    float basaltNoise = Mathf.Clamp01((basaltScaledFloorHeight - y) * voronoirBlendFactor + 0.5f);
                                    if (basaltNoise == 0f)
                                    {
                                        break;
                                    }

                                    map[x, y, z] = basaltNoise;
                                }
                            }
                        }
                    }

                    if (isWall)
                    {
                        //for (int i = -2; i <= 2; i++)
                        //{
                        //    int posX = Mathf.Clamp(x + i, 0, size.x - 1);
                        //    for (int j = -2; j <= 2; j++)
                        //    {
                        //        int posZ = Mathf.Clamp(z + j, 0, size.z - 1);
                        wallMap[x, z] = true;
                        //    }
                        //}
                    }

                    (float ellipsisDistance, float floorHeight) ComputeIslandFloor(Vector2 pos)
                    {
                        float dx = pos.x - center.x;
                        float dz = pos.y - center.y;

                        float ellipsisDistance = Mathf.Pow((dx * dx) / (center.x * center.x) + (dz * dz) / (center.y * center.y), ellipsisDistancePower);
                        float heightMapNoise = 0.5f * (heightMap.Evaluate(pos.x + heightMapSeedX, pos.y + heightMapSeedZ) + 1);
                        //float scaledNoise = heightMapNoise * (1 - Mathf.Clamp01(ellipsisDistance));
                        float min = floorMinNoiseByEclipseDistanceCurve.Evaluate(ellipsisDistance);
                        float max = floorMaxNoiseByEclipseDistanceCurve.Evaluate(ellipsisDistance);
                        float scaledNoise = Mathf.Lerp(min, max, heightMapNoise);

                        float floorHeight = heightMapCurve.Evaluate(scaledNoise);

                        return (ellipsisDistance, floorHeight);
                    }
                }
            });

            LogStats("heightmap");

            float[,,] floorlessMap = new float[size.x, size.y, size.z];

            Parallel.For(0, size.x, x =>
            {
                for (int z = 0; z < size.z; z++)
                {
                    for (int y = 0; y < size.y; y++)
                    {
                        //todo: something better
                        floorlessMap[x, y, z] = wallMap[x, z]
                            ? 1
                            : 0.5f * RandomPG.Random(new Vector2(x, z));
                    }
                }
            });

            LogStats("floorlessMap");

            return (map, floorlessMap);

            void LogStats(string name)
            {
                Log.Debug($"{name}: {stopwatch.Elapsed}");
                stopwatch.Restart();
            }
        }

        private float[,,] GenerateVoronoi(Vector3Int size)
        {
            float[,,] map = new float[size.x, size.y, size.z];

            int seedX = MapGenerator.rng.RangeInt(0, short.MaxValue);
            int seedZ = MapGenerator.rng.RangeInt(0, short.MaxValue);

            float max = float.MinValue;

            Parallel.For(0, size.x, x =>
            {
                for (int z = 0; z < size.z; z++)
                {
                    var noiseWithDerivative = heightMap.EvaluateWithDerivative(x * voronoiScale, 0, z * voronoiScale);
                    float noiseHeight = heightMapCurve.Evaluate(noiseWithDerivative.Noise);

                    //Vector2 uv = new Vector2(
                    //    x * voronoiScale + noiseHeight * derivativeDisplacementStrength * (derivativeMax - noiseWithDerivative.Derivative.x), 
                    //    z * voronoiScale + noiseHeight * derivativeDisplacementStrength * (derivativeMax - noiseWithDerivative.Derivative.z));

                    var derivativeNormalised = noiseWithDerivative.Derivative.normalized;

                    Vector2 uv = new Vector2(
                        x * voronoiScale + derivativeNormalised.x * (1 - noiseHeight) * derivativeDisplacementStrength,
                        z * voronoiScale + derivativeNormalised.z * (1 - noiseHeight) * derivativeDisplacementStrength);

                    Vector2 uvIntegral = new Vector2Int(Mathf.FloorToInt(uv.x), Mathf.FloorToInt(uv.y));
                    Vector2 uvFractional = uv - uvIntegral;

                    float minDistance1 = float.MaxValue;
                    Vector2 bestPos1 = new Vector2();

                    //float minDistance2 = float.MaxValue;
                    //Vector2 bestPos2 = new Vector2();

                    for (int j = -1; j <= 1; j++)
                    {
                        for (int i = -1; i <= 1; i++)
                        {
                            Vector2 neighbor = new Vector2(i, j);
                            Vector2 pos = uvIntegral + neighbor;
                            Vector2 displacement = RandomPG.Random2(pos);
                            Vector2 diff = neighbor + displacement - uvFractional;
                            float dist = diff.sqrMagnitude;
                            if (dist < minDistance1)
                            {
                                //bestPos2 = bestPos1;
                                //minDistance2 = minDistance1;

                                bestPos1 = pos + displacement;
                                minDistance1 = dist;
                            }
                            //else if (dist < minDistance2)
                            //{
                            //    bestPos2 = pos + displacement;
                            //    minDistance2 = dist;
                            //}
                        }
                    }

                    //float voronoiNoise = Mathf.Sqrt(minDistance2) / (Mathf.Sqrt(minDistance2) + Mathf.Sqrt(minDistance1));

                    max = Math.Max(max, 0.5f * (heightMap.Evaluate(bestPos1.x + seedX, 0, bestPos1.y + seedZ) + 1));
                    float floorHeight = heightMapCurve.Evaluate(0.5f * (heightMap.Evaluate(bestPos1.x + seedX, 0, bestPos1.y + seedZ) + 1));
                    float scaledFloorHeight = floorMinHeigth + Mathf.Clamp01(floorHeight) * (floorMaxHeigth - floorMinHeigth);

                    for (int y = 0; y < size.y; y++)
                    {
                        float noise = Mathf.Clamp01((scaledFloorHeight - y) * voronoirBlendFactor + 0.5f);
                        if (noise == 0f)
                        {
                            break;
                        }
                        map[x, y, z] = noise;
                    }
                }
            });

            Log.Debug("MAX: " + max);

            return map;


        }
    }
}

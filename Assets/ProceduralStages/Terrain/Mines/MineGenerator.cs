﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityMeshSimplifier;

namespace ProceduralStages
{
    [CreateAssetMenu(fileName = "MineGenerator", menuName = "ProceduralStages/MineGenerator", order = 2)]
    public class MineGenerator : TerrainGenerator
    {
        public override TerrainType TerrainType => TerrainType.Mines;

        public FBM heightMapNoise;
        public ThreadSafeCurve heightCurve;
        public BenchesHeightCurve[] benchesHeightCurves;
        public ThreadSafeCurve benchesWidthByHeightCurve;
        public float floorBlendFactor = 0.1f;

        public ThreadSafeCurve densityCurve;

        public ThreadSafeCurve caveCurve;
        public ThreadSafeCurve caveYDerivativeBonus;
        public FBM spaghettiNoise;

        public override Terrain Generate()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            Vector3Int size = MapGenerator.instance.stageSize;

            (float[,] map2d, float min, float max) = GenerateHeightMap(size);
            LogStats("GenerateHeightMap");
            
            
            float[,,] map3d = To3DMap(map2d, size, min, max);
            LogStats("To3DMap");
            
            //CarveCaves(map3d, size);
            //LogStats("CarveCaves");

            //var map3d = CarveCaves2(size);

            float[,,] floorDensityMap = ComputeFloorDensityMap(map3d, size);
            LogStats("floorDensityMap");

            var unOptimisedMesh = MarchingCubes.CreateMesh(map3d, MapGenerator.instance.mapScale);
            LogStats("marchingCubes");

            MeshSimplifier simplifier = new MeshSimplifier(unOptimisedMesh);
            simplifier.SimplifyMesh(MapGenerator.instance.meshQuality);
            var optimisedMesh = simplifier.ToMesh();
            LogStats("MeshSimplifier");


            return new Terrain
            {
                meshResult = new MeshResult
                {
                    mesh = optimisedMesh,
                    normals = optimisedMesh.normals,
                    triangles = optimisedMesh.triangles,
                    vertices = optimisedMesh.vertices
                },
                floorlessDensityMap = floorDensityMap,
                densityMap = map3d,
                maxGroundheight = float.MaxValue
            };

            void LogStats(string name)
            {
                Log.Debug($"{name}: {stopwatch.Elapsed}");
                stopwatch.Restart();
            }
        }

        private (float[,] map, float min, float max) GenerateHeightMap(Vector3Int size)
        {
            int seedX = MapGenerator.rng.RangeInt(0, short.MaxValue);
            int seedZ = MapGenerator.rng.RangeInt(0, short.MaxValue);

            float[,] map = new float[size.x, size.z];
            float[] mins = new float[size.x];
            float[] maxs = new float[size.x];

            Parallel.For(0, size.x, x =>
            {
                float min = float.MaxValue;
                float max = float.MinValue;

                for (int z = 0; z < size.z; z++)
                {
                    (float noise, Vector2 derivative) = heightMapNoise.EvaluateWithDerivative(x + seedX, z + seedZ);
                    float noise01 = heightCurve.Evaluate((noise + 1) * 0.5f);

                    float slopeAngle = Mathf.Atan2(derivative.y, derivative.x);
                    float slopeAngle01 = (slopeAngle + Mathf.PI) / (2 * Mathf.PI);

                    float benchWidth = benchesWidthByHeightCurve.Evaluate(noise01);

                    float benchCrest1 = 0;
                    float benchCrest2 = 1;
                    for (int i = 0; i < benchesHeightCurves.Length; i++)
                    {
                        var benchesHeightCurve = benchesHeightCurves[i].curve;
                        float benchPos = benchesHeightCurves[i].direction == BenchDirection.AntiClockWise
                            ? slopeAngle01
                            : 1 - slopeAngle01;

                        for (; benchPos < benchesHeightCurve._max; benchPos++)
                        {
                            float benchCrest = benchesHeightCurve.Evaluate(benchPos);

                            if (benchCrest <= noise01)
                            {
                                benchCrest1 = Mathf.Max(benchCrest1, benchCrest);
                            }
                            else
                            {
                                benchCrest2 = Mathf.Min(benchCrest2, benchCrest);
                            }
                        }
                    }

                    float benchToe1 = benchCrest1 + benchWidth;

                    float height;
                    if (benchCrest1 == 0 || benchCrest2 == 1)
                    {
                        height = noise01;
                    }
                    if (noise01 < benchToe1)
                    {
                        height = benchCrest1;
                    }
                    else if (benchToe1 > benchCrest2)
                    {
                        height = benchCrest2;
                    }
                    else
                    {
                        float benchWallWidth = benchCrest2 - benchToe1;
                        //float crestDistance = benchCrest2 - benchCrest1;

                        float t = (noise01 - benchToe1) / benchWallWidth;

                        //float relativeDistance = benchWallWidth / crestDistance;
                        //height = benchCrest1 + (noise01 - benchToe1) * relativeDistance;
                        height = Mathf.Lerp(benchCrest1, benchCrest2, t);
                    }

                    min = Mathf.Min(height, min);
                    max = Mathf.Max(height, max);

                    map[x, z] = height;
                }

                mins[x] = min;
                maxs[x] = max;
            });
            return (map, mins.Min(), maxs.Max());
        }

        private float[,,] To3DMap(float[,] map2d, Vector3Int size, float min, float max)
        {
            float[,,] map = new float[size.x, size.y, size.z];
            float coefficient = 1 / (max - min);

            Parallel.For(0, size.x, x =>
            {
                for (int z = 0; z < size.z; z++)
                {
                    for (int y = 0; y < size.y; y++)
                    {
                        float floorTickness = 1 + (map2d[x, z] - min) * coefficient * size.y;
                        float noise = Mathf.Clamp01((floorTickness - y) * floorBlendFactor + 0.5f);
                        if (noise == 0f)
                        {
                            break;
                        }

                        map[x, y, z] = noise;
                    }
                }
            });

            return map;
        }

        private void CarveCaves(float[,,] map3d, Vector3Int size)
        {
            int seed1X = MapGenerator.rng.RangeInt(0, short.MaxValue);
            int seed1Y = MapGenerator.rng.RangeInt(0, short.MaxValue);
            int seed1Z = MapGenerator.rng.RangeInt(0, short.MaxValue);

            int seed2X = MapGenerator.rng.RangeInt(0, short.MaxValue);
            int seed2Y = MapGenerator.rng.RangeInt(0, short.MaxValue);
            int seed2Z = MapGenerator.rng.RangeInt(0, short.MaxValue);

            Parallel.For(0, size.y, y =>
            {
                for (int x = 0; x < size.x; x++)
                {
                    for (int z = 0; z < size.z; z++)
                    {
                        (float noise1, Vector3 derivative1) = spaghettiNoise.EvaluateWithDerivative(x + seed1X, y + seed1Y, z + seed1Z);
                        (float noise2, Vector3 derivative2) = spaghettiNoise.EvaluateWithDerivative(x + seed2X, y + seed2Y, z + seed2Z);
                        Vector3 fullNoise = derivative1 + derivative2;

                        float noiseAngle = (2 * Mathf.Atan2(fullNoise.y, 1)) / Mathf.PI;

                        float noise = 0.5f * (noise1 * noise1 + noise2 * noise2);

                        float cavelNoise = Mathf.Clamp01(caveCurve.Evaluate(noise) + caveYDerivativeBonus.Evaluate(noiseAngle));


                        map3d[x, y, z] = Math.Min(cavelNoise, map3d[x, y, z]);
                    }
                }
            });
        }

        private float[,,] CarveCaves2(Vector3Int size)
        {
            float[,,] map = new float[size.x, size.y, size.z];

            int seed1X = MapGenerator.rng.RangeInt(0, short.MaxValue);
            int seed1Y = MapGenerator.rng.RangeInt(0, short.MaxValue);
            int seed1Z = MapGenerator.rng.RangeInt(0, short.MaxValue);

            int seed2X = MapGenerator.rng.RangeInt(0, short.MaxValue);
            int seed2Y = MapGenerator.rng.RangeInt(0, short.MaxValue);
            int seed2Z = MapGenerator.rng.RangeInt(0, short.MaxValue);

            Parallel.For(0, size.y, y =>
            {
                for (int x = 0; x < size.x; x++)
                {
                    for (int z = 0; z < size.z; z++)
                    {
                        (float noise1, Vector3 derivative1) = spaghettiNoise.EvaluateWithDerivative(x + seed1X, y + seed1Y, z + seed1Z);
                        (float noise2, Vector3 derivative2) = spaghettiNoise.EvaluateWithDerivative(x + seed2X, y + seed2Y, z + seed2Z);
                        Vector3 fullNoise = derivative1 + derivative2;

                        float noiseAngle = (2 * Mathf.Atan2(fullNoise.y, 1)) / Mathf.PI;

                        float noise = 0.5f * (noise1 * noise1 + noise2 * noise2);

                        float cavelNoise = Mathf.Clamp01(caveCurve.Evaluate(noise) + caveYDerivativeBonus.Evaluate(noiseAngle));


                        map[x, y, z] = cavelNoise;
                    }
                }
            });

            return map;
        }

        private float[,,] ComputeFloorDensityMap(float[,,] map3d, Vector3Int size)
        {
            float[,,] densityMap = new float[size.x, size.y, size.z];

            Parallel.For(0, size.y, y =>
            {
                for (int x = 0; x < size.x; x++)
                {
                    for (int z = 0; z < size.z; z++)
                    {
                        if (x == 0 || z == 0 || x == size.x - 1 || z == size.z - 1)
                        {
                            densityMap[x, y, z] = 1;
                            continue;
                        }

                        float baseDensity = map3d[x, y, z];

                        float min = float.MaxValue;
                        float max = float.MinValue;

                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dz = -1; dz <= 1; dz++)
                            {
                                float value = map3d[x + dx, y, z + dz];
                                float delta = value - baseDensity;

                                min = Mathf.Min(delta, min);
                                max = Mathf.Max(delta, max);
                            }
                        }

                        float density = -min > max ? min : max;
                        densityMap[x, y, z] = densityCurve.Evaluate(density);
                    }
                }
            });

            return densityMap;
        }
    }
}
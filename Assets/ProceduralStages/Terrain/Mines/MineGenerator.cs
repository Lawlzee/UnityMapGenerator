using System;
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
        public ThreadSafeCurve benchesHeightCurve;
        public ThreadSafeCurve benchesWidthByHeightCurve;
        public float floorBlendFactor = 0.1f;

        public override Terrain Generate()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            float[,] map2d = GenerateHeightMap(MapGenerator.instance.stageSize);
            LogStats("GenerateHeightMap");


            float[,,] map3d = To3DMap(map2d, MapGenerator.instance.stageSize);
            LogStats("wallGenerator");

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
                floorlessDensityMap = map3d,
                densityMap = map3d,
                maxGroundheight = float.MaxValue
            };

            void LogStats(string name)
            {
                Log.Debug($"{name}: {stopwatch.Elapsed}");
                stopwatch.Restart();
            }
        }

        private float[,] GenerateHeightMap(Vector3Int size)
        {
            int seedX = MapGenerator.rng.RangeInt(0, short.MaxValue);
            int seedZ = MapGenerator.rng.RangeInt(0, short.MaxValue);

            float[,] map = new float[size.x, size.z];

            float min = float.MaxValue;
            float max = float.MinValue;

            Parallel.For(0, size.x, x =>
            {
                

                for (int z = 0; z < size.z; z++)
                {
                    (float noise, Vector2 derivative) = heightMapNoise.EvaluateWithDerivative(x + seedX, z + seedZ);
                    float noise01 = Mathf.Clamp01(heightCurve.Evaluate((noise + 1) * 0.5f)); ;
                    
                    float slopeAngle = Mathf.Atan2(derivative.y, derivative.x);
                    float slopeAngle01 = (slopeAngle + Mathf.PI) / (2 * Mathf.PI);

                    min = Mathf.Min(min, slopeAngle01);
                    max = Mathf.Max(max, slopeAngle01);

                    //map[x, z] = Mathf.Clamp01(heightCurve.Evaluate(slopeAngle01));
                    //continue;

                    float benchWidth = benchesWidthByHeightCurve.Evaluate(noise01);

                    float benchCrest1 = 0;
                    float benchCrest2 = 1;
                    for (float benchPos = slopeAngle01; benchPos < benchesHeightCurve._max; benchPos++)
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

                    float benchToe1 = benchCrest1 + benchWidth;

                    float height;
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

                    map[x, z] = Mathf.Clamp01(height);
                }
            });

            Log.Debug("min: " + min);
            Log.Debug("max: " + max);

            return map;
        }

        private float[,,] To3DMap(float[,] map2d, Vector3Int size)
        {
            float[,,] map = new float[size.x, size.y, size.z];

            Parallel.For(0, size.x, x =>
            {
                for (int z = 0; z < size.z; z++)
                {
                    for (int y = 0; y < size.y; y++)
                    {
                        float floorTickness = 1 + map2d[x, z] * size.y;
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
    }
}

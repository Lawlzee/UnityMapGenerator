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

        public override Terrain Generate()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            //float[,,] map3d = RandomWalk(MapGenerator.instance.stageSize);
            float[,,] map3d = GenerateVoronoi(MapGenerator.instance.stageSize);
            LogStats("GenerateVoronoi");
            //float[,,] map3d = AngleRandomWalk(MapGenerator.instance.stageSize);

            //map3d = smoother.SmoothMap(map3d);
            LogStats("smoother.SmoothMap");

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

                    Vector2 uvIntergral = new Vector2Int(Mathf.FloorToInt(uv.x), Mathf.FloorToInt(uv.y));
                    Vector2 uvFractional = uv - uvIntergral;

                    float minDistance1 = float.MaxValue;
                    Vector2 bestPos1 = new Vector2();

                    //float minDistance2 = float.MaxValue;
                    //Vector2 bestPos2 = new Vector2();

                    for (int j = -1; j <= 1; j++)
                    {
                        for (int i = -1; i <= 1; i++)
                        {
                            Vector2 neighbor = new Vector2(i, j);
                            Vector2 pos = uvIntergral + neighbor;
                            Vector2 displacement = Random2(pos);
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

            //https://github.com/patriciogonzalezvivo/lygia/blob/main/generative/random.hlsl
            Vector2 Random2(Vector2 point)
            {
                Vector3 randomScale = new Vector3(443.897f, 441.423f, 0.0973f);

                Vector3 point2 = (new Vector3(point.x * randomScale.x, point.y * randomScale.y, point.x * randomScale.z)).Frac();
                var dot = Vector3.Dot(point2, new Vector3(point2.y + 19.19f, point2.z + 19.19f, point2.x + 19.19f));
                Vector3 point3 = new Vector3(point2.x + dot, point2.y + dot, point2.z + dot);
                return ((new Vector2(point3.x, point3.x) + new Vector2(point3.y, point3.z)) * new Vector2(point3.z, point3.y)).Frac();
            }
        }
    }
}

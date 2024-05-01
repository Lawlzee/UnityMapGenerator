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
        public int seedToSearch;
        public float minPlayableArea;
        public float maxPlayableArea;
        [Range(-1f, 1f)]
        public float wallNoiseLevel;
        public Vector2Int outerWallBuffer;

        public FBM heightMapNoise;
        public ThreadSafeCurve heightCurve;
        public BenchesHeightCurve[] benchesHeightCurves;
        public ThreadSafeCurve benchesWidthByHeightCurve;
        public ThreadSafeCurve bonusNoiseByEllipsisDistance;

        public float floorBlendFactor = 0.1f;

        public ThreadSafeCurve densityCurve;

        public ThreadSafeCurve caveCurve;
        public ThreadSafeCurve caveYDerivativeBonus;
        public FBM spaghettiNoise;

        public FBM wallCurvingNoise;
        [Range(0f, 1f)]
        public float wallCurvingMinNoise;
        public float wallCurvingVecticalScale;

        public CellularAutomata3d smoother;

        public override Terrain Generate()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            (Vector2Int seed, Vector3Int size) = FindSeed(MapGenerator.instance.stageSize);
            LogStats("FindSeed");
            MapGenerator.instance.stageSize = size;

            (float[,] map2d, float min, float max) = GenerateHeightMap(seed, size);
            LogStats("GenerateHeightMap");


            float[,,] map3d = To3DMap(map2d, size, min, max);
            LogStats("To3DMap");

            //CarveCaves(map3d, size);
            //LogStats("CarveCaves");

            //var map3d = CarveCaves2(size);

            map3d = smoother.SmoothMap(map3d);
            LogStats("smoother.SmoothMap");


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

        private (Vector2Int seed, Vector3Int size) FindSeed(Vector3Int targetSize)
        {
            while (true)
            {
                int targetArea = (targetSize.x + 2 * outerWallBuffer.x) * (targetSize.y + 2 * outerWallBuffer.y);
                float minArea = targetArea * minPlayableArea;
                float maxArea = targetArea * maxPlayableArea;

                Vector2Int[] seeds = new Vector2Int[seedToSearch];
                Vector2Int[] mapSizes = new Vector2Int[seedToSearch];
                float[] scores = new float[seedToSearch];

                for (int i = 0; i < seedToSearch; i++)
                {
                    seeds[i] = new Vector2Int(
                        MapGenerator.rng.RangeInt(0, short.MaxValue),
                        MapGenerator.rng.RangeInt(0, short.MaxValue));
                }

                Parallel.For(0, seedToSearch, seedIndex =>
                {
                    Vector2Int seed = seeds[seedIndex];
                    int xOffset = 0;
                    for (; true; xOffset++)
                    {
                        float noise = -heightMapNoise.Evaluate(xOffset + seed.x, seed.y);
                        if (noise < wallNoiseLevel)
                        {
                            seed.x += xOffset;
                            break;
                        }
                    }

                    int minX = seed.x;
                    int maxX = seed.x;

                    int minY = seed.y;
                    int maxY = seed.y;

                    Queue<Vector2Int> queue = new Queue<Vector2Int>();
                    queue.Enqueue(seed);

                    HashSet<Vector2Int> positionUsed = new HashSet<Vector2Int>();
                    positionUsed.Add(seed);

                    while (queue.Count > 0)
                    {
                        var position = queue.Dequeue();

                        if (positionUsed.Count > maxArea)
                        {
                            break;
                        }

                        float noise = -heightMapNoise.Evaluate(position);
                        if (noise >= wallNoiseLevel)
                        {
                            continue;
                        }

                        minX = Mathf.Min(minX, position.x);
                        maxX = Mathf.Max(maxX, position.x);

                        minY = Mathf.Min(minY, position.y);
                        maxY = Mathf.Max(maxY, position.y);

                        var p1 = position + Vector2Int.up;
                        var p2 = position + Vector2Int.down;
                        var p3 = position + Vector2Int.left;
                        var p4 = position + Vector2Int.right;

                        if (!positionUsed.Contains(p1))
                        {
                            queue.Enqueue(p1);
                            positionUsed.Add(p1);
                        }

                        if (!positionUsed.Contains(p2))
                        {
                            queue.Enqueue(p2);
                            positionUsed.Add(p2);
                        }

                        if (!positionUsed.Contains(p3))
                        {
                            queue.Enqueue(p3);
                            positionUsed.Add(p3);
                        }

                        if (!positionUsed.Contains(p4))
                        {
                            queue.Enqueue(p4);
                            positionUsed.Add(p4);
                        }
                    }

                    if (positionUsed.Count < minArea || queue.Count > 0)
                    {
                        scores[seedIndex] = float.MaxValue;
                        return;
                    }

                    seeds[seedIndex] = new Vector2Int(minX, minY) - outerWallBuffer;
                    Vector2Int mapSize = new Vector2Int(maxX - minX, maxY - minY) + 2 * outerWallBuffer;
                    mapSizes[seedIndex] = mapSize;
                    scores[seedIndex] = 1 - (positionUsed.Count / (float)(mapSize.x * mapSize.y));
                });

                var bestSeed = Enumerable.Range(0, seedToSearch)
                    .Select(i => new
                    {
                        Index = i,
                        Score = scores[i]
                    })
                    .Where(x => x.Score != float.MaxValue)
                    .OrderBy(x => x.Score)
                    .Select(x => (
                        seed: seeds[x.Index],
                        size: new Vector3Int(mapSizes[x.Index].x, targetSize.y, mapSizes[x.Index].y)
                    ))
                    .FirstOrDefault();

                if (bestSeed.size == Vector3Int.zero)
                {
                    Log.Debug("No seed found, retrying");
                    continue;
                }

                return bestSeed;
            }
        }

        private (float[,] map, float min, float max) GenerateHeightMap(Vector2Int seed, Vector3Int size)
        {
            float[,] map = new float[size.x, size.z];
            float[] mins = new float[size.x];
            float[] maxs = new float[size.x];

            var center = size / 2;

            Parallel.For(0, size.x, x =>
            {
                float min = float.MaxValue;
                float max = float.MinValue;

                for (int z = 0; z < size.z; z++)
                {
                    float dx = x - center.x;
                    float dz = z - center.z;

                    float ellipsisDistance = Mathf.Sqrt((dx * dx) / (center.x * center.x) + (dz * dz) / (center.z * center.z));
                    
                    float bonusDistanceNoise = bonusNoiseByEllipsisDistance.Evaluate(ellipsisDistance);
                    float ellipsisDerivative = bonusNoiseByEllipsisDistance.Derivative(ellipsisDistance);
                    Vector2 ellipsisDirection = new Vector2(dx, dz);
                    ellipsisDirection.Normalize();

                    (float noise, Vector2 derivative) = heightMapNoise.EvaluateWithDerivative(x + seed.x, z + seed.y);
                    float noise01 = heightCurve.Evaluate((noise + 1) * 0.5f - bonusDistanceNoise);

                    float slopeAngle = Mathf.Atan2(derivative.y + ellipsisDirection.y * ellipsisDerivative, derivative.x + ellipsisDirection.x * ellipsisDerivative);
                    float slopeAngle01 = (slopeAngle + Mathf.PI) / (2 * Mathf.PI);

                    float benchWidth = benchesWidthByHeightCurve.Evaluate(noise01);

                    float benchCrest1 = 0;
                    float benchCrest2 = 1;
                    for (int i = 0; i < benchesHeightCurves.Length; i++)
                    {
                        var benchesHeightCurve = benchesHeightCurves[i].curve;
                        float angleStartHeight = benchesHeightCurves[i].direction == BenchDirection.ClockWise
                            ? slopeAngle01
                            : 1 - slopeAngle01;

                        float benchPos = angleStartHeight;

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
                    else if (noise01 < benchToe1)
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

            int curveSeedX = MapGenerator.rng.RangeInt(0, short.MaxValue);
            int curveSeedY = MapGenerator.rng.RangeInt(0, short.MaxValue);
            int curveSeedZ = MapGenerator.rng.RangeInt(0, short.MaxValue);

            Parallel.For(0, size.x, x =>
            {
                for (int z = 0; z < size.z; z++)
                {
                    for (int y = 0; y < size.y - 1; y++)
                    {
                        float curveNoise = (wallCurvingNoise.Evaluate(x + curveSeedX, y * wallCurvingVecticalScale + curveSeedY, z + curveSeedZ) + 1) / 2;
                        float scaledCurveNoise = wallCurvingMinNoise + (curveNoise * (1 - wallCurvingMinNoise));

                        float floorTickness = 1 + (map2d[x, z] - min) * coefficient * size.y;
                        float noise = Mathf.Clamp01(scaledCurveNoise * ((floorTickness - y) * floorBlendFactor + 0.5f));
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

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
        [Range(0, 1)]
        public float volcanoRoomMinCenterHeight;

        public GameObject volcanoParticleSystemPrefab;
        public float volcanoParticleSystemHeight;
        public float volcanoParticleSystemWidth;

        public override Terrain Generate()
        {
            Vector3Int stageSize = MapGenerator.instance.stageSize;
            (float[,,] map3d, float[,,] floorlessMap) = GenerateIsland(stageSize);
            ProfilerLog.Debug("GenerateVoronoi");

            RemoveHoles(map3d, stageSize);
            ProfilerLog.Debug("GenerateVoronoi");


            var meshResult = MarchingCubes.CreateMesh(map3d, MapGenerator.instance.mapScale);
            ProfilerLog.Debug("marchingCubes");

            GameObject volcanoParticleSystem = Instantiate(volcanoParticleSystemPrefab);

            float height = volcanoParticleSystemHeight * stageSize.y * MapGenerator.instance.mapScale;
            volcanoParticleSystem.transform.position = new Vector3(
                MapGenerator.instance.mapScale * stageSize.x / 2f,
                height,
                MapGenerator.instance.mapScale * stageSize.z / 2f);

            ParticleSystem particleSystem = volcanoParticleSystem.GetComponent<ParticleSystem>();
            var shape = particleSystem.shape;
            shape.length = height;
            shape.angle = Mathf.Rad2Deg * Mathf.Atan2(MapGenerator.instance.mapScale * (stageSize.x + stageSize.z) * volcanoParticleSystemWidth, height);

            return new Terrain
            {
                generator = this,
                meshResult = meshResult,
                floorlessDensityMap = floorlessMap,
                densityMap = map3d,
                maxGroundHeight = float.MaxValue,
                minInteractableHeight = waterLevel,
                customObjects = new List<GameObject>()
                {
                    volcanoParticleSystem
                }
            };
        }

        private (float[,,] map, float[,,] floorlessMap) GenerateIsland(Vector3Int size)
        {
            using (ProfilerLog.CreateScope("GenerateIsland"))
            {
                float[,,] map = new float[size.x, size.y, size.z];
                bool[,] wallMap = new bool[size.x, size.z];

                int heightMapSeedX = MapGenerator.rng.RangeInt(0, short.MaxValue);
                int heightMapSeedZ = MapGenerator.rng.RangeInt(0, short.MaxValue);

                int peekSeedX = MapGenerator.rng.RangeInt(0, short.MaxValue);
                int peekSeedZ = MapGenerator.rng.RangeInt(0, short.MaxValue);

                int volcanoSeedX = MapGenerator.rng.RangeInt(0, short.MaxValue);
                int volcanoSeedZ = MapGenerator.rng.RangeInt(0, short.MaxValue);

                int volcanoRoomSeedX = 0;
                int volcanoRoomSeedZ = 0;

                Vector2 center = new Vector3(size.x / 2f, size.z / 2f);

                for (int i = 0; i < 1000; i++)
                {
                    volcanoRoomSeedX = MapGenerator.rng.RangeInt(0, short.MaxValue);
                    volcanoRoomSeedZ = MapGenerator.rng.RangeInt(0, short.MaxValue);

                    float roomNoise = 0.5f * (volcanoRoomHeightMap.Evaluate(center.x + volcanoRoomSeedX, center.y + volcanoRoomSeedZ) + 1);
                    float noise = volcanoRoomHeightCurve.Evaluate(roomNoise);

                    if (noise > volcanoRoomMinCenterHeight)
                    {
                        Log.Debug("Center found after " + i);
                        break;
                    }
                }

                ProfilerLog.Debug("Center");

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

                ProfilerLog.Debug("heightmap");

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

                ProfilerLog.Debug("floorlessMap");

                return (map, floorlessMap);
            }
        }

        private void RemoveHoles(float[,,] map, Vector3Int size)
        {
            Vector2Int[] neighbors = new Vector2Int[]
            {
                Vector2Int.up,
                Vector2Int.down,
                Vector2Int.left,
                Vector2Int.right
            };

            bool[,] visitedCells = new bool[size.x, size.z];
            List<List<Vector2Int>> holes = new List<List<Vector2Int>>();
            Queue<Vector2Int> queue = new Queue<Vector2Int>();

            for (int x = 0; x < size.x; x++)
            {
                for (int z = 0; z < size.z; z++)
                {
                    if (!visitedCells[x, z] && map[x, 0, z] <= 0.5f)
                    {
                        List<Vector2Int> currentHole = new List<Vector2Int>();

                        visitedCells[x, z] = true;
                        queue.Enqueue(new Vector2Int(x, z));

                        while (queue.Count > 0)
                        {
                            Vector2Int currentPos = queue.Dequeue();
                            currentHole.Add(currentPos);

                            for (int i = 0; i < neighbors.Length; i++)
                            {
                                int posX = currentPos.x + neighbors[i].x;
                                int posZ = currentPos.y + neighbors[i].y;

                                if (posX < 0
                                    || posZ < 0
                                    || posX >= size.x
                                    || posZ >= size.z)
                                {
                                    continue;
                                }

                                if (!visitedCells[posX, posZ] && map[posX, 0, posZ] <= 0.5f)
                                {
                                    visitedCells[posX, posZ] = true;
                                    queue.Enqueue(new Vector2Int(posX, posZ));
                                }
                            }

                        }

                        holes.Add(currentHole);
                    }
                }
            }

            Log.Debug("holes.Count " + holes.Count);

            var holesToRemove = holes
                .OrderByDescending(x => x.Count)
                .Skip(1)
                .SelectMany(x => x)
                .ToList();

            for (int i = 0; i < holesToRemove.Count; i++)
            {
                var pos = holesToRemove[i];
                map[pos.x, 0, pos.y] = 0.501f;
            }
        }
    }
}

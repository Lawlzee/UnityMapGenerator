using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityMeshSimplifier;
using static Rewired.ComponentControls.Effects.RotateAroundAxis;

namespace ProceduralStages
{
    [CreateAssetMenu(fileName = "TowersGenerator", menuName = "ProceduralStages/TowersGenerator", order = 2)]
    public class TowersGenerator : TerrainGenerator
    {
        public VoronoiWallGenerator voronoiWallGenerator;

        public int outerWallDepth = 4;

        public float towerCellsSize;

        public int minTowerCount;
        public int maxTowerCount;
        public float minTowerWidth;
        public float maxTowerWidth;
        public float minTowerSegmentsHeight;
        public float maxTowerSegmentsHeight;
        public CubicHoneycomb towersCubicHoneycomb;


        public SquareHoneycomb floorSquareHoneycomb;
        public FBM floorFBM;
        public float floorMinHeight;
        public float floorMaxHeight;
        public float floorBlendFactor;
        public ThreadSafeCurve floorCullingCurve;

        private class Tower
        {
            public Vector2 position;
            public List<TowerSegment> segments;
        }

        public class TowerSegment
        {
            public float bottomPositionY;
            public float topPositionY;
            public float height;
            public float width;
        }

        public override Terrain Generate()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            var rng = MapGenerator.rng;

            var stageSize = MapGenerator.instance.stageSize;

            //int towerCount = rng.RangeInt(minTowerCount, maxTowerCount);

            Vector2Int gridSize = new Vector2Int(
                Mathf.CeilToInt(stageSize.x / towerCellsSize),
                Mathf.CeilToInt(stageSize.z / towerCellsSize));

            Tower[] towers = new Tower[gridSize.x * gridSize.y];

            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    Vector2 towerPositions = new Vector2(
                        x * towerCellsSize + rng.RangeFloat(0, towerCellsSize),
                        y * towerCellsSize + rng.RangeFloat(0, towerCellsSize));

                    List<TowerSegment> towerSegments = new List<TowerSegment>();

                    float towerHeight = 0f;

                    while (towerHeight < stageSize.y)
                    {
                        float segmentHeight = rng.RangeFloat(minTowerSegmentsHeight, maxTowerSegmentsHeight);

                        towerSegments.Add(new TowerSegment
                        {
                            bottomPositionY = towerHeight,
                            topPositionY = towerHeight + segmentHeight,
                            height = segmentHeight,
                            width = rng.RangeFloat(minTowerWidth, maxTowerWidth)
                        });

                        towerHeight += segmentHeight;
                    }

                    towers[x * gridSize.y + y] = new Tower
                    {
                        position = towerPositions,
                        segments = towerSegments
                    };
                }
            }

            //for (int i = 0; i < towerCount; i++)
            //{
            //    Vector2 towerPositions = new Vector2(
            //        rng.RangeFloat(0, stageSize.x),
            //        rng.RangeFloat(0, stageSize.z));
            //
            //    List<TowerSegment> towerSegments = new List<TowerSegment>();
            //
            //    float towerHeight = 0f;
            //
            //    while (towerHeight < stageSize.y)
            //    {
            //        float segmentHeight = rng.RangeFloat(minTowerSegmentsHeight, maxTowerSegmentsHeight);
            //
            //        towerSegments.Add(new TowerSegment
            //        {
            //            bottomPositionY = towerHeight,
            //            topPositionY = towerHeight + segmentHeight,
            //            height = segmentHeight,
            //            width = rng.RangeFloat(minTowerWidth, maxTowerWidth)
            //        });
            //
            //        towerHeight += segmentHeight;
            //    }
            //
            //    towers[i] = new Tower
            //    {
            //        position = towerPositions,
            //        segments = towerSegments
            //    };
            //}

            LogStats("towers");

            bool[,,] towerBitMap = new bool[stageSize.x, stageSize.y, stageSize.z];

            //Parallel.For(0, stageSize.x, x =>
            //{
            //    for (int z = 0; z < stageSize.z; z++)
            //    {
            //        float minDistance = float.MaxValue;
            //        Tower closestTower = null;
            //
            //        for (int i = 0; i < towerCount; i++)
            //        {
            //            Tower tower = towers[i];
            //            float distance = Mathf.Max(
            //                Mathf.Abs(tower.position.x - x),
            //                Mathf.Abs(tower.position.y - z));
            //
            //            if (distance < minDistance)
            //            {
            //                minDistance = distance;
            //                closestTower = tower;
            //            }
            //        }
            //
            //        TowerSegment towerSegment = closestTower.segments[0];
            //        int segmentIndex = 0;
            //
            //        for (int y = 0; y < stageSize.y; y++)
            //        {
            //            if (towerSegment.topPositionY < y)
            //            {
            //                segmentIndex++;
            //                towerSegment = closestTower.segments[segmentIndex];
            //            }
            //
            //            if (minDistance < towerSegment.width)
            //            {
            //                towerBitMap[x, y, z] = true;
            //            }
            //        }
            //    }
            //});

            Parallel.ForEach(towers, tower =>
            {
                for (int i = 0; i < tower.segments.Count; i++)
                {
                    var segment = tower.segments[i];
                    int minX = Math.Max(0, Mathf.FloorToInt(tower.position.x - 0.5f * segment.width));
                    int minY = Math.Max(0, Mathf.FloorToInt(segment.bottomPositionY));
                    int minZ = Math.Max(0, Mathf.FloorToInt(tower.position.y - 0.5f * segment.width));

                    int maxX = Math.Min(stageSize.x - 1, Mathf.CeilToInt(tower.position.x + 0.5f * segment.width));
                    int maxY = Math.Min(stageSize.y - 1, Mathf.CeilToInt(segment.topPositionY));
                    int maxZ = Math.Min(stageSize.z - 1, Mathf.CeilToInt(tower.position.y + 0.5f * segment.width));

                    for (int x = minX; x <= maxX; x++)
                    {
                        for (int y = minY; y <= maxY; y++)
                        {
                            for (int z = minZ; z <= maxZ; z++)
                            {
                                towerBitMap[x, y, z] = true;
                            }
                        }
                    }
                }
            });

            LogStats("towerBitMap");

            Parallel.For(0, outerWallDepth, i =>
            {
                for (int x = 0; x < stageSize.x; x++)
                {
                    for (int y = 0; y < stageSize.y; y++)
                    {
                        towerBitMap[x, y, i] = true;
                        towerBitMap[x, y, stageSize.z - i - 1] = true;
                    }

                    for (int z = 0; z < stageSize.z; z++)
                    {
                        towerBitMap[x, stageSize.y - i - 1, z] = true;
                    }
                }

                for (int y = 0; y < stageSize.y; y++)
                {
                    for (int z = 0; z < stageSize.z; z++)
                    {
                        towerBitMap[i, y, z] = true;
                        towerBitMap[stageSize.x - i - 1, y, z] = true;
                    }
                }
            });

            LogStats("outerWall");

            float[,,] floorlessMap = new float[stageSize.x, stageSize.y, stageSize.z];

            Parallel.For(0, stageSize.x, x =>
            {
                for (int y = 0; y < stageSize.y; y++)
                {
                    for (int z = 0; z < stageSize.z; z++)
                    {
                        //Voronoi3DResult voronoiResult = voronoi[x, y, z];
                        Voronoi3DResult voronoiResult = towersCubicHoneycomb[x, y, z];

                        bool isWall1 = towerBitMap[
                            Mathf.Clamp(x + Mathf.RoundToInt(voronoiResult.displacement1.x), 0, stageSize.x - 1),
                            Mathf.Clamp(y + Mathf.RoundToInt(voronoiResult.displacement1.y), 0, stageSize.y - 1),
                            Mathf.Clamp(z + Mathf.RoundToInt(voronoiResult.displacement1.z), 0, stageSize.z - 1)];

                        bool isWall2 = towerBitMap[
                            Mathf.Clamp(x + Mathf.RoundToInt(voronoiResult.displacement2.x), 0, stageSize.x - 1),
                            Mathf.Clamp(y + Mathf.RoundToInt(voronoiResult.displacement2.y), 0, stageSize.y - 1),
                            Mathf.Clamp(z + Mathf.RoundToInt(voronoiResult.displacement2.z), 0, stageSize.z - 1)];

                        if (isWall1 && isWall2)
                        {
                            floorlessMap[x, y, z] = 1;
                        }
                        else if (isWall1)
                        {
                            floorlessMap[x, y, z] = Mathf.Clamp(1 - voronoiResult.weight, 0.5f, 1);
                        }
                        else if (isWall2)
                        {
                            floorlessMap[x, y, z] = Mathf.Clamp(voronoiResult.weight, 0, 0.5f);
                        }
                    }
                }
            });

            LogStats("densityMap");

            float[,,] densityMap = new float[stageSize.x, stageSize.y, stageSize.z];

            int floorSeedX = MapGenerator.rng.RangeInt(0, short.MaxValue);
            int floorSeedZ = MapGenerator.rng.RangeInt(0, short.MaxValue);

            Parallel.For(0, stageSize.x, x =>
            {
                for (int z = 0; z < stageSize.z; z++)
                {
                    Voronoi2DResult voronoiResult = floorSquareHoneycomb[x, z];

                    Vector2 position = new Vector2(x, z) + voronoiResult.displacement1;
                    float floorNoise = floorCullingCurve.Evaluate(0.5f * (floorFBM.Evaluate(position) + 1));

                    float floorHeight = floorMinHeight + floorNoise * (floorMaxHeight - floorMinHeight);

                    int y = 0;
                    for (; y < stageSize.y; y++)
                    {
                        float noise = Mathf.Clamp01((floorHeight - y) * floorBlendFactor + 0.5f);
                        if (noise == 0f)
                        {
                            break;
                        }
                        densityMap[x, y, z] = Mathf.Max(noise, floorlessMap[x, y, z]);
                    }


                    for (; y < stageSize.y; y++)
                    {
                        densityMap[x, y, z] = floorlessMap[x, y, z];
                    }
                }
            });

            LogStats("floor");

            var meshResult = MarchingCubes.CreateMesh(densityMap, MapGenerator.instance.mapScale);
            LogStats("marchingCubes");

            return new Terrain
            {
                meshResult = meshResult,
                floorlessDensityMap = floorlessMap,
                densityMap = densityMap,
                maxGroundHeight = float.MaxValue
            };

            void LogStats(string name)
            {
                Log.Debug($"{name}: {stopwatch.Elapsed}");
                stopwatch.Restart();
            }
        }
    }
}

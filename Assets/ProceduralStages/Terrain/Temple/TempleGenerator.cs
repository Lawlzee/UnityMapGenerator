using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ProceduralStages
{
    [CreateAssetMenu(fileName = "TempleGenerator", menuName = "ProceduralStages/TempleGenerator", order = 2)]
    public class TempleGenerator : TerrainGenerator
    {
        public Interval towerWidth;
        public Interval towerSegmentsHeight;
        public CubicHoneycomb towersCubicHoneycomb;

        public FBM floorFBM;
        public ThreadSafeCurve floorCurve;
        public ThreadSafeCurve floorHeightByDistanceCurve;
        public float floorMaxHeight;
        public float floorBlendFactor;

        public FBM ringYOffsetFBM;
        public ThreadSafeCurve ringYOffsetCurve;

        public FBM ringHeightFBM;
        public ThreadSafeCurve ringHeightCurve;

        public FBM crystalFBM;
        public ThreadSafeCurve crystalCurve;
        public ThreadSafeCurve crystalRadiusByHeightCurve;
        public float crystalMaxRadius;
        public Voronoi3D crystalVoronoi;
        public CubicHoneycomb crystalCubicHoneycomb;
        public GameObject crystalParticleSystemPrefab;
        public float crystalParticleSystemRadius;
        public string crystalParticleMaterialKey;

        public StoneWall[] stoneWalls;

        private class Tower
        {
            public Vector2 position;
            public List<TowerSegment> segments;
        }

        public class TowerSegment
        {
            public Interval positionY;
            public float height;
            public float width;
        }

        [Serializable]
        public struct StoneWall
        {
            public Interval distance;
            public int towerCount;
            public Interval towerHeight;
            public Ring[] rings;
            public float ringPathWidth;

            [NonSerialized]
            public Interval[] pathAngles;
        }

        [Serializable]
        public struct Ring
        {
            public IntervalInt heigth;
            public bool addPath;

            [NonSerialized]
            public int seedX;
            public int seedZ;
        }

        public override Terrain Generate()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            var rng = MapGenerator.rng;

            var stageSize = MapGenerator.instance.stageSize;
            Vector3 center3 = new Vector3(stageSize.x / 2f, stageSize.y / 2f, stageSize.z / 2f);
            Vector2 center2 = new Vector2(stageSize.x / 2f, stageSize.z / 2f);
            float circleRadius = Mathf.Min(center2.x, center2.y);

            List<Tower> towers = new List<Tower>();

            for (int i = 0; i < stoneWalls.Length; i++)
            {
                ref StoneWall wall = ref stoneWalls[i];
                wall.pathAngles = new Interval[wall.towerCount];

                float angle = rng.RangeFloat(0, 2 * Mathf.PI);

                float wallDistance = 0.5f * (wall.distance.min + wall.distance.max) * circleRadius;

                for (int j = 0; j < wall.towerCount; j++)
                {
                    Vector2 towerPosition = center2 + wallDistance * new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    float towerMaxHeight = rng.RangeFloat(wall.towerHeight.min, wall.towerHeight.max);

                    List<TowerSegment> towerSegments = new List<TowerSegment>();

                    float towerHeight = 0f;

                    while (towerHeight < towerMaxHeight)
                    {
                        float segmentHeight = rng.RangeFloat(towerSegmentsHeight.min, towerSegmentsHeight.max);

                        towerSegments.Add(new TowerSegment
                        {
                            positionY = new Interval
                            {
                                min = towerHeight,
                                max = towerHeight + segmentHeight
                            },
                            height = segmentHeight,
                            width = rng.RangeFloat(towerWidth.min, towerWidth.max)
                        });

                        towerHeight += segmentHeight;
                    }

                    towers.Add(new Tower
                    {
                        position = towerPosition,
                        segments = towerSegments
                    });

                    angle += 2 * Mathf.PI / wall.towerCount;
                    float pathAngle = angle + Mathf.PI / wall.towerCount;
                    wall.pathAngles[j] = new Interval
                    {
                        min = ((pathAngle - wall.ringPathWidth) + (2 * Mathf.PI)) % (2 * Mathf.PI),
                        max = (pathAngle + wall.ringPathWidth) % (2 * Mathf.PI)
                    };
                }

                for (int j = 0; j < wall.rings.Length; j++)
                {
                    ref Ring ring = ref wall.rings[j];
                    ring.seedX = rng.RangeInt(0, short.MaxValue);
                    ring.seedZ = rng.RangeInt(0, short.MaxValue);
                }
            }

            LogStats("towers");

            bool[,,] towerBitMap = new bool[stageSize.x, stageSize.y, stageSize.z];

            Parallel.ForEach(towers, tower =>
            {
                for (int i = 0; i < tower.segments.Count; i++)
                {
                    var segment = tower.segments[i];
                    int minX = Math.Max(0, Mathf.FloorToInt(tower.position.x - 0.5f * segment.width));
                    int minY = Math.Max(0, Mathf.FloorToInt(segment.positionY.min));
                    int minZ = Math.Max(0, Mathf.FloorToInt(tower.position.y - 0.5f * segment.width));

                    int maxX = Math.Min(stageSize.x - 1, Mathf.CeilToInt(tower.position.x + 0.5f * segment.width));
                    int maxY = Math.Min(stageSize.y - 1, Mathf.CeilToInt(segment.positionY.max));
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

            Parallel.ForEach(stoneWalls, wall =>
            {
                for (int x = 0; x < stageSize.x; x++)
                {
                    for (int z = 0; z < stageSize.z; z++)
                    {
                        Vector2 position = new Vector2(x, z);
                        Vector2 delta = position - center2;
                        float distance = delta.magnitude / circleRadius;

                        if (wall.distance.min < distance && distance < wall.distance.max)
                        {
                            float angle = (Mathf.Atan2(delta.y, delta.x) + 2 * Mathf.PI) % (2 * Mathf.PI);
                            bool isPath = false;

                            for (int i = 0; i < wall.pathAngles.Length; i++)
                            {
                                var pathAngles = wall.pathAngles[i];
                                if (pathAngles.min < pathAngles.max)
                                {
                                    if (pathAngles.min < angle && angle < pathAngles.max)
                                    {
                                        isPath = true;
                                        break;
                                    }
                                }
                                else if (pathAngles.min < angle || angle < pathAngles.max)
                                {
                                    isPath = true;
                                    break;
                                }
                            }

                            for (int i = 0; i < wall.rings.Length; i++)
                            {
                                Ring wallRing = wall.rings[i];

                                if (wallRing.addPath && isPath)
                                {
                                    continue;
                                }

                                float positionOffsetNoise = ringYOffsetFBM.Evaluate(x + wallRing.seedX, z + wallRing.seedZ);
                                float positionOffset = ringYOffsetCurve.Evaluate(positionOffsetNoise);

                                float heightNoise = 0.5f * (ringHeightFBM.Evaluate(x + wallRing.seedX, z + wallRing.seedZ) + 1);
                                float heightOffset = ringHeightCurve.Evaluate(heightNoise);

                                float min = wallRing.heigth.min + positionOffset - heightOffset;
                                float max = wallRing.heigth.max + positionOffset + heightOffset;

                                int minY = Mathf.Clamp(Mathf.FloorToInt(min), 0, stageSize.y);
                                int maxY = Mathf.Clamp(Mathf.CeilToInt(max), 0, stageSize.y);

                                for (int y = minY; y < maxY; y++)
                                {
                                    towerBitMap[x, y, z] = true;
                                }
                            }
                        }
                    }
                }
            });

            LogStats("towerBitMap");

            float[,,] floorlessMap = new float[stageSize.x, stageSize.y, stageSize.z];
            int stoneSeedX = rng.RangeInt(0, short.MaxValue);
            int stoneSeedY = rng.RangeInt(0, short.MaxValue);
            int stoneSeedZ = rng.RangeInt(0, short.MaxValue);

            Parallel.For(0, stageSize.x, x =>
            {
                for (int y = 0; y < stageSize.y; y++)
                {
                    for (int z = 0; z < stageSize.z; z++)
                    {
                        //Voronoi3DResult voronoiResult = voronoi[x, y, z];
                        Voronoi3DResult voronoiResult = towersCubicHoneycomb[x + stoneSeedX, y + stoneSeedY, z + stoneSeedZ];

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

            Vector3Int crystalSeed = new Vector3Int(
                rng.RangeInt(0, short.MaxValue),
                rng.RangeInt(0, short.MaxValue),
                rng.RangeInt(0, short.MaxValue));
            
            Parallel.For(0, stageSize.x, x =>
            {
                for (int y = 0; y < stageSize.y; y++)
                {
                    for (int z = 0; z < stageSize.z; z++)
                    {
                        Vector3 position = new Vector3(x, y, z);
            
                        Voronoi3DResult voronoiResult = crystalCubicHoneycomb[x, y, z];
            
                        Vector3 pos1 = position + voronoiResult.displacement1;
                        Vector3 delta1 = pos1 - center3;
                        float radius1 = crystalMaxRadius * crystalRadiusByHeightCurve.Evaluate(pos1.y / stageSize.y);
            
                        float ellipsisDistance1 = Mathf.Sqrt(
                            (delta1.x * delta1.x) / (radius1 * radius1)
                            + (delta1.z * delta1.z) / (radius1 * radius1));
            
                        float noise1Bonus = crystalCurve.Evaluate(0.5f * (crystalFBM.Evaluate(pos1 + crystalSeed) + 1));
                        float noise1 = 1 - ellipsisDistance1 + noise1Bonus;
                        bool isWall1 = noise1 > 0.5f;
            
                        Vector3 pos2 = position + voronoiResult.displacement1;
                        Vector3 delta2 = pos2 - center3;
                        float radius2 = crystalMaxRadius * crystalRadiusByHeightCurve.Evaluate(pos2.y / stageSize.y);
            
                        float ellipsisDistance2 = Mathf.Sqrt(
                            (delta2.x * delta2.x) / (radius2 * radius2)
                            + (delta2.z * delta2.z) / (radius2 * radius2));
            
                        float noise2Bonus = crystalCurve.Evaluate(0.5f * (crystalFBM.Evaluate(pos2 + crystalSeed) + 1));
                        float noise2 = 1 - ellipsisDistance2 + noise2Bonus;
                        bool isWall2 = noise2 > 0.5f;
            
                        if (isWall1 && isWall2)
                        {
                            floorlessMap[x, y, z] = 1;
                        }
                        else if (isWall1)
                        {
                            floorlessMap[x, y, z] = Mathf.Max(floorlessMap[x, y, z], 1 - voronoiResult.weight);
                        }
                        else if (isWall2)
                        {
                            floorlessMap[x, y, z] = Mathf.Max(floorlessMap[x, y, z], voronoiResult.weight);
                        }
                    }
                }
            });


            float[,,] densityMap = new float[stageSize.x, stageSize.y, stageSize.z];

            int floorSeedX = rng.RangeInt(0, short.MaxValue);
            int floorSeedZ = rng.RangeInt(0, short.MaxValue);

            Parallel.For(0, stageSize.x, x =>
            {
                for (int z = 0; z < stageSize.z; z++)
                {
                    Vector2 position = new Vector2(x, z);
                    float distance = (position - center2).magnitude / circleRadius;

                    float baseFloorHeight = floorMaxHeight * floorHeightByDistanceCurve.Evaluate(distance);
                    float floorHeight = baseFloorHeight + floorCurve.Evaluate(floorFBM.Evaluate(x + floorSeedX, z + floorSeedZ));

                    int y = 0;
                    for (; y < stageSize.y; y++)
                    {
                        float noise = (floorHeight - y) * floorBlendFactor + 0.5f;
                        if (noise <= 0f)
                        {
                            break;
                        }
                        densityMap[x, y, z] = Mathf.Clamp01(Mathf.Max(noise, floorlessMap[x, y, z]));
                    }


                    for (; y < stageSize.y; y++)
                    {
                        densityMap[x, y, z] = Mathf.Clamp01(floorlessMap[x, y, z]);
                    }
                }
            });

            LogStats("densityMap");

            GameObject crystalParticleSystem = Instantiate(crystalParticleSystemPrefab);
            crystalParticleSystem.transform.position = MapGenerator.instance.mapScale * new Vector3(center3.x, 0, center3.z);
            ParticleSystem particleSystem = crystalParticleSystem.GetComponent<ParticleSystem>();
            ParticleSystem.ShapeModule particleSystemShape = particleSystem.shape;
            particleSystemShape.radius = MapGenerator.instance.mapScale * circleRadius * crystalParticleSystemRadius;

            if (!string.IsNullOrEmpty(crystalParticleMaterialKey))
            {
                var crytalParticleMaterial = Addressables.LoadAssetAsync<Material>(crystalParticleMaterialKey).WaitForCompletion();
                ParticleSystemRenderer crytalParticleRenderer = particleSystem.GetComponent<ParticleSystemRenderer>();
                crytalParticleRenderer.material = crytalParticleMaterial;
            }
            
            var meshResult = MarchingCubes.CreateMesh(densityMap, MapGenerator.instance.mapScale);
            LogStats("marchingCubes");

            return new Terrain
            {
                meshResult = meshResult,
                floorlessDensityMap = floorlessMap,
                densityMap = densityMap,
                maxGroundHeight = float.MaxValue,
                customObjects = new GameObject[]
                {
                    crystalParticleSystem
                }
            };

            void LogStats(string name)
            {
                Log.Debug($"{name}: {stopwatch.Elapsed}");
                stopwatch.Restart();
            }
        }
    }
}

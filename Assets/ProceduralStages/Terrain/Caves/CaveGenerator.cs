using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityMeshSimplifier;
using static Rewired.ComponentControls.Effects.RotateAroundAxis;

namespace ProceduralStages
{
    [CreateAssetMenu(fileName = "caveGenerator", menuName = "ProceduralStages/CaveGenerator", order = 2)]
    public class CaveGenerator : TerrainGenerator
    {
        public VoronoiWallGenerator voronoiWallGenerator;
        public Map2dGenerator wallGenerator = new Map2dGenerator();
        public Carver carver = new Carver();
        public Waller waller = new Waller();

        public Interval floorThickness;
        public FBM floorFBM;
        public ThreadSafeCurve floorCurve;

        public float stalagmitesMaxHeight;
        public FBM stalagmitesFBM;
        public ThreadSafeCurve stalagmitesCurve;

        public CellularAutomata3d cave3d = new CellularAutomata3d();
        public Map3dNoiser map3dNoiser = new Map3dNoiser();
        public StalactitesGenerator stalactitesGenerator;

        public override Terrain Generate()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            //float[,,] map3d = voronoiWallGenerator.Create(MapGenerator.instance.stageSize);
            //LogStats("voronoiWallGenerator");

            Vector3Int stageSize = MapGenerator.instance.stageSize;
            float[,,] floorlessMap = wallGenerator.Create(stageSize);
            LogStats("wallGenerator");
            
            carver.CarveWalls(floorlessMap);
            LogStats("carver");

            waller.AddWalls(floorlessMap);
            LogStats("waller.AddWalls");

            stalactitesGenerator.AddStalactites(floorlessMap);
            LogStats("stalactitesGenerator.AddStalactites");

            int floorSeedX = MapGenerator.rng.RangeInt(0, short.MaxValue);
            int floorSeedZ = MapGenerator.rng.RangeInt(0, short.MaxValue);
            
            float[,,] densityMap = new float[stageSize.x, stageSize.y, stageSize.z];
            Parallel.For(0, stageSize.x, x =>
            {
                for (int z = 0; z < stageSize.z; z++)
                {
                    float floorNoise = floorCurve.Evaluate(0.5f * (floorFBM.Evaluate(x + floorSeedX, z + floorSeedZ) + 1));
                    float floorHeight = floorThickness.min + floorNoise * (floorThickness.max - floorThickness.min);

                    int y = 0;
                    for (; y < stageSize.y; y++)
                    {
                        float noise = Mathf.Clamp01((floorHeight - y) * waller.blendFactor + 0.5f);
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
            LogStats("waller.AddFloor");

            densityMap = map3dNoiser.AddNoise(densityMap);
            LogStats("map3dNoiser");

            densityMap = cave3d.SmoothMap(densityMap);
            LogStats("SmoothMap");

            int stalagmitesSeedX = MapGenerator.rng.RangeInt(0, short.MaxValue);
            int stalagmitesSeedZ = MapGenerator.rng.RangeInt(0, short.MaxValue);

            Parallel.For(0, stageSize.x, x =>
            {
                for (int z = 0; z < stageSize.z; z++)
                {
                    float stalagmitesNoise = stalagmitesCurve.Evaluate(0.5f * (stalagmitesFBM.Evaluate(x + stalagmitesSeedX, z + stalagmitesSeedZ) + 1));
                    float stalagmitesHeight = stalagmitesNoise * stalagmitesMaxHeight;

                    int y = 0;
                    for (; y < stageSize.y; y++)
                    {
                        float noise = Mathf.Clamp01((stalagmitesHeight - y) * waller.blendFactor + 0.5f);
                        if (noise == 0f)
                        {
                            break;
                        }
                        densityMap[x, y, z] = Mathf.Max(noise, densityMap[x, y, z]);
                    }

                    for (; y < stageSize.y; y++)
                    {
                        densityMap[x, y, z] = densityMap[x, y, z];
                    }
                }
            });

            var meshResult = MarchingCubes.CreateMesh(densityMap, MapGenerator.instance.mapScale);
            LogStats("marchingCubes");

            //MeshSimplifier simplifier = new MeshSimplifier(unOptimisedMesh);
            //simplifier.SimplifyMesh(MapGenerator.instance.meshQuality);
            //var optimisedMesh = simplifier.ToMesh();
            //LogStats("MeshSimplifier");

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

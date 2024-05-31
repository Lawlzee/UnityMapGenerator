using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityMeshSimplifier;

namespace ProceduralStages
{
    [CreateAssetMenu(fileName = "overworldGenerator", menuName = "ProceduralStages/OverworldGenerator", order = 2)]
    public class OverworldGenerator : TerrainGenerator
    {
        public Map2dGenerator wallGenerator = new Map2dGenerator();
        public Carver carver = new Carver();
        public Waller waller = new Waller();
        public FloorWallsMixer floorWallsMixer = new FloorWallsMixer();
        public CellularAutomata3d cave3d = new CellularAutomata3d();
        public Map3dNoiser map3dNoiser = new Map3dNoiser();

        public override Terrain Generate()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            var stageSize = MapGenerator.instance.stageSize;
            float[,,] wallOnlyMap = wallGenerator.Create(stageSize);
            LogStats("wallGenerator");

            carver.CarveWalls(wallOnlyMap);
            LogStats("carver");

            //waller.AddCeilling(map3d);
            //LogStats("waller.AddCeilling");

            waller.AddWalls(wallOnlyMap);
            LogStats("waller.AddWalls");

            float[,,] floorOnlyMap = new float[stageSize.x, stageSize.y, stageSize.z];
            floorOnlyMap = waller.AddFloor(floorOnlyMap);
            LogStats("waller.AddFloor");

            float[,,] densityMap = floorWallsMixer.Mix(floorOnlyMap, wallOnlyMap);
            LogStats("floorWallsMixer");

            densityMap = map3dNoiser.AddNoise(densityMap);
            LogStats("map3dNoiser");

            float[,,] smoothMap3d = cave3d.SmoothMap(densityMap);
            LogStats("cave3d");

            var meshResult = MarchingCubes.CreateMesh(smoothMap3d, MapGenerator.instance.mapScale);
            LogStats("marchingCubes");

            //MeshSimplifier simplifier = new MeshSimplifier(unOptimisedMesh);
            //simplifier.SimplifyMesh(MapGenerator.instance.meshQuality);
            //var optimisedMesh = simplifier.ToMesh();
            //LogStats("MeshSimplifier");

            return new Terrain
            {
                meshResult = meshResult,
                floorlessDensityMap = wallOnlyMap,
                densityMap = smoothMap3d,
                maxGroundheight = waller.floor.maxThickness * MapGenerator.instance.mapScale
            };

            void LogStats(string name)
            {
                Log.Debug($"{name}: {stopwatch.Elapsed}");
                stopwatch.Restart();
            }
        }
    }
}

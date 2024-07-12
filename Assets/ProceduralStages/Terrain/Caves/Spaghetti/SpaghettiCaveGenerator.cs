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
    [CreateAssetMenu(fileName = "spaghettiCaveGenerator", menuName = "ProceduralStages/SpaghettiCaveGenerator", order = 2)]
    public class SpaghettiCaveGenerator : TerrainGenerator
    {
        public Map2dGenerator wallGenerator = new Map2dGenerator();
        public SpaghettiCaver spaghettiCaver = new SpaghettiCaver();
        public SinCaver sin = new SinCaver();
        public Carver carver = new Carver();
        public Waller waller = new Waller();
        public CellularAutomata3d cave3d = new CellularAutomata3d();
        public Map3dNoiser map3dNoiser = new Map3dNoiser();

        public override Terrain Generate()
        {
            var maps = spaghettiCaver.Create(MapGenerator.instance.stageSize);
            ProfilerLog.Debug("spaghettiCaver");
            //float[,,] map3d = sin.Create(MapGenerator.instance.stageSize);

            //float[,,] map3d = wallGenerator.Create(MapGenerator.instance.stageSize);
            //LogStats("wallGenerator");
            //
            //carver.CarveWalls(map3d);
            //LogStats("carver");
            //
            //waller.AddCeilling(map3d);
            //LogStats("waller.AddCeilling");
            //
            //waller.AddWalls(map3d);
            //LogStats("waller.AddWalls");
            //
            //var floorlessMap = map3d;
            //map3d = waller.AddFloor(map3d);
            //LogStats("waller.AddFloor");

            //float[,,] noiseMap3d = map3dNoiser.AddNoise(map3d);
            //LogStats("map3dNoiser");
            //
            float[,,] smoothMap3d = maps.map;//cave3d.SmoothMap(maps.map);
            ProfilerLog.Debug("cave3d");

            var meshResult = MarchingCubes.CreateMesh(smoothMap3d, MapGenerator.instance.mapScale);
            ProfilerLog.Debug("marchingCubes");

            //MeshSimplifier simplifier = new MeshSimplifier(unOptimisedMesh);
            //simplifier.SimplifyMesh(MapGenerator.instance.meshQuality);
            //var optimisedMesh = simplifier.ToMesh();
            //LogStats("MeshSimplifier");

            return new Terrain
            {
                generator = this,
                meshResult = meshResult,
                floorlessDensityMap = maps.floorlessMap,
                densityMap = smoothMap3d,
                maxGroundHeight = float.MaxValue
            };
        }
    }
}

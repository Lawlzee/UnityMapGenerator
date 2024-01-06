using Assets.Scripts;
using ProceduralStages;
using RoR2;
using RoR2.Navigation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

namespace Generator.Assets.Scripts
{
    //[DefaultExecutionOrder(-1)]
    public class MapGenerator : MonoBehaviour
    {
        public int width;
        public int height;
        public int depth;

        public int seed;

        [Range(0, 10)]
        public float mapScale = 1f;

        public CellularAutomata2d cave2d = new CellularAutomata2d();

        public Map2dToMap3d map2dToMap3d = new Map2dToMap3d();
        public Carver carver = new Carver();
        public Waller waller = new Waller();

        public CellularAutomata3d cave3d = new CellularAutomata3d();

        public Map3dNoiser map3dNoiser = new Map3dNoiser();

        public MeshColorer meshColorer = new MeshColorer();

        public ColorPatelette colorPatelette = new ColorPatelette();

        public NodeGraphCreator nodeGraphCreator = new NodeGraphCreator();

        //private float[,] _map;
        //private bool[,,] _map;

        private void Awake()
        {
            UnityEngine.Debug.Log("Awake");
            GenerateMap();
        }

        private void Start()
        {
            UnityEngine.Debug.Log("Start");
            //GenerateMap();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F2))
            {
                GenerateMap();
            }
        }

        private void OnValidate()
        {
            if (Application.IsPlaying(this))
            {
                GenerateMap();
            }
        }

        private void GenerateMap()
        {
            UnityEngine.Debug.Log("GenerateMap");
            int currentSeed = seed == 0
                ? Time.time.GetHashCode()
                : seed;
            System.Random rng = new System.Random(currentSeed);

            Stopwatch totalStopwatch = Stopwatch.StartNew();

            Stopwatch stopwatch = Stopwatch.StartNew();

            var map2D = cave2d.Create(width, depth, rng);
            UnityEngine.Debug.Log($"cave2d: " + stopwatch.Elapsed.ToString());
            stopwatch.Restart();

            bool[,,] map3d = map2dToMap3d.Convert(map2D, height);
            UnityEngine.Debug.Log($"map2dToMap3d: " + stopwatch.Elapsed.ToString());
            stopwatch.Restart();

            carver.CarveWalls(map3d, rng);
            UnityEngine.Debug.Log($"carver: " + stopwatch.Elapsed.ToString());
            stopwatch.Restart();

            waller.AddFloorAndCeilling(map3d, rng);
            UnityEngine.Debug.Log($"waller.AddFloorAndCeilling: " + stopwatch.Elapsed.ToString());
            stopwatch.Restart();

            waller.AddWalls(map3d, rng);
            UnityEngine.Debug.Log($"waller.AddWalls: " + stopwatch.Elapsed.ToString());
            stopwatch.Restart();

            bool[,,] smoothMap3d = cave3d.SmoothMap(map3d);
            UnityEngine.Debug.Log($"cave3d: " + stopwatch.Elapsed.ToString());
            stopwatch.Restart();

            float[,,] noiseMap3d = map3dNoiser.ToNoiseMap(smoothMap3d, rng);
            UnityEngine.Debug.Log($"map3dNoiser: " + stopwatch.Elapsed.ToString());
            stopwatch.Restart();

            var meshResult = MarchingCubes.CreateMesh(noiseMap3d, mapScale, meshColorer, rng);
            UnityEngine.Debug.Log($"mesh: " + stopwatch.Elapsed.ToString());
            stopwatch.Restart();

            GetComponent<MeshFilter>().mesh = meshResult.mesh;
            UnityEngine.Debug.Log($"MeshFilter: " + stopwatch.Elapsed.ToString());
            stopwatch.Restart();

            Texture2D texture = colorPatelette.Create(rng);
            GetComponent<MeshRenderer>().material.mainTexture = texture;
            UnityEngine.Debug.Log($"MeshRenderer: " + stopwatch.Elapsed.ToString());
            stopwatch.Restart();

            var surface = ScriptableObject.CreateInstance<SurfaceDef>();
            surface.approximateColor = colorPatelette.AverageColor(texture);
            surface.materialSwitchString = "stone";

            GetComponent<SurfaceDefProvider>().surfaceDef = surface;
            UnityEngine.Debug.Log($"surfaceDef: " + stopwatch.Elapsed.ToString());
            stopwatch.Restart();

            //RoR2/Base/Common/VFX/StoneImpact.prefab
            GetComponent<MeshCollider>().sharedMesh = meshResult.mesh;
            UnityEngine.Debug.Log($"MeshCollider: " + stopwatch.Elapsed.ToString());
            stopwatch.Restart();

            (NodeGraph groundNodes, HashSet<int> mainIsland) = nodeGraphCreator.CreateGroundNodes(meshResult);
            UnityEngine.Debug.Log($"groundNodes: " + stopwatch.Elapsed.ToString());
            stopwatch.Restart();

            for (int i = 0; i < 50; i++)
            {
                var newtIndex = mainIsland.ElementAt(rng.Next(mainIsland.Count));
                var node = groundNodes.nodes[newtIndex];
                if ((node.flags & NodeFlags.NoChestSpawn) == 0)
                {
                    NewtPlacer newtPlacer = GetComponent<NewtPlacer>();
                    newtPlacer.position = node.position;
                    newtPlacer.rotation = Quaternion.Euler(0, rng.Next(-180, 180), 0);
                    break;
                }
            }


            NodeGraph airNodes = nodeGraphCreator.CreateAirNodes(groundNodes, mainIsland, map3d, mapScale);
            UnityEngine.Debug.Log($"airNodes: " + stopwatch.Elapsed.ToString());
            stopwatch.Restart();

            SceneInfo sceneInfo = GetComponent<SceneInfo>();
            sceneInfo.groundNodes = groundNodes;
            sceneInfo.airNodes = airNodes;



            UnityEngine.Debug.Log($"total: " + totalStopwatch.Elapsed.ToString());


            //_map = smoothMap3d;
        }






    }
}
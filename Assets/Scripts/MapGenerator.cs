using Assets.Scripts;
using RoR2;
using RoR2.Navigation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace Generator.Assets.Scripts
{
    //[DefaultExecutionOrder(-1)]
    public class MapGenerator : MonoBehaviour
    {
        public int width;
        public int height;
        public int depth;

        public string seed;

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

        public event Action<MeshResult> onGenerated;

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
            int currentSeed = string.IsNullOrEmpty(seed)
                ? Time.time.GetHashCode()
                : seed.GetHashCode();
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

            GetComponent<MeshRenderer>().material.mainTexture = colorPatelette.Create(rng);
            UnityEngine.Debug.Log($"MeshRenderer: " + stopwatch.Elapsed.ToString());
            stopwatch.Restart();

            GetComponent<MeshCollider>().sharedMesh = meshResult.mesh;
            UnityEngine.Debug.Log($"MeshCollider: " + stopwatch.Elapsed.ToString());
            stopwatch.Restart();

            (NodeGraph groundNodes, HashSet<int> mainIsland) = nodeGraphCreator.CreateGroundNodes(meshResult);
            UnityEngine.Debug.Log($"groundNodes: " + stopwatch.Elapsed.ToString());
            stopwatch.Restart();

            NodeGraph airNodes = nodeGraphCreator.CreateAirNodes(groundNodes, mainIsland, map3d, mapScale);
            UnityEngine.Debug.Log($"airNodes: " + stopwatch.Elapsed.ToString());
            stopwatch.Restart();

            SceneInfo sceneInfo = GetComponent<SceneInfo>();
            sceneInfo.groundNodes = groundNodes;
            sceneInfo.airNodes = airNodes;



            UnityEngine.Debug.Log($"total: " + totalStopwatch.Elapsed.ToString());



            if (onGenerated != null)
            {
                onGenerated(meshResult);
            }

            //_map = smoothMap3d;
        }

        

        

        
    }
}
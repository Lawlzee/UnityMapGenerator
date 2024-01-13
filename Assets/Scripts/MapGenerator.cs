using Assets.Scripts;
using RoR2;
using RoR2.Navigation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityMeshSimplifier;

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

        [Range(0, 1)]
        public float meshQuality = 0.1f;

        public CellularAutomata2d cave2d = new CellularAutomata2d();

        public Map2dToMap3d map2dToMap3d = new Map2dToMap3d();
        public Carver carver = new Carver();
        public Waller waller = new Waller();

        public CellularAutomata3d cave3d = new CellularAutomata3d();

        public Map3dNoiser map3dNoiser = new Map3dNoiser();

        public MeshColorer meshColorer = new MeshColorer();

        public ColorPatelette colorPatelette = new ColorPatelette();

        public NodeGraphCreator nodeGraphCreator = new NodeGraphCreator();

        public event Action<float> onSunHueSelected;
        //private float[,] _map;
        //private bool[,,] _map;

        private void Awake()
        {
            //UnityEngine.Debug.Log("Awake");
            GenerateMap();
        }

        private void Start()
        {
            //UnityEngine.Debug.Log("Start");
            //GenerateMap();
        }

        private void Update()
        {
            //if (Input.GetKeyDown(KeyCode.F2))
            //{
            //    GenerateMap();
            //}
        }

        private void OnValidate()
        {
#if UNITY_2019_4
            if (Application.IsPlaying(this))
            {
                GenerateMap();
                GetComponent<SceneInfo>().OnValidate();
            }
#endif

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
            LogStats("cave2d");

            bool[,,] map3d = map2dToMap3d.Convert(map2D, height);
            LogStats("map2dToMap3d");

            carver.CarveWalls(map3d, rng);
            LogStats("carver");

            waller.AddFloorAndCeilling(map3d, rng);
            LogStats("waller.AddFloorAndCeilling");

            waller.AddWalls(map3d, rng);
            LogStats("waller.AddWalls");

            bool[,,] smoothMap3d = cave3d.SmoothMap(map3d);
            LogStats("cave3d");

            float[,,] noiseMap3d = map3dNoiser.ToNoiseMap(smoothMap3d, rng);
            LogStats("map3dNoiser");

            var unOptimisedMesh = MarchingCubes.CreateMesh(noiseMap3d, mapScale, meshColorer, rng);
            LogStats("marchingCubes");

            MeshSimplifier simplifier = new MeshSimplifier(unOptimisedMesh);
            simplifier.SimplifyMesh(meshQuality);
            var optimisedMesh = simplifier.ToMesh();
            LogStats("MeshSimplifier");

            var meshResult = new MeshResult
            {
                mesh = optimisedMesh,
                normals = optimisedMesh.normals,
                triangles = optimisedMesh.triangles,
                vertices = optimisedMesh.vertices
            };

            meshColorer.ColorMesh(meshResult, rng);
            LogStats("meshColorer");

            GetComponent<MeshFilter>().mesh = meshResult.mesh;
            LogStats("MeshFilter");

            Texture2D heightMap = colorPatelette.CreateHeightMap(rng);
            Texture2D texture = colorPatelette.CreateTexture(rng, heightMap);
            LogStats("colorPatelette");

            var material = GetComponent<MeshRenderer>().material;
            material.mainTexture = texture;
            material.SetTexture("_ParallaxMap", heightMap);
            //material.SetTexture("_DetailAlbedoMap", texture);
            material.color = new Color(1.3f, 1.3f, 1.3f);

            RenderSettings.ambientIntensity = 1.4f;
            RenderSettings.sun.intensity = 0.75f;
            float sunHue = (float)rng.NextDouble();
            RenderSettings.sun.color = Color.HSVToRGB(sunHue, colorPatelette.light.saturation, colorPatelette.light.value);

            if (onSunHueSelected != null)
            {
                onSunHueSelected(sunHue);
            }
            LogStats("MeshRenderer");

            var surface = ScriptableObject.CreateInstance<SurfaceDef>();
            surface.approximateColor = colorPatelette.AverageColor(texture);
            surface.materialSwitchString = "stone";

            GetComponent<SurfaceDefProvider>().surfaceDef = surface;
            LogStats("surfaceDef");

            GetComponent<MeshCollider>().sharedMesh = meshResult.mesh;
            LogStats("MeshCollider");

            (NodeGraph groundNodes, NodeGraph airNodes) = nodeGraphCreator.CreateGraphs(meshResult, map3d, mapScale);
            LogStats("nodeGraphs");

            SceneInfo sceneInfo = GetComponent<SceneInfo>();
            sceneInfo.groundNodes = groundNodes;
            sceneInfo.airNodes = airNodes;

            UnityEngine.Debug.Log($"total: " + totalStopwatch.Elapsed.ToString());

            void LogStats(string name)
            {
                UnityEngine.Debug.Log($"{name}: {stopwatch.Elapsed}");
                stopwatch.Restart();
            }
        }
    }
}
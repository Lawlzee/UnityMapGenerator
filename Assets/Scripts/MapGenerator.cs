using Assets.Scripts;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Generator.Assets.Scripts
{
    public class MapGenerator : MonoBehaviour
    {
        public int width;
        public int height;
        public int depth;

        public string seed;

        [Range(0, 100)]
        public float mapScale = 1f;

        public CellularAutomata2d cave2d = new CellularAutomata2d();

        public Map2dToMap3d map2dToMap3d = new Map2dToMap3d();
        public Carver carver = new Carver();
        public Waller waller = new Waller();

        public CellularAutomata3d cave3d = new CellularAutomata3d();

        public Map3dNoiser map3dNoiser = new Map3dNoiser();

        public MeshColorer meshColorer = new MeshColorer();

        public ColorPatelette colorPatelette = new ColorPatelette();

        //private float[,] _map;
        //private bool[,,] _map;

        private void Awake()
        {
            Debug.Log("Awake");
        }

        private void Start()
        {
            Debug.Log("Start");
            GenerateMap();
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
            Debug.Log("GenerateMap");
            int currentSeed = string.IsNullOrEmpty(seed)
                ? Time.time.GetHashCode()
                : seed.GetHashCode();
            System.Random rng = new System.Random(currentSeed);

            var map2D = cave2d.Create(width, depth, rng);

            bool[,,] map3d = map2dToMap3d.Convert(map2D, height);
            carver.CarveWalls(map3d, rng);
            waller.AddFloorAndCeilling(map3d, rng);
            waller.AddWalls(map3d, rng);

            bool[,,] smoothMap3d = cave3d.SmoothMap(map3d);
            float[,,] noiseMap3d = map3dNoiser.ToNoiseMap(smoothMap3d, rng);



            var mesh = MarchingCubes.CreateMesh(noiseMap3d, meshColorer, rng);

            GetComponent<MeshFilter>().mesh = mesh;

            GetComponent<MeshRenderer>().material.mainTexture = colorPatelette.Create(rng);

            GetComponent<MeshCollider>().sharedMesh = mesh;

            //_map = smoothMap3d;
        }

        

        

        
    }
}
using RoR2;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using UnityMeshSimplifier;

namespace ProceduralStages
{
    [CreateAssetMenu(fileName = "moonGenerator", menuName = "ProceduralStages/MoonGenerator", order = 2)]
    public class MoonGenerator : TerrainGenerator
    {
        public Map2dGenerator wallGenerator = new Map2dGenerator();
        public Carver carver = new Carver();
        public Waller waller = new Waller();
        public FloorWallsMixer floorWallsMixer = new FloorWallsMixer();
        public CellularAutomata3d cave3d = new CellularAutomata3d();
        public Map3dNoiser map3dNoiser = new Map3dNoiser();

        public override Terrain Generate()
        {
            var moonObject = SceneManager.GetActiveScene().GetRootGameObjects().Single(x => x.name == "Moon");
            moonObject.SetActive(true);

            MoonArena.AddArena(new Vector3(-77, -205, -1));
            
            if (NetworkServer.active)
            {
                var dropship = MoonDropship.Place(new Vector3(100, 0, 0));

                moonObject.transform.Find("MoonEscapeSequence").GetComponent<MoonEscapeSequence>().dropshipZone = dropship;
            }
            
            //MoonEscapeSequence.Place(dropship);
            //MapGenerator.instance.StartCoroutine(TransferMithrixArena());

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

            densityMap = map3dNoiser.AddNoise(densityMap);
            LogStats("map3dNoiser");

            float[,,] smoothMap3d = cave3d.SmoothMap(densityMap);
            LogStats("cave3d");

            var unOptimisedMesh = MarchingCubes.CreateMesh(smoothMap3d, MapGenerator.instance.mapScale);
            LogStats("marchingCubes");

            MeshSimplifier simplifier = new MeshSimplifier(unOptimisedMesh);
            simplifier.SimplifyMesh(MapGenerator.instance.meshQuality);
            var optimisedMesh = simplifier.ToMesh();
            LogStats("MeshSimplifier");

            return new Terrain
            {
                meshResult = new MeshResult
                {
                    mesh = optimisedMesh,
                    normals = optimisedMesh.normals,
                    triangles = optimisedMesh.triangles,
                    vertices = optimisedMesh.vertices
                },
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

        private IEnumerator TransferMithrixArena()
        {
            var proceduralScene = SceneManager.GetActiveScene();

            var scene = Addressables.LoadSceneAsync("RoR2/Base/moon2/moon2.unity", LoadSceneMode.Additive, activateOnLoad: false);
            yield return scene;

            Scene moon = SceneManager.GetSceneByName("moon2");

            if (false && scene.Status == AsyncOperationStatus.Succeeded)
            {
                //AsyncOperation scene = SceneManager.LoadSceneAsync("moon2", LoadSceneMode.Additive);
                //scene.allowSceneActivation = false;

                //Wait until we are done loading the scene
                //while (!scene.IsDone)
                //{
                //    yield return null;
                //}


                //if (moon.IsValid())
                {
                    var rootObjects = moon.GetRootGameObjects();

                    var gameplaySpace = rootObjects.First(x => x.name == "HOLDER: Gameplay Space");
                    SceneManager.MoveGameObjectToScene(gameplaySpace, proceduralScene);

                    //HOLDER: STATIC MESH
                    var staticMesh = gameplaySpace.transform.GetChild(0).gameObject;
                    //Quadrant 5: Blood Arena
                    Destroy(staticMesh.transform.GetChild(5).gameObject);
                    //Quadrant 4: Starting Temple
                    Destroy(staticMesh.transform.GetChild(4).gameObject);
                    //Quadrant 3: Greenhouse
                    Destroy(staticMesh.transform.GetChild(3).gameObject);
                    //Quadrant 2: Workshop
                    Destroy(staticMesh.transform.GetChild(2).gameObject);
                    //Quadrant 1: Quarry
                    Destroy(staticMesh.transform.GetChild(1).gameObject);

                    //HOLDER: OPTIONAL MESH
                    Destroy(gameplaySpace.transform.GetChild(1).gameObject);

                    //var tower = gameplaySpace.transform.Cast<Transform>()
                    //    .Where(x => x.name == "HOLDER: STATIC MESH")
                    //    .SelectMany(x => x.Cast<Transform>())
                    //    .Where(x => x.name == "Tower")
                    //    .Select(x => x.gameObject)
                    //    .First();
                    //
                    //var finalArena = gameplaySpace.transform.Cast<Transform>()
                    //    .Where(x => x.name == "HOLDER: Final Arena")
                    //    .Select(x => x.gameObject)
                    //    .First();


                    //SceneManager.MoveGameObjectToScene(tower, currentScene);
                    //SceneManager.MoveGameObjectToScene(finalArena, currentScene);
                }
            }

            //SceneManager.UnloadSceneAsync(moon);
        }

        
    }
}

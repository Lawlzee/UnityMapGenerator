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
            AddArena(Vector3.zero);
            MapGenerator.instance.StartCoroutine(TransferMithrixArena());

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

        private void AddArena(Vector3 position)
        {
            //var holder = MapGenerator.instance.propsPlacer.propsObject.transform;
            //Vector3 vanillaFinalArenaPosition = new Vector3(-11, 690.96f, -1);

            var arenaMaterial = Addressables.LoadAssetAsync<Material>("RoR2/Base/moon/matMoonRuinsDirtyArena.mat").WaitForCompletion();

            GameObject arenaPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/moon/HG_Moon_Arena.fbx").WaitForCompletion();
            GameObject moonArenaBaseLayerBowlPrefab = arenaPrefab.transform.GetChild(0).gameObject;
            GameObject moonArenaBaseLayerOctagonPrefab = arenaPrefab.transform.GetChild(1).gameObject;
            GameObject arenaOctogon001Prefab = arenaPrefab.transform.GetChild(2).gameObject;
            GameObject moonArenaBaseLayerRoofPrefab = arenaPrefab.transform.GetChild(3).gameObject;
            GameObject arenaRoundPrefab = arenaPrefab.transform.GetChild(4).gameObject;
            GameObject moonArenaColumnPrefab = arenaPrefab.transform.GetChild(6).gameObject;

            GameObject mdlPlatform_Column_LowPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/moon/mdlPlatform_Column_Low.prefab").WaitForCompletion();
            GameObject mdlPlatform_Column_Low_StraightPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/moon/mdlPlatform_Column_Low_Straight Variant.prefab").WaitForCompletion();
            GameObject HG_Tower_terrainPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/moon/HG_Tower_terrain.fbx").WaitForCompletion();
            GameObject mdlroot_structurePrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/moon/mdlroot_structure.prefab").WaitForCompletion();
            GameObject mdl_disc_platformPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/moon/mdl_disc_platform.prefab").WaitForCompletion();
            GameObject moonArenaColumnSmallPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/moon/MoonArenaColumn, Small.prefab").WaitForCompletion();
            GameObject moonArenaColumnHugePrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/moon/MoonArenaColumn, Huge.prefab").WaitForCompletion();
            GameObject moonArenaColumnHugeAltPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/moon/MoonArenaColumn, Huge Alt.prefab").WaitForCompletion();

            GameObject gameplaySpace = new GameObject("HOLDER: Gameplay Space");
            gameplaySpace.transform.position = new Vector3(-77, -205, -1);

            GameObject staticMesh = new GameObject("HOLDER: STATIC MESH");
            staticMesh.transform.parent = gameplaySpace.transform;
            staticMesh.layer = LayerIndex.world.intVal;
            staticMesh.transform.localPosition = new Vector3(0, 0, 0);

            GameObject tower = new GameObject("Tower");
            tower.transform.parent = staticMesh.transform;
            tower.layer = LayerIndex.world.intVal;
            tower.transform.localPosition = new Vector3(0, 0, 0);

            GameObject towerCenterRing = new GameObject("Tower Center Ring");
            towerCenterRing.transform.parent = tower.transform;
            towerCenterRing.layer = LayerIndex.world.intVal;
            towerCenterRing.transform.localPosition = new Vector3(0, 0, 0);

            GameObject mdlPlatform_Column_Low = Instantiate(mdlPlatform_Column_LowPrefab);
            mdlPlatform_Column_Low.transform.parent = towerCenterRing.transform;
            mdlPlatform_Column_Low.transform.localPosition = new Vector3(0, 14.2f, 0);
            mdlPlatform_Column_Low.transform.localEulerAngles = new Vector3(-90, 0, -133.047f);
            mdlPlatform_Column_Low.transform.localScale = new Vector3(0.6658599f, 0.6658599f, 0.6658599f);

            GameObject mdlPlatform_Column_Low2 = Instantiate(mdlPlatform_Column_LowPrefab);
            mdlPlatform_Column_Low2.transform.parent = towerCenterRing.transform;
            mdlPlatform_Column_Low2.transform.localPosition = new Vector3(0, 14.2f, 0);
            mdlPlatform_Column_Low2.transform.localEulerAngles = new Vector3(-90, 0, 46.953f);
            mdlPlatform_Column_Low2.transform.localScale = new Vector3(0.6658599f, 0.6658599f, 0.6658599f);

            GameObject mdlPlatform_Column_Low3 = Instantiate(mdlPlatform_Column_LowPrefab);
            mdlPlatform_Column_Low3.transform.parent = towerCenterRing.transform;
            mdlPlatform_Column_Low3.transform.localPosition = new Vector3(0, 14.2f, 0);
            mdlPlatform_Column_Low3.transform.localEulerAngles = new Vector3(-90, 0, -43.047f);
            mdlPlatform_Column_Low3.transform.localScale = new Vector3(0.6658599f, 0.6658599f, 0.6658599f);

            GameObject mdlPlatform_Column_Low_Straight = Instantiate(mdlPlatform_Column_Low_StraightPrefab);
            mdlPlatform_Column_Low_Straight.transform.parent = towerCenterRing.transform;
            mdlPlatform_Column_Low_Straight.transform.localPosition = new Vector3(0, 14.2f, 0);
            mdlPlatform_Column_Low_Straight.transform.localEulerAngles = new Vector3(-90, 0, 136.953f);
            mdlPlatform_Column_Low_Straight.transform.localScale = new Vector3(0.6658599f, 0.6658599f, 0.6658599f);
            
            GameObject HG_Tower_terrain = Instantiate(HG_Tower_terrainPrefab);
            HG_Tower_terrain.transform.parent = towerCenterRing.transform;
            HG_Tower_terrain.layer = LayerIndex.world.intVal;
            HG_Tower_terrain.transform.localPosition = new Vector3(0, 0, 0);
            HG_Tower_terrain.transform.localScale = new Vector3(100, 100, 100);

            //todo: spmMoonGrass1 x4


            GameObject mdlroot_structure = Instantiate(mdlroot_structurePrefab);
            mdlroot_structure.transform.parent = towerCenterRing.transform;
            mdlroot_structure.transform.localPosition = new Vector3(0, -529.3599f, 0.00006103516f);
            mdlroot_structure.transform.localEulerAngles = new Vector3(-90, 0, 55.6f);

            GameObject mdl_disc_platform = Instantiate(mdl_disc_platformPrefab);
            mdl_disc_platform.transform.parent = towerCenterRing.transform;
            mdl_disc_platform.transform.localPosition = new Vector3(0.00003051758f, 16.8f, 0.00004440546f);
            mdl_disc_platform.transform.localEulerAngles = new Vector3(-90, 0, -90);

            //todo: HG_Rock_001 x N

            GameObject finalArena = new GameObject("HOLDER: Final Arena");
            finalArena.transform.parent = gameplaySpace.transform;
            finalArena.layer = LayerIndex.world.intVal;
            finalArena.transform.localPosition = new Vector3(-11, 690.96f, 1);

            GameObject columnHolderSetInner = new GameObject("ColumnHolderSet, Inner");
            columnHolderSetInner.transform.parent = finalArena.transform;
            columnHolderSetInner.layer = LayerIndex.world.intVal;
            columnHolderSetInner.transform.localPosition = new Vector3(0, 0, 0);
            columnHolderSetInner.transform.localEulerAngles = new Vector3(0, 45, 0);
            columnHolderSetInner.transform.localScale = new Vector3(12, 12, 12);

            AddMoonArenaColumnSmall(new Vector3(0, 0, 0));
            AddMoonArenaColumnSmall(new Vector3(0, 90, 0));
            AddMoonArenaColumnSmall(new Vector3(0, -90, 0));
            AddMoonArenaColumnSmall(new Vector3(0, -180, 0));

            void AddMoonArenaColumnSmall(Vector3 angle)
            {
                GameObject moonArenaColumnSmall = Instantiate(moonArenaColumnSmallPrefab);
                moonArenaColumnSmall.transform.parent = columnHolderSetInner.transform;
                moonArenaColumnSmall.transform.localPosition = new Vector3(0, 0, 0);
                moonArenaColumnSmall.transform.localEulerAngles = angle;// new Vector3(0, 0, 0);
                moonArenaColumnSmall.transform.localScale = Vector3.one;

                var child = moonArenaColumnSmall.transform.GetChild(0);
                child.transform.localPosition = new Vector3(7.2f, -1.079999f, 0);
                child.transform.localEulerAngles = new Vector3(-89.98f, 0, 0);
                child.transform.localScale = Vector3.one;
            }

            GameObject columnHolderSetOuter = new GameObject("ColumnHolderSet, Outer");
            columnHolderSetOuter.transform.parent = finalArena.transform;
            columnHolderSetOuter.layer = LayerIndex.world.intVal;
            columnHolderSetOuter.transform.localPosition = new Vector3(0, 2.76001F, 0);
            columnHolderSetOuter.transform.localScale = new Vector3(12, 12, 12);

            GameObject moonArenaColumnHuge = Instantiate(moonArenaColumnHugeAltPrefab);
            moonArenaColumnHuge.transform.parent = columnHolderSetOuter.transform;
            moonArenaColumnHuge.transform.localPosition = new Vector3(0, 0, 0);
            moonArenaColumnHuge.transform.localEulerAngles = new Vector3(0, -45, 0);
            moonArenaColumnHuge.transform.localScale = Vector3.one;

            GameObject moonArenaColumnHuge2 = Instantiate(moonArenaColumnHugeAltPrefab);
            moonArenaColumnHuge2.transform.parent = columnHolderSetOuter.transform;
            moonArenaColumnHuge2.transform.localPosition = new Vector3(0, 0, 0);
            moonArenaColumnHuge2.transform.localEulerAngles = new Vector3(0, 45, 0);
            moonArenaColumnHuge2.transform.localScale = Vector3.one;

            GameObject moonArenaColumnHuge3 = Instantiate(moonArenaColumnHugeAltPrefab);
            moonArenaColumnHuge3.transform.parent = columnHolderSetOuter.transform;
            moonArenaColumnHuge3.transform.localPosition = new Vector3(0, 0, 0);
            moonArenaColumnHuge3.transform.localEulerAngles = new Vector3(0, -135, 0);
            moonArenaColumnHuge3.transform.localScale = Vector3.one;

            GameObject moonArenaColumnHuge4 = Instantiate(moonArenaColumnHugeAltPrefab);
            moonArenaColumnHuge4.transform.parent = columnHolderSetOuter.transform;
            moonArenaColumnHuge4.transform.localPosition = new Vector3(0, 0, 0);
            moonArenaColumnHuge4.transform.localEulerAngles = new Vector3(0, 135, 0);
            moonArenaColumnHuge4.transform.localScale = Vector3.one;
            
            GameObject columnHolderSetBase = new GameObject("ColumnHolderSet, Base");
            columnHolderSetBase.transform.parent = finalArena.transform;
            columnHolderSetBase.layer = LayerIndex.world.intVal;
            columnHolderSetBase.transform.localPosition = new Vector3(0, -353.3f, 0);
            columnHolderSetBase.transform.localEulerAngles = new Vector3(0, -22.5f, 0);
            columnHolderSetBase.transform.localScale = new Vector3(12, 12, 12);

            AddArenaColumn(new Vector3(0, 90, 0));
            AddArenaColumn(new Vector3(0, 180, 0));
            AddArenaColumn(new Vector3(0, -90, 0));
            AddArenaColumn(new Vector3(0, 0, 0));
            AddArenaColumn(new Vector3(0, -135, 0));
            AddArenaColumn(new Vector3(0, 135, 0));
            AddArenaColumn(new Vector3(0, -45, 0));
            AddArenaColumn(new Vector3(0, 45, 0));

            void AddArenaColumn(Vector3 rotation)
            {
                GameObject columnHolder = new GameObject("ColumnHolder");
                columnHolder.transform.parent = columnHolderSetBase.transform;
                columnHolder.layer = LayerIndex.world.intVal;
                columnHolder.transform.localPosition = new Vector3(0, 0, 0);
                columnHolder.transform.localEulerAngles = rotation;
                columnHolder.transform.localScale = Vector3.one;

                GameObject moonArenaColumn = Instantiate(moonArenaColumnPrefab);
                moonArenaColumn.transform.parent = columnHolder.transform;
                moonArenaColumn.layer = LayerIndex.world.intVal;
                moonArenaColumn.transform.localPosition = new Vector3(21.8f, 37.8f, 0);
                moonArenaColumn.transform.localEulerAngles = new Vector3(37.4f, 90f, -90);
                moonArenaColumn.transform.localScale = new Vector3(3f, 1.84f, 3f);
                moonArenaColumn.GetComponent<MeshRenderer>().material = arenaMaterial;
            }

            //todo: water
            //todo: Silver Cluster

            GameObject arenaBase = new GameObject("Arena Base");
            arenaBase.transform.parent = finalArena.transform;
            arenaBase.layer = LayerIndex.world.intVal;
            arenaBase.transform.localPosition = new Vector3(0, 0, 0);
            arenaBase.transform.localEulerAngles = new Vector3(0, 0, 0);
            arenaBase.transform.localScale = new Vector3(12, 12, 12);


            GameObject moonArenaBaseLayerBowl = Instantiate(moonArenaBaseLayerBowlPrefab);
            moonArenaBaseLayerBowl.transform.parent = arenaBase.transform;
            moonArenaBaseLayerBowl.layer = LayerIndex.world.intVal;
            moonArenaBaseLayerBowl.transform.localPosition = new Vector3(0, 0.07f, 0);
            moonArenaBaseLayerBowl.transform.localEulerAngles = new Vector3(-90, 0, 45);
            moonArenaBaseLayerBowl.transform.localScale = new Vector3(0.9999999f, 0.9999999f, 0.377764f);
            moonArenaBaseLayerBowl.GetComponent<MeshRenderer>().material = arenaMaterial;

            GameObject moonArenaBaseLayerBowl2 = Instantiate(moonArenaBaseLayerBowlPrefab);
            moonArenaBaseLayerBowl2.transform.parent = arenaBase.transform;
            moonArenaBaseLayerBowl2.layer = LayerIndex.world.intVal;
            moonArenaBaseLayerBowl2.transform.localPosition = new Vector3(0, -0.07f, 0);
            moonArenaBaseLayerBowl2.transform.localEulerAngles = new Vector3(-90, 0, 45);
            moonArenaBaseLayerBowl2.transform.localScale = new Vector3(1.9f, 1.9f, 1.9f);
            moonArenaBaseLayerBowl2.GetComponent<MeshRenderer>().material = arenaMaterial;

            GameObject moonArenaBaseLayerOctagon = Instantiate(moonArenaBaseLayerOctagonPrefab);
            moonArenaBaseLayerOctagon.transform.parent = arenaBase.transform;
            moonArenaBaseLayerOctagon.layer = LayerIndex.world.intVal;
            moonArenaBaseLayerOctagon.transform.localPosition = new Vector3(0, -0.07f, 0);
            moonArenaBaseLayerOctagon.transform.localEulerAngles = new Vector3(-90, 0, -135);
            moonArenaBaseLayerOctagon.transform.localScale = new Vector3(0.9999999f, 0.9999999f, 1);
            moonArenaBaseLayerOctagon.GetComponent<MeshRenderer>().material = arenaMaterial;

            GameObject moonArenaBaseLayerOctagon2 = Instantiate(moonArenaBaseLayerOctagonPrefab);
            moonArenaBaseLayerOctagon2.transform.parent = moonArenaBaseLayerOctagon.transform;
            moonArenaBaseLayerOctagon2.layer = LayerIndex.world.intVal;
            moonArenaBaseLayerOctagon2.transform.localPosition = new Vector3(0, 0, 0.1300001f);
            moonArenaBaseLayerOctagon2.transform.localEulerAngles = new Vector3(0, 0, 0);
            moonArenaBaseLayerOctagon2.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
            moonArenaBaseLayerOctagon2.GetComponent<MeshRenderer>().material = arenaMaterial;

            GameObject moonArenaBaseLayerOctagon3 = Instantiate(moonArenaBaseLayerOctagonPrefab);
            moonArenaBaseLayerOctagon3.transform.parent = moonArenaBaseLayerOctagon2.transform;
            moonArenaBaseLayerOctagon3.layer = LayerIndex.world.intVal;
            moonArenaBaseLayerOctagon3.transform.localPosition = new Vector3(0, 0, 0.2555565f);
            moonArenaBaseLayerOctagon3.transform.localEulerAngles = new Vector3(0, 0, 0);
            moonArenaBaseLayerOctagon3.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            moonArenaBaseLayerOctagon3.GetComponent<MeshRenderer>().material = arenaMaterial;

            //todo: HOLDER: Rubble, Outer Outer
            //todo: HOLDER: Robble, Outer Inner
            //todo: spmMoonGrass1 x7


            GameObject moonArenaBaseLayerRoof = Instantiate(moonArenaBaseLayerRoofPrefab);
            moonArenaBaseLayerRoof.transform.parent = arenaBase.transform;
            moonArenaBaseLayerRoof.layer = LayerIndex.world.intVal;
            moonArenaBaseLayerRoof.transform.localPosition = new Vector3(0, 9.0814f, 0);
            moonArenaBaseLayerRoof.transform.localEulerAngles = new Vector3(90, 0, -45);
            moonArenaBaseLayerRoof.transform.localScale = new Vector3(1.907233f, 1.907233f, 1.907233f);
            moonArenaBaseLayerRoof.GetComponent<MeshRenderer>().material = arenaMaterial;

            GameObject octagonPlates = new GameObject("OctagonPlates");
            octagonPlates.transform.parent = finalArena.transform;
            octagonPlates.layer = LayerIndex.world.intVal;
            octagonPlates.transform.localPosition = new Vector3(0, 0, 0);

            AddOctagon(new Vector3(0, 22.5f, 0));
            AddOctagon(new Vector3(0, -67.5f, 0));
            AddOctagon(new Vector3(0, -157.5f, 0));
            AddOctagon(new Vector3(0, 112.5f, 0));
            AddOctagon(new Vector3(0, 67.5f, 0));
            AddOctagon(new Vector3(0, 0, 180));
            AddOctagon(new Vector3(0, -112.5f, 0));
            AddOctagon(new Vector3(0, 157.5f, 0));

            //todo: Boulders

            void AddOctagon(Vector3 rotation)
            {
                GameObject octagonHolder = new GameObject("OctagonHolder");
                octagonHolder.transform.parent = octagonPlates.transform;
                octagonHolder.layer = LayerIndex.world.intVal;
                octagonHolder.transform.localPosition = new Vector3(0, 0, 0);
                octagonHolder.transform.localEulerAngles = rotation;
                octagonHolder.transform.localScale = new Vector3(2.808f, 2.808f, 2.808f);

                GameObject moonArenaColumn = Instantiate(moonArenaBaseLayerOctagonPrefab);
                moonArenaColumn.transform.parent = octagonHolder.transform;
                moonArenaColumn.layer = LayerIndex.world.intVal;
                moonArenaColumn.transform.localPosition = new Vector3(86.66f, 3.47f, 0.07f);
                moonArenaColumn.transform.localEulerAngles = new Vector3(-90f, 0f, 0f);
                moonArenaColumn.transform.localScale = Vector3.one;
                moonArenaColumn.GetComponent<MeshRenderer>().material = arenaMaterial;
            }

            //GameObject moonArenaColumnHuge = Instantiate(moonArenaColumnHugePrefab);

            //MoonArenaColumn, Huge Alt (4)





            //RoR2/Base/moon/HG_Moon_Arena.fbx
            //RoR2/Base/moon/mdlroot_structure_VFX.fbx
            //RoR2/Base/moon/mdlroot_structure.fbx
            //RoR2/Base/moon/mdlroot_structure.prefab
            //RoR2/Base/moon/HG_Tower_terrain.fbx
            //RoR2/Base/moon/mdl_disc_platform.fbx
            //RoR2/Base/moon/mdl_disc_platform.prefab
            //RoR2/Base/moon/mdlPlatform_Column_Low.fbx
            //RoR2/Base/moon/mdlPlatform_Column_Low_Collision.fbx
            //RoR2/Base/moon/MoonArenaColumn, Huge.prefab
            //RoR2/Base/moon/MoonArenaColumn, Small.prefab



            //PlaceArena(arenaRoofPrefab, new Vector3(-0.000001379f, 9.0814f, 0.00000018723f), new Vector3(90, 0, -45), new Vector3(1.907233f, 1.907233f, 1.907233f));
            //
            //PlaceArena(arenaBowlPrefab, new Vector3(0, -0.07f, 0), new Vector3(-90, 0, 45), new Vector3(1.9f, 1.9f, 1.9f));
            //PlaceArena(arenaBowlPrefab, new Vector3(0, 0.07f, 0), new Vector3(-90, 0, 45), new Vector3(0.9999999f, 0.9999999f, 0.377764f));
            ////PlaceArena(arenaOctogon001Prefab, new Vector3(), new Vector3(), new Vector3());
            ////PlaceArena(arenaRoofPrefab, new Vector3(), new Vector3(), new Vector3());
            ////PlaceArena(arenaRoundPrefab, new Vector3(), new Vector3(), new Vector3());
            ////PlaceArena(arenaColumnPrefab, new Vector3(), new Vector3(), new Vector3());
            //
            //void PlaceArena(GameObject prefab, Vector3 moonPosition, Vector3 rotation, Vector3 scale)
            //{
            //    Place(prefab, moonPosition - vanillaFinalArenaPosition, rotation, scale);
            //}
            //
            //void Place(GameObject prefab, Vector3 moonPosition, Vector3 rotation, Vector3 scale)
            //{
            //    GameObject gameObject = Instantiate(prefab, holder);
            //    gameObject.transform.position = position + moonPosition - vanillaGameplaySpacePosition;
            //    gameObject.transform.eulerAngles = rotation;
            //    gameObject.transform.localScale = scale;
            //
            //    MapGenerator.instance.propsPlacer.instances.Add(gameObject);
            //}

            MapGenerator.instance.propsPlacer.instances.Add(gameplaySpace);
        }
    }
}

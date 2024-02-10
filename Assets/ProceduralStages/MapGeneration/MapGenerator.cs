using RoR2;
using RoR2.ExpansionManagement;
using RoR2.Navigation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.Rendering.PostProcessing;
using UnityMeshSimplifier;

namespace ProceduralStages
{
    //[DefaultExecutionOrder(-1)]
    public class MapGenerator : MonoBehaviour
    {
        public int width;
        public int height;
        public int depth;

        [Range(0, 10)]
        public float mapScale = 1f;

        [Range(0, 1)]
        public float meshQuality = 0.1f;

        public bool loadResourcesInEditor = false;
        public bool loadPropsInEditor = false;
        public ulong editorSeed;

        public Map2dGenerator wallGenerator = new Map2dGenerator();

        public Carver carver = new Carver();
        public Waller waller = new Waller();

        public CellularAutomata3d cave3d = new CellularAutomata3d();

        public Map3dNoiser map3dNoiser = new Map3dNoiser();

        public MeshColorer meshColorer = new MeshColorer();

        public ColorPatelette colorPatelette = new ColorPatelette();
        public MapTextures textures = new MapTextures();

        public NodeGraphCreator nodeGraphCreator = new NodeGraphCreator();

        public DccsPoolGenerator dccsPoolGenerator = new DccsPoolGenerator();

        public PropsPlacer propsPlacer = new PropsPlacer();

        public GameObject sceneInfoObject;
        public GameObject postProcessingObject;
        public GameObject directorObject;

        public int editorFloorIndex;
        public int editorWallIndex;

        public static Xoroshiro128Plus rng;

        private void Awake()
        {
            if (Application.IsPlaying(this))
            {
                GenerateMap();
            }
        }

        private void Start()
        {
            //UnityEngine.Debug.Log("Start");
            //GenerateMap();
        }

        private void OnDestroy()
        {
            rng = null;

        }

        private void Update()
        {
            if (Application.isEditor && Input.GetKeyDown(KeyCode.F2))
            {
                for (int i = 0; i < propsPlacer.instances.Count; i++)
                {
                    Destroy(propsPlacer.instances[i]);
                }
                propsPlacer.instances.Clear();
                GenerateMap();
            }
        }

        private void OnValidate()
        {
            if (Application.IsPlaying(this) && editorFloorIndex >= 0 && editorWallIndex >= 0)
            {
                MapTextures.SurfaceTexture wall = textures.walls[editorWallIndex];
                MapTextures.SurfaceTexture floor = textures.floor[editorFloorIndex];

                SetTextures(floor, wall);
            }
        }

        private void GenerateMap()
        {
            int stageInLoop = ((Run.instance?.stageClearCount ?? 0) % Run.stagesPerLoop) + 1;

            ulong currentSeed;
            Log.Debug("GenerateMap");
            if (Application.isEditor)
            {
                if (editorSeed != 0)
                {
                    currentSeed = editorSeed;
                }
                else if (SeedSyncer.randomStageRng != null)
                {
                    currentSeed = SeedSyncer.randomStageRng.nextUlong;
                }
                else
                {
                    currentSeed = (ulong)DateTime.Now.Ticks;
                }
            }
            else
            {
                currentSeed = SeedSyncer.randomStageRng.nextUlong;
            }
            

            rng = new Xoroshiro128Plus(currentSeed);

            Stopwatch totalStopwatch = Stopwatch.StartNew();

            Stopwatch stopwatch = Stopwatch.StartNew();

            float[,,] map3d = wallGenerator.Create(width, height, depth);
            LogStats("wallGenerator");

            //float[,,] map3d = map2dToMap3d.Convert(map2d, height);
            //LogStats("map2dToMap3d");

            carver.CarveWalls(map3d);
            LogStats("carver");

            waller.AddCeilling(map3d);
            LogStats("waller.AddCeilling");

            waller.AddWalls(map3d);
            LogStats("waller.AddWalls");

            var floorMap = map3d;
            map3d = waller.AddFloor(map3d);
            LogStats("waller.AddFloor");

            float[,,] noiseMap3d = map3dNoiser.AddNoise(map3d);
            //float[,,] noiseMap3d = map3dNoiser.ToNoiseMap(smoothMap3d, rng);
            LogStats("map3dNoiser");

            float[,,] smoothMap3d = cave3d.SmoothMap(noiseMap3d);
            LogStats("cave3d");

            
            var unOptimisedMesh = MarchingCubes.CreateMesh(smoothMap3d, mapScale);
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

            meshColorer.ColorMesh(meshResult);
            LogStats("meshColorer");

            GetComponent<MeshFilter>().mesh = meshResult.mesh;
            LogStats("MeshFilter");

            Texture2D texture = colorPatelette.CreateTexture();
            LogStats("colorPatelette");

            var material = GetComponent<MeshRenderer>().material;
            material.SetTexture("_ColorTex", texture);

            int floorIndex = rng.RangeInt(0, textures.floor.Length);
            int wallIndex = rng.RangeInt(0, textures.walls.Length);
            if (Application.isEditor)
            {
                if (editorFloorIndex >= 0)
                {
                    floorIndex = editorFloorIndex;
                }

                if (editorWallIndex >= 0)
                {
                    wallIndex = editorWallIndex;
                }
            }

            MapTextures.SurfaceTexture wall = textures.walls[wallIndex];
            MapTextures.SurfaceTexture floor = textures.floor[floorIndex];

            Material terrainMaterial = SetTextures(floor, wall);
            LogStats("MeshRenderer");

            float sunHue = rng.nextNormalizedFloat;
            RenderSettings.sun.color = Color.HSVToRGB(sunHue, colorPatelette.light.saturation, colorPatelette.light.value);
            LogStats("RenderSettings");

            RampFog fog = postProcessingObject.GetComponent<PostProcessVolume>().profile.GetSetting<RampFog>();
            var fogColor = Color.HSVToRGB(sunHue, colorPatelette.fog.saturation, colorPatelette.fog.value);

            fog.fogColorStart.value = fogColor;
            fog.fogColorStart.value.a = colorPatelette.fog.colorStartAlpha;
            fog.fogColorMid.value = fogColor;
            fog.fogColorMid.value.a = colorPatelette.fog.colorMidAlpha;
            fog.fogColorEnd.value = fogColor;
            fog.fogColorEnd.value.a = colorPatelette.fog.colorEndAlpha;
            fog.fogZero.value = colorPatelette.fog.zero;
            fog.fogOne.value = colorPatelette.fog.one;

            fog.fogIntensity.value = colorPatelette.fog.intensity;
            fog.fogPower.value = colorPatelette.fog.power;
            LogStats("Fog/Light");

            var surface = ScriptableObject.CreateInstance<SurfaceDef>();
            surface.approximateColor = colorPatelette.AverageColor(texture);
            surface.materialSwitchString = "stone";

            GetComponent<SurfaceDefProvider>().surfaceDef = surface;
            LogStats("surfaceDef");

            GetComponent<MeshCollider>().sharedMesh = meshResult.mesh;
            LogStats("MeshCollider");

            Graphs graphs = nodeGraphCreator.CreateGraphs(meshResult, smoothMap3d, floorMap, mapScale);
            LogStats("nodeGraphs");

            SceneInfo sceneInfo = sceneInfoObject.GetComponent<SceneInfo>();
            sceneInfo.groundNodes = graphs.ground;
            sceneInfo.airNodes = graphs.air;

            ClassicStageInfo stageInfo = sceneInfoObject.GetComponent<ClassicStageInfo>();

            SetDCCS(stageInfo);
            LogStats("SetDCCS");

            var combatDirectors = directorObject.GetComponents<CombatDirector>();
            if (IsSimulacrum() || Application.isEditor)
            {
                foreach (var combatDirector in combatDirectors)
                {
                    combatDirector.monsterCredit = float.MinValue;
                    combatDirector.moneyWaveIntervals = new RangeFloat[0];
                    combatDirector.moneyWaves = new CombatDirector.DirectorMoneyWave[0];
                }
            }

            if (!IsSimulacrum())
            {
                stageInfo.sceneDirectorMonsterCredits = 30 * (stageInLoop + 4);
            }

            if (!IsSimulacrum() || Application.isEditor)
            {
                stageInfo.sceneDirectorInteractibleCredits = 75 * (stageInLoop + 2);
            }

            SceneDirector sceneDirector = directorObject.GetComponent<SceneDirector>();

            bool useLunarPortal = stageInLoop == Run.stagesPerLoop;
            string portalPath = useLunarPortal
                ? "RoR2/Base/Teleporters/iscLunarTeleporter.asset"
                : "RoR2/Base/Teleporters/iscTeleporter.asset";

            if (!Application.isEditor || loadResourcesInEditor)
            {
                if (!IsSimulacrum())
                {
                    sceneDirector.teleporterSpawnCard = Addressables.LoadAssetAsync<InteractableSpawnCard>(portalPath).WaitForCompletion();
                }

                if (NetworkServer.active)
                {
                    Action<SceneDirector> placeInteractables = null;
                    placeInteractables = _ =>
                    {
                        SceneDirector.onPostPopulateSceneServer -= placeInteractables;

                        if (!IsSimulacrum())
                        {
                            InteractablePlacer.Place("RoR2/Base/NewtStatue/NewtStatue.prefab", NodeFlagsExt.Newt, Vector3.up);
                        }

                        if (stageInLoop == 4)
                        {
                            InteractablePlacer.Place("RoR2/Base/GoldChest/GoldChest.prefab", NodeFlagsExt.Newt);
                        }
                    };

                    SceneDirector.onPostPopulateSceneServer += placeInteractables;
                }
            }
            
            if (!Application.isEditor || loadPropsInEditor)
            {
                propsPlacer.PlaceAll(graphs, meshColorer, texture, terrainMaterial);
                LogStats("propsPlacer");
            }

            if (!Application.isEditor)
            {
                var stages = SceneCatalog.allStageSceneDefs
                    .Where(x => x.cachedName != "random")
                    .ToList();

                var mainTracks = stages
                    .Select(x => x.mainTrack)
                    .Where(x => x != null)
                    .ToList();

                var bossTracks = stages
                    .Select(x => x.bossTrack)
                    .Where(x => x != null)
                    .ToList();

                var mainTrack = mainTracks[rng.RangeInt(0, mainTracks.Count)];
                var bossTrack = bossTracks[rng.RangeInt(0, bossTracks.Count)];

                Action<SceneDef> onSceneChanged = null;
                onSceneChanged = scene =>
                {
                    SceneCatalog.onMostRecentSceneDefChanged -= onSceneChanged;
                    if (scene.cachedName == "random")
                    {
                        scene.mainTrack = mainTrack;
                        scene.bossTrack = bossTrack;
                    }
                };

                SceneCatalog.onMostRecentSceneDefChanged += onSceneChanged;
                LogStats("music");
            }

            Log.Debug($"total: " + totalStopwatch.Elapsed.ToString());

            void LogStats(string name)
            {
                Log.Debug($"{name}: {stopwatch.Elapsed}");
                stopwatch.Restart();
            }
        }

        private void SetDCCS(ClassicStageInfo stageInfo)
        {
            if (Application.isEditor && !loadResourcesInEditor)
            {
                return;
            }

            ExpansionDef expansionDef = ExpansionCatalog.expansionDefs.FirstOrDefault(x => x.name == "DLC1");

            bool hasDLC1 = expansionDef && Run.instance.IsExpansionEnabled(expansionDef);

            var validPools = DccsPoolItem.All
                .Where(x => IsSimulacrum()
                    ? x.StageType == StageType.Simulacrum
                    : x.StageType == StageType.Regular)
                .Where(x => hasDLC1 || !x.DLC1)
                .ToList();

            if (IsSimulacrum())
            {
                var dpMonsters = validPools
                    .Where(x => x.Type == DccsPoolItemType.Monsters)
                    .Select(x => x.Asset)
                    .ToList();

                string dpMonster = dpMonsters[rng.RangeInt(0, dpMonsters.Count)];
                Log.Debug(dpMonster);

                stageInfo.monsterDccsPool = Addressables.LoadAssetAsync<DccsPool>(dpMonster).WaitForCompletion();
            }
            else
            {
                Log.Debug("dpCustomProceduralStages");
                stageInfo.monsterDccsPool = dccsPoolGenerator.GenerateMonstersDccs(hasDLC1);
            }

            var dpInteratables = validPools
                .Where(x => x.Type == DccsPoolItemType.Interactables)
                .Select(x => x.Asset)
                .ToList();

            string dpInteratable = dpInteratables[rng.RangeInt(0, dpInteratables.Count)];
            Log.Debug(dpInteratable);

            stageInfo.interactableDccsPool = Addressables.LoadAssetAsync<DccsPool>(dpInteratable).WaitForCompletion();
        }

        private bool IsSimulacrum()
        {
            return !Application.isEditor && Run.instance is InfiniteTowerRun;
        }

        private Material SetTextures(MapTextures.SurfaceTexture floor, MapTextures.SurfaceTexture wall)
        {
            var material = GetComponent<MeshRenderer>().material;

            material.mainTexture = Addressables.LoadAssetAsync<Texture2D>(wall.textureAsset).WaitForCompletion();
            if (string.IsNullOrEmpty(wall.normalAsset))
            {
                material.SetTexture("_WallNormalTex", null);
            }
            else
            {
                material.SetTexture("_WallNormalTex", Addressables.LoadAssetAsync<Texture2D>(wall.normalAsset).WaitForCompletion());
            }
            material.SetFloat("_WallBias", wall.bias);
            material.SetColor("_WallColor", wall.averageColor);
            material.SetFloat("_WallScale", wall.scale);
            material.SetFloat("_WallBumpScale", wall.bumpScale);
            material.SetFloat("_WallContrast", wall.constrast);
            material.SetFloat("_WallGlossiness", wall.glossiness);
            material.SetFloat("_WallMetallic", wall.metallic);

            material.SetTexture("_FloorTex", Addressables.LoadAssetAsync<Texture2D>(floor.textureAsset).WaitForCompletion());
            if (string.IsNullOrEmpty(floor.normalAsset))
            {
                material.SetTexture("_FloorNormalTex", null);
            }
            else
            {
                material.SetTexture("_FloorNormalTex", Addressables.LoadAssetAsync<Texture2D>(floor.normalAsset).WaitForCompletion());
            }
            material.SetFloat("_FloorBias", floor.bias);
            material.SetColor("_FloorColor", floor.averageColor);
            material.SetFloat("_FloorScale", floor.scale);
            material.SetFloat("_FloorBumpScale", floor.bumpScale);
            material.SetFloat("_FloorContrast", floor.constrast);
            material.SetFloat("_FloorGlossiness", floor.glossiness);
            material.SetFloat("_FloorMetallic", floor.metallic);

            return material;
        }
    }
}
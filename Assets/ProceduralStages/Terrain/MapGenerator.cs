using RoR2;
using RoR2.EntityLogic;
using RoR2.ExpansionManagement;
using RoR2.Navigation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.Rendering.PostProcessing;
using UnityMeshSimplifier;

namespace ProceduralStages
{
    //[DefaultExecutionOrder(-1)]
    public class MapGenerator : MonoBehaviour
    {
        [HideInInspector]
        public Vector3Int stageSize;

        [Range(0, 10)]
        public float mapScale = 1f;

        [Range(0, 1)]
        public float meshQuality = 0.1f;

        public bool loadResourcesInEditor = false;
        public bool loadPropsInEditor = false;
        public ulong editorSeed;
        public TerrainType editorTerrainType;
        public Theme editorTheme;
        [Range(1, 5)]
        public int editorStageInLoop = 1;

        public TerrainGenerator[] terrainGenerators;
        public MapTheme[] themes;

        public MeshColorer meshColorer = new MeshColorer();

        public NodeGraphCreator nodeGraphCreator = new NodeGraphCreator();

        public DccsPoolGenerator dccsPoolGenerator = new DccsPoolGenerator();

        public PropsPlacer propsPlacer = new PropsPlacer();

        public PostProcessVolume postProcessVolume;

        public MeshRenderer waterRenderer;
        public MeshRenderer seaFloorRenderer;
        public GameObject waterPostProcessingObject;

        public GameObject sceneInfoObject;
        public GameObject directorObject;

        public BoxCollider oobZone;
        public AwuEventBehaviour awuEvent;

        public Graphs graphs;

        private ulong lastSeed;
        public static MapGenerator instance;
        public static Xoroshiro128Plus rng;

        private void Awake()
        {
            instance = this;
            if (Application.IsPlaying(this))
            {
                lastSeed = SetSeed();
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
            instance = null;
            rng = null;
        }

        private void Update()
        {
            if (Application.isEditor && (Input.GetKeyDown(KeyCode.F2) || Input.GetKeyDown(KeyCode.F3)))
            {
                for (int i = 0; i < propsPlacer.instances.Count; i++)
                {
                    Destroy(propsPlacer.instances[i]);
                }
                propsPlacer.instances.Clear();

                if (Input.GetKeyDown(KeyCode.F2))
                {
                    lastSeed = SetSeed();
                }
                else
                {
                    rng = new Xoroshiro128Plus(lastSeed);
                }

                GenerateMap();
            }
        }

        private void GenerateMap()
        {
            int stageClearCount = RunConfig.instance.nextStageClearCount;
            int stageInLoop = Application.isEditor
                ? editorStageInLoop
                : (stageClearCount % Run.stagesPerLoop) + 1;

            int stageScaling = RunConfig.instance.infiniteMapScaling
                ? stageClearCount + 1
                : stageInLoop;

            Stopwatch totalStopwatch = Stopwatch.StartNew();

            Stopwatch stopwatch = Stopwatch.StartNew();

            TerrainType terrainType = RunConfig.instance.selectedTerrainType;
            if (Application.isEditor)
            {
                if (editorTerrainType == TerrainType.Random)
                {
                    terrainType = terrainGenerators[rng.RangeInt(1, terrainGenerators.Length)].terrainType;
                }
                else
                {
                    terrainType = editorTerrainType;
                }
            }
            else if (terrainType == TerrainType.Random)
            {
                var typesWeights = RunConfig.instance.terrainTypesPercents
                    .Where(x => x.StageIndex + 1 == stageInLoop)
                    .ToList();

                WeightedSelection<TerrainType> selection = new WeightedSelection<TerrainType>(typesWeights.Count);

                for (int i = 0; i < typesWeights.Count; i++)
                {
                    var config = typesWeights[i];
                    selection.AddChoice(config.TerrainType, config.Percent);
                }

                terrainType = selection.totalWeight > 0
                    ? selection.Evaluate(rng.nextNormalizedFloat)
                    : TerrainType.OpenCaves;

            }

            Log.Debug(terrainType);

            TerrainGenerator terrainGenerator = terrainGenerators.First(x => x.terrainType == terrainType);
            RunConfig.instance.selectedTerrainType = TerrainType.Random;

            PostProcessVolume waterPPV = waterPostProcessingObject.GetComponent<PostProcessVolume>();
            ColorGrading waterColorGrading = waterPPV.profile.GetSetting<ColorGrading>();
            BoxCollider waterPPCollider = waterPostProcessingObject.GetComponent<BoxCollider>();

            waterRenderer.gameObject.transform.position = new Vector3(0, terrainGenerator.waterLevel, 0);
            waterPPCollider.center = new Vector3(0, terrainGenerator.waterLevel / 2, 0);
            waterPPCollider.size = new Vector3(10000, terrainGenerator.waterLevel, 10000);

            stageSize = terrainGenerator.size + stageScaling * terrainGenerator.sizeIncreasePerStage;
            stageSize.x -= Mathf.CeilToInt(rng.nextNormalizedFloat * stageSize.x * terrainGenerator.sizeVariation.x);
            stageSize.y -= Mathf.CeilToInt(rng.nextNormalizedFloat * stageSize.y * terrainGenerator.sizeVariation.y);
            stageSize.z -= Mathf.CeilToInt(rng.nextNormalizedFloat * stageSize.z * terrainGenerator.sizeVariation.z);

            Terrain terrain = terrainGenerator.Generate();

            var scaledSize = new Vector3(stageSize.x * mapScale, stageSize.y * mapScale * 1.5f, stageSize.z * mapScale);
            oobZone.size = scaledSize;
            oobZone.center = scaledSize / 2;

            Theme themeType = Theme.LegacyRandom;
            if (Application.isEditor && editorTheme != Theme.Random)
            {
                themeType = editorTheme;
            }
            else
            {
                //todo: add to config
                themeType = themes[rng.RangeInt(1, themes.Length)].Theme;
            }

            MapTheme theme = themes.First(x => x.Theme == themeType);

            meshColorer.ColorMesh(terrain.meshResult);
            LogStats("meshColorer");

            GetComponent<MeshFilter>().mesh = terrain.meshResult.mesh;
            LogStats("MeshFilter");

            Material terrainMaterial = GetComponent<MeshRenderer>().material;
            var profile = postProcessVolume.profile;
            RampFog fog = profile.GetSetting<RampFog>();
            Vignette vignette = profile.GetSetting<Vignette>();

            Texture2D colorGradiant = theme.ApplyTheme(terrainGenerator, terrainMaterial, fog, vignette, waterRenderer, waterColorGrading, seaFloorRenderer);

            GetComponent<SurfaceDefProvider>().surfaceDef = Addressables.LoadAssetAsync<SurfaceDef>("RoR2/Base/Common/sdStone.asset").WaitForCompletion();
            LogStats("surfaceDef");

            GetComponent<MeshCollider>().sharedMesh = terrain.meshResult.mesh;
            LogStats("MeshCollider");

            graphs = nodeGraphCreator.CreateGraphs(terrain);
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
                stageInfo.sceneDirectorMonsterCredits = 30 * (stageScaling + 4);
            }

            if (!IsSimulacrum() || Application.isEditor)
            {
                stageInfo.sceneDirectorInteractibleCredits = 75 * (stageScaling + 2);
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

                        SpecialInteractablesPlacer.Place(graphs, stageInLoop, IsSimulacrum());
                    };

                    SceneDirector.onPostPopulateSceneServer += placeInteractables;
                }
            }

            if (!Application.isEditor || loadPropsInEditor)
            {
                PropsDefinitionCollection propsCollection = theme.propCollections[rng.RangeInt(0, theme.propCollections.Length)];
                propsPlacer.PlaceAll(
                    graphs,
                    propsCollection,
                    meshColorer,
                    colorGradiant,
                    terrainMaterial,
                    terrainGenerator.ceillingPropsWeight);
                LogStats("propsPlacer");
            }

            if (!Application.isEditor)
            {
                var stages = SceneCatalog.allStageSceneDefs
                    .Where(x => x.cachedName != Main.SceneName)
                    .ToList();

                var mainTracks = stages
                    .Select(x => x.mainTrack)
                    .Where(x => x)
                    .Distinct()
                    .ToList();

                var bossTracks = stages
                    .Select(x => x.bossTrack)
                    .Distinct()
                    .Where(x => x.cachedName != "muRaidfightDLC1_07" && x.cachedName != "muSong25")
                    .ToList();

                var mainTrack = mainTracks[rng.RangeInt(0, mainTracks.Count)];
                var bossTrack = bossTracks[rng.RangeInt(0, bossTracks.Count)];

                Action<SceneDef> onSceneChanged = null;
                onSceneChanged = scene =>
                {
                    SceneCatalog.onMostRecentSceneDefChanged -= onSceneChanged;
                    if (scene.cachedName == Main.SceneName)
                    {
                        scene.mainTrack = mainTrack;
                        scene.bossTrack = bossTrack;
                        scene.nameToken = terrainType.GetName();
                        scene.subtitleToken = terrainType.GetSubTitle();
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

        private ulong SetSeed()
        {
            ulong currentSeed;
            if (Application.isEditor)
            {
                if (editorSeed != 0)
                {
                    currentSeed = editorSeed;
                }
                else if (RunConfig.instance.stageRng != null)
                {
                    currentSeed = RunConfig.instance.stageRng.nextUlong;
                }
                else
                {
                    currentSeed = (ulong)DateTime.Now.Ticks;
                }
            }
            else
            {
                if (RunConfig.instance.stageSeed != "")
                {
                    if (ulong.TryParse(RunConfig.instance.stageSeed, out ulong seed))
                    {
                        currentSeed = seed;
                    }
                    else
                    {
                        currentSeed = (ulong)RunConfig.instance.stageSeed.GetHashCode();
                    }
                }
                else
                {
                    currentSeed = RunConfig.instance.stageRng.nextUlong;
                }
            }

            Log.Debug("Stage Seed: " + currentSeed);

            rng = new Xoroshiro128Plus(currentSeed);
            return currentSeed;
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
    }
}
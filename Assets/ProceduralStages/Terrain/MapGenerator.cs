﻿using RoR2;
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
        public SurfaceTexture editorFloorTexture;
        public SurfaceTexture editorWallTexture;
        public SurfaceTexture editorDetailTexture;
        public SkyboxDef editorSkybox;

        public TerrainGenerator[] terrainGenerators;
        public MapThemeCollection themes;

        public MeshColorer meshColorer = new MeshColorer();

        public NodeGraphCreator nodeGraphCreator = new NodeGraphCreator();
        public DccsPoolGenerator dccsPoolGenerator = new DccsPoolGenerator();
        public PropsPlacer propsPlacer = new PropsPlacer();
        public OcclusionCulling occlusionCulling;

        public PostProcessVolume postProcessVolume;

        public MeshRenderer waterRenderer;
        public GameObject waterController;
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
        public static Xoroshiro128Plus serverRng;


        [HideInInspector]
        public StageType stageType;

        private List<GameObject> terrainCustomObject;
        private void Awake()
        {
            instance = this;
            if (Application.IsPlaying(this))
            {
                lastSeed = SetSeed();
                GenerateMap();
            }
        }

        private void OnDestroy()
        {
            instance = null;
            rng = null;
            serverRng = null;
        }

        private bool generateNextFrame;

        private void Update()
        {
            if (generateNextFrame)
            {
                generateNextFrame = false;
                GenerateMap();
            }

            if (Application.isEditor && (Input.GetKeyDown(KeyCode.F2) || Input.GetKeyDown(KeyCode.F3)))
            {
                for (int i = 0; i < propsPlacer.instances.Count; i++)
                {
                    Destroy(propsPlacer.instances[i]);
                }
                propsPlacer.instances.Clear();

                for (int i = 0; i < terrainCustomObject.Count; i++)
                {
                    if (terrainCustomObject[i] != null)
                    {
                        Destroy(terrainCustomObject[i]);
                    }
                }

                if (Input.GetKeyDown(KeyCode.F2))
                {
                    lastSeed = SetSeed();
                }
                else
                {
                    rng = new Xoroshiro128Plus(lastSeed);
                    serverRng = new Xoroshiro128Plus(lastSeed + 1);
                }

                generateNextFrame = true;
            }
        }

        private void GenerateMap()
        {
            ProfilerLog.Reset();
            using (ProfilerLog.CreateScope("total"))
            {
                int stageClearCount = RunConfig.instance.nextStageClearCount;
                int stageInLoop = Application.isEditor
                    ? editorStageInLoop
                    : (stageClearCount % Run.stagesPerLoop) + 1;

                int stageScaling = RunConfig.instance.infiniteMapScaling
                    ? stageClearCount + 1
                    : stageInLoop;

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
                else if (ModConfig.PotRollingModeEnabled.Value)
                {
                    terrainType = TerrainType.PotRolling;
                }
                else if (terrainType == TerrainType.Random)
                {
                    var typesWeights = RunConfig.instance.terrainTypesPercents
                        .Where(x => x.StageIndex + 1 == stageInLoop)
                        .ToList();

                    WeightedSelection<TerrainType> filteredSelection = new WeightedSelection<TerrainType>(typesWeights.Count);
                    WeightedSelection<TerrainType> allSelection = new WeightedSelection<TerrainType>(typesWeights.Count);

                    int loopIndex = stageClearCount / Run.stagesPerLoop;
                    TerrainType[] terrainTypesVisitedInLoop = RunConfig.instance.terrainTypeVisits
                        .Where(x => (x.stageCount - 1) / Run.stagesPerLoop == loopIndex)
                        .Select(x => x.terrainType)
                        .ToArray();

                    for (int i = 0; i < typesWeights.Count; i++)
                    {
                        var config = typesWeights[i];
                        if (RunConfig.instance.terrainRepetition == TerrainRepetition.NonePerLoop && !terrainTypesVisitedInLoop.Contains(config.TerrainType))
                        {
                            filteredSelection.AddChoice(config.TerrainType, config.Percent);
                        }
                        allSelection.AddChoice(config.TerrainType, config.Percent);
                    }

                    terrainType = filteredSelection.totalWeight > 0
                        ? filteredSelection.Evaluate(rng.nextNormalizedFloat)
                        : allSelection.totalWeight > 0
                            ? allSelection.Evaluate(rng.nextNormalizedFloat)
                            : TerrainType.OpenCaves;

                    if (NetworkServer.active)
                    {
                        RunConfig.instance.terrainTypeVisits.Add(new TerrainTypeVisit
                        {
                            stageCount = stageClearCount + 1,
                            terrainType = terrainType
                        });
                    }
                }
                //terrainType = TerrainType.Moon;
                Log.Debug(terrainType);

                stageType = terrainType == TerrainType.PotRolling
                    ? StageType.PotRolling
                        : terrainType == TerrainType.Moon
                        ? StageType.Moon
                        : !Application.isEditor && Run.instance is InfiniteTowerRun
                            ? StageType.Simulacrum
                            : StageType.Regular;

                TerrainGenerator terrainGenerator = terrainGenerators.First(x => x.terrainType == terrainType);
                RunConfig.instance.selectedTerrainType = TerrainType.Random;

                PostProcessVolume waterPPV = waterPostProcessingObject.GetComponent<PostProcessVolume>();
                ColorGrading waterColorGrading = waterPPV.profile.GetSetting<ColorGrading>();
                BoxCollider waterPPCollider = waterPostProcessingObject.GetComponent<BoxCollider>();
                BoxCollider waterControllerBoxController = waterController.GetComponent<BoxCollider>();

                waterRenderer.gameObject.transform.position = new Vector3(0, terrainGenerator.waterLevel, 0);
                waterPPCollider.center = new Vector3(0, terrainGenerator.waterLevel / 2, 0);
                waterPPCollider.size = new Vector3(10000, terrainGenerator.waterLevel, 10000);

                waterControllerBoxController.center = waterPPCollider.center;
                waterControllerBoxController.size = waterPPCollider.size;

                waterControllerBoxController.GetComponent<SurfaceDefProvider>().surfaceDef = Addressables.LoadAssetAsync<SurfaceDef>("RoR2/Base/Common/sdWater.asset").WaitForCompletion();

                if (!Application.isEditor && stageType == StageType.PotRolling)
                {
                    stageSize = new Vector3Int(
                        Mathf.CeilToInt(ModConfig.PotRollingStageWidth.Value / mapScale),
                        Mathf.CeilToInt(ModConfig.PotRollingStageHeight.Value / mapScale),
                        Mathf.CeilToInt(ModConfig.PotRollingStageDepth.Value / mapScale));
                }
                else
                {
                    stageSize = terrainGenerator.size + stageScaling * terrainGenerator.sizeIncreasePerStage;
                    stageSize.x -= Mathf.CeilToInt(rng.nextNormalizedFloat * stageSize.x * terrainGenerator.sizeVariation.x);
                    stageSize.y -= Mathf.CeilToInt(rng.nextNormalizedFloat * stageSize.y * terrainGenerator.sizeVariation.y);
                    stageSize.z -= Mathf.CeilToInt(rng.nextNormalizedFloat * stageSize.z * terrainGenerator.sizeVariation.z);
                }
                
                Terrain terrain;
                using (ProfilerLog.CreateScope("terrainGenerator.Generate"))
                {
                    terrain = terrainGenerator.Generate();
                }

                var scaledSize = new Vector3(stageSize.x * mapScale * terrain.oobScale.x, stageSize.y * mapScale * terrain.oobScale.y, stageSize.z * mapScale * terrain.oobScale.z);
                oobZone.size = scaledSize;
                oobZone.center = 0.5f * new Vector3(stageSize.x * mapScale, stageSize.y * mapScale * terrain.oobScale.y, stageSize.z * mapScale);

                Theme themeType = Theme.LegacyRandom;
                if (Application.isEditor)
                {
                    if (editorTheme == Theme.Random)
                    {
                        themeType = themes.themes[rng.RangeInt(1, themes.themes.Length)].Theme;
                    }
                    else
                    {
                        themeType = editorTheme;
                    }
                }
                else if (RunConfig.instance.selectedTheme != Theme.Random)
                {
                    themeType = RunConfig.instance.selectedTheme;
                    RunConfig.instance.selectedTheme = Theme.Random;
                }
                else
                {
                    WeightedSelection<Theme> selection = new WeightedSelection<Theme>(RunConfig.instance.themePercents.Length);

                    for (int i = 0; i < RunConfig.instance.themePercents.Length; i++)
                    {
                        var config = RunConfig.instance.themePercents[i];
                        selection.AddChoice(config.Theme, config.Percent);
                    }

                    themeType = selection.totalWeight > 0
                        ? selection.Evaluate(rng.nextNormalizedFloat)
                        : Theme.Plains;
                }

                Log.Debug(themeType);
                MapTheme theme = themes.themes.First(x => x.Theme == themeType);

                if (Application.isEditor)
                {
                    foreach (var t in themes.themes)
                    {
                        t.CheckAssets();
                    }
                }

                meshColorer.ColorMesh(terrain.meshResult, Matrix4x4.identity, rng);
                ProfilerLog.Debug("meshColorer");

                GetComponent<MeshFilter>().mesh = terrain.meshResult.mesh;
                ProfilerLog.Debug("MeshFilter");


                GetComponent<MeshCollider>().sharedMesh = terrain.meshResult.mesh;
                ProfilerLog.Debug("MeshCollider");

                graphs = nodeGraphCreator.CreateGraphs(terrain);
                ProfilerLog.Debug("nodeGraphs");

                SceneInfo sceneInfo = sceneInfoObject.GetComponent<SceneInfo>();
                sceneInfo.groundNodes = graphs.ground;
                sceneInfo.airNodes = graphs.air;

                ClassicStageInfo stageInfo = sceneInfoObject.GetComponent<ClassicStageInfo>();

                SetDCCS(stageInfo);
                ProfilerLog.Debug("SetDCCS");

                if (Application.isEditor)
                {
                    directorObject.SetActive(false);
                }

                var combatDirectors = directorObject.GetComponents<CombatDirector>();
                if (stageType == StageType.Simulacrum || stageType == StageType.PotRolling)
                {
                    foreach (var combatDirector in combatDirectors)
                    {
                        combatDirector.monsterCredit = float.MinValue;
                        combatDirector.moneyWaveIntervals = new RangeFloat[0];
                        combatDirector.moneyWaves = new CombatDirector.DirectorMoneyWave[0];
                    }
                }

                if (stageType != StageType.Simulacrum && stageType != StageType.PotRolling)
                {
                    stageInfo.sceneDirectorMonsterCredits = 30 * (stageScaling + 4);
                }

                if (stageType == StageType.Regular || Application.isEditor)
                {
                    stageInfo.sceneDirectorInteractibleCredits = 75 * (stageScaling + 2);
                }

                SceneDirector sceneDirector = directorObject.GetComponent<SceneDirector>();

                bool useLunarPortal = stageInLoop == Run.stagesPerLoop;
                string portalPath = useLunarPortal
                    ? "RoR2/Base/Teleporters/iscLunarTeleporter.asset"
                    : "RoR2/Base/Teleporters/iscTeleporter.asset";

                if (stageType == StageType.Regular)
                {
                    sceneDirector.teleporterSpawnCard = Addressables.LoadAssetAsync<InteractableSpawnCard>(portalPath).WaitForCompletion();
                }

                if (!Application.isEditor)
                {
                    if (NetworkServer.active)
                    {
                        Action<SceneDirector> placeInteractables = null;
                        placeInteractables = _ =>
                        {
                            SceneDirector.onPostPopulateSceneServer -= placeInteractables;

                            SpecialInteractablesPlacer.Place(graphs, stageInLoop, terrain.moonTerrain);
                        };

                        SceneDirector.onPostPopulateSceneServer += placeInteractables;
                    }
                }

                Material terrainMaterial = GetComponent<MeshRenderer>().material;
                var profile = postProcessVolume.profile;
                RampFog fog = profile.GetSetting<RampFog>();
                Vignette vignette = profile.GetSetting<Vignette>();

                Texture2D colorGradiant = theme.ApplyTheme(
                    terrainGenerator,
                    terrainMaterial,
                    fog,
                    vignette,
                    waterRenderer,
                    waterColorGrading,
                    seaFloorRenderer,
                    GetComponent<SurfaceDefProvider>());
                ProfilerLog.Debug("surfaceDef");

                if (!Application.isEditor || loadPropsInEditor)
                {
                    PropsDefinitionCollection propsCollection = theme.propCollections[rng.RangeInt(0, theme.propCollections.Length)];
                    propsPlacer.PlaceAll(
                        MapGenerator.rng,
                        Vector3.zero,
                        graphs,
                        propsCollection,
                        meshColorer,
                        colorGradiant,
                        terrainMaterial,
                        terrainGenerator.ceillingPropsWeight,
                        terrainGenerator.propCountWeight,
                        new Bounds(Vector3.zero, new Vector3(1000000, 100000, 10000)));

                    ProfilerLog.Debug("propsPlacer");

                    terrainGenerator.AddProps(terrain, graphs);
                    terrainCustomObject = terrain.customObjects;
                    ProfilerLog.Debug("terrainGenerator.AddProps");

                    if (terrainGenerator.backdropGenerator != null)
                    {
                        using (ProfilerLog.CreateScope("backdropGenerator.Generate"))
                        {
                            GameObject[] backdropObjects = terrainGenerator.backdropGenerator.Generate(terrainMaterial, colorGradiant, propsCollection);
                            terrainCustomObject.AddRange(backdropObjects);
                        }
                    }

                    using (ProfilerLog.CreateScope("OcclusionCulling.SetTargets"))
                    {
                        Vector3 mapSize = mapScale * (Vector3)stageSize;
                        occlusionCulling.SetTargets(propsPlacer.instances, new Bounds(mapSize / 2f, mapSize));
                    }
                }

                if (!Application.isEditor)
                {
                    var stages = SceneCatalog.allStageSceneDefs
                        .Where(x => x.cachedName != Main.SceneName)
                        .ToList();

                    MusicTrackDef mainTrack;
                    MusicTrackDef bossTrack;

                    if (stageType == StageType.Moon)
                    {
                        var moonStage = stages.First(x => x.cachedName == "moon2");
                        mainTrack = moonStage.mainTrack;
                        bossTrack = moonStage.bossTrack;
                    }
                    else
                    {
                        var mainTracks = stages
                            .Select(x => x.mainTrack)
                            .Where(x => x)
                            .Distinct()
                            .ToList();

                        var bossTracks = stages
                            .Select(x => x.bossTrack)
                            .Where(x => x)
                            .Distinct()
                            .Where(x => x.cachedName != "muRaidfightDLC1_07" && x.cachedName != "muSong25")
                            .ToList();

                        mainTrack = mainTracks[rng.RangeInt(0, mainTracks.Count)];
                        bossTrack = bossTracks[rng.RangeInt(0, bossTracks.Count)];
                    }

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
                    ProfilerLog.Debug("music");
                }
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
            serverRng = new Xoroshiro128Plus(currentSeed + 1);
            return currentSeed;
        }

        private void SetDCCS(ClassicStageInfo stageInfo)
        {
            if (Application.isEditor && !loadResourcesInEditor)
            {
                return;
            }

            ExpansionDef dlc1Def = ExpansionCatalog.expansionDefs.FirstOrDefault(x => x.name == "DLC1");
            ExpansionDef dlc2Def = ExpansionCatalog.expansionDefs.FirstOrDefault(x => x.name == "DLC2");

            bool hasDLC1 = Application.isEditor || (dlc1Def && Run.instance.IsExpansionEnabled(dlc1Def));
            bool hasDLC2 = Application.isEditor || (dlc2Def && Run.instance.IsExpansionEnabled(dlc2Def));

            var validPools = DccsPoolItem.All
                .Where(x => stageType == StageType.PotRolling || x.StageType == stageType)
                .Where(x => hasDLC1 || !x.DLC1)
                .Where(x => hasDLC2 || !x.DLC2)
                .ToList();

            if (stageType != StageType.Regular)
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
                stageInfo.monsterDccsPool = dccsPoolGenerator.GenerateMonstersDccs(hasDLC1, hasDLC2);
            }

            var dpInteratables = validPools
                .Where(x => x.Type == DccsPoolItemType.Interactables)
                .Select(x => x.Asset)
                .ToList();

            string dpInteratable = dpInteratables[rng.RangeInt(0, dpInteratables.Count)];
            Log.Debug(dpInteratable);

            stageInfo.interactableDccsPool = Addressables.LoadAssetAsync<DccsPool>(dpInteratable).WaitForCompletion();
        }
    }
}
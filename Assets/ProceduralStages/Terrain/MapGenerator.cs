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
        public Vector3Int size;
        public Vector3Int sizeIncreasePerStage;
        public Vector3 sizeVariation;

        [HideInInspector]
        public Vector3Int stageSize;

        [Range(0, 10)]
        public float mapScale = 1f;

        [Range(0, 1)]
        public float meshQuality = 0.1f;

        public bool loadResourcesInEditor = false;
        public bool loadPropsInEditor = false;
        public ulong editorSeed;

        public TerrainGenerator[] terrainGenerators;
        public MapTheme[] themes;

        public MeshColorer meshColorer = new MeshColorer();

        public NodeGraphCreator nodeGraphCreator = new NodeGraphCreator();

        public DccsPoolGenerator dccsPoolGenerator = new DccsPoolGenerator();

        public PropsPlacer propsPlacer = new PropsPlacer();

        public GameObject sceneInfoObject;
        public GameObject postProcessingObject;
        public GameObject directorObject;
        public GameObject oobZoneObject;
        public GameObject awuEventObject;

        public Graphs graphs;

        public static MapGenerator instance;
        public static Xoroshiro128Plus rng;

        private void Awake()
        {
            instance = this;
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
            instance = null;
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

        private void GenerateMap()
        {
            int stageInLoop = ((Run.instance?.stageClearCount ?? 0) % Run.stagesPerLoop) + 1;

            ulong currentSeed;
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

            stageSize = size + stageInLoop * sizeIncreasePerStage;
            stageSize.x -= Mathf.CeilToInt(rng.nextNormalizedFloat * stageSize.x * sizeVariation.x);
            stageSize.y -= Mathf.CeilToInt(rng.nextNormalizedFloat * stageSize.y * sizeVariation.y);
            stageSize.z -= Mathf.CeilToInt(rng.nextNormalizedFloat * stageSize.z * sizeVariation.z);

            BoxCollider oobZone = oobZoneObject.GetComponent<BoxCollider>();
            var scaledSize = new Vector3(stageSize.x * mapScale, stageSize.y * mapScale, stageSize.z * mapScale);

            oobZone.size = scaledSize;
            oobZone.center = scaledSize / 2;

            TerrainGenerator terrainGenerator = terrainGenerators[rng.RangeInt(0, terrainGenerators.Length)];
            Terrain terrain = terrainGenerator.Generate();

            MapTheme theme = themes[rng.RangeInt(0, themes.Length)];

            meshColorer.ColorMesh(terrain.meshResult);
            LogStats("meshColorer");

            GetComponent<MeshFilter>().mesh = terrain.meshResult.mesh;
            LogStats("MeshFilter");

            var terrainMaterial = GetComponent<MeshRenderer>().material;
            var colorGradiant = theme.SetTexture(terrainMaterial);

            var surface = ScriptableObject.CreateInstance<SurfaceDef>();
            surface.approximateColor = theme.colorPalette.AverageColor(colorGradiant);
            surface.materialSwitchString = "stone";

            RampFog fog = postProcessingObject.GetComponent<PostProcessVolume>().profile.GetSetting<RampFog>();
            theme.SetSunAndFog(fog);

            GetComponent<SurfaceDefProvider>().surfaceDef = surface;
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

                        SpecialInteractablesPlacer.Place(graphs, stageInLoop, IsSimulacrum());
                    };

                    SceneDirector.onPostPopulateSceneServer += placeInteractables;
                }
            }

            if (!Application.isEditor || loadPropsInEditor)
            {
                propsPlacer.PlaceAll(graphs, meshColorer, colorGradiant, terrainMaterial);
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
    }
}
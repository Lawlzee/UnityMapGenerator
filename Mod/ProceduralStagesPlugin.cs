using BepInEx;
using BepInEx.Configuration;
using RiskOfOptions.Options;
using RiskOfOptions;
using UnityEngine;
using System.IO;
using RiskOfOptions.OptionConfigs;
using RoR2;
using UnityEngine.AddressableAssets;
using R2API;
using UnityEngine.SceneManagement;
using Generator.Assets.Scripts;
using RoR2.Navigation;
using System;
using RoR2.ContentManagement;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine.Networking;

namespace ProceduralStages
{
    [BepInDependency("com.rune580.riskofoptions")]
    [BepInDependency("com.bepis.r2api.stages")]
    [BepInDependency("com.bepis.r2api.language")]
    [BepInDependency("com.bepis.r2api.content_management")]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class ProceduralStagesPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "Lawlzee.ProceduralStages";
        public const string PluginAuthor = "Lawlzee";
        public const string PluginName = "ProceduralStages";
        public const string PluginVersion = "1.0.1";

        public static ConfigEntry<bool> ReplaceAllStages;
        public static ConfigEntry<float> FloorSaturation;
        public static ConfigEntry<float> FloorValue;
        public static ConfigEntry<float> WallsSaturation;
        public static ConfigEntry<float> WallsValue;
        public static ConfigEntry<float> CeillingSaturation;
        public static ConfigEntry<float> CeillingValue;
        public static ConfigEntry<float> LightSaturation;
        public static ConfigEntry<float> LightValue;

        public void Awake()
        {
            Log.Init(Logger);

            ReplaceAllStages = Config.Bind("Configuration", "Replace all stages", true, "If enabled, all the stages will be procedurally generated. If disabled, normal stages and procedurally generated stages will be used.");
            ModSettingsManager.AddOption(new CheckBoxOption(ReplaceAllStages));

            FloorSaturation = Config.Bind("Advanced", nameof(FloorSaturation), 0.5f);
            FloorValue = Config.Bind("Advanced", nameof(FloorValue), 0.36f);
            WallsSaturation = Config.Bind("Advanced", nameof(WallsSaturation), 0.23f);
            WallsValue = Config.Bind("Advanced", nameof(WallsValue), 0.27f);
            CeillingSaturation = Config.Bind("Advanced", nameof(CeillingSaturation), 0.3f);
            CeillingValue = Config.Bind("Advanced", nameof(CeillingValue), 0.15f);
            LightSaturation = Config.Bind("Advanced", nameof(LightSaturation), 0.7f);
            LightValue = Config.Bind("Advanced", nameof(LightValue), 0.7f);

            ModSettingsManager.AddOption(new SliderOption(FloorSaturation, new SliderConfig { min = 0, max = 1, formatString = "{0:0.00}" }));
            ModSettingsManager.AddOption(new SliderOption(FloorValue, new SliderConfig { min = 0, max = 1, formatString = "{0:0.00}" }));
            ModSettingsManager.AddOption(new SliderOption(WallsSaturation, new SliderConfig { min = 0, max = 1, formatString = "{0:0.00}" }));
            ModSettingsManager.AddOption(new SliderOption(WallsValue, new SliderConfig { min = 0, max = 1, formatString = "{0:0.00}" }));
            ModSettingsManager.AddOption(new SliderOption(CeillingSaturation, new SliderConfig { min = 0, max = 1, formatString = "{0:0.00}" }));
            ModSettingsManager.AddOption(new SliderOption(CeillingValue, new SliderConfig { min = 0, max = 1, formatString = "{0:0.00}" }));
            ModSettingsManager.AddOption(new SliderOption(LightSaturation, new SliderConfig { min = 0, max = 1, formatString = "{0:0.00}" }));
            ModSettingsManager.AddOption(new SliderOption(LightValue, new SliderConfig { min = 0, max = 1, formatString = "{0:0.00}" }));


            On.RoR2.RoR2Application.LoadGameContent += RoR2Application_LoadGameContent;

            //On.RoR2.SceneDirector.PlaceTeleporter += SceneDirector_PlaceTeleporter;

            var texture = LoadTexture("icon.png");
            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0, 0));
            ModSettingsManager.SetModIcon(sprite);

            ContentManager.collectContentPackProviders += GiveToRoR2OurContentPackProviders;
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;

            On.RoR2.WireMeshBuilder.GenerateMesh_Mesh += WireMeshBuilder_GenerateMesh_Mesh;
            SceneCatalog.onMostRecentSceneDefChanged += SceneCatalog_onMostRecentSceneDefChanged;

            On.RoR2.Run.PickNextStageScene += Run_PickNextStageScene;
            On.RoR2.SceneDirector.DefaultPlayerSpawnPointGenerator += SceneDirector_DefaultPlayerSpawnPointGenerator;
            On.RoR2.SceneDirector.PlacePlayerSpawnsViaNodegraph += SceneDirector_PlacePlayerSpawnsViaNodegraph;
            
        }

        public void Start()
        {
            //This should be after DirectorPlugin.Awake();
            On.RoR2.ClassicStageInfo.Start += ClassicStageInfo_Start;
        }

        private void ClassicStageInfo_Start(On.RoR2.ClassicStageInfo.orig_Start orig, ClassicStageInfo self)
        {
            if (SceneCatalog.currentSceneDef.cachedName == "random")
            {
                //Ungly hack to workaround the caching of R2API.DirectorAPI
                SceneCatalog.currentSceneDef.cachedName = Guid.NewGuid().ToString(); 
                try
                {
                    orig(self);
                }
                finally
                {
                    SceneCatalog.currentSceneDef.cachedName = "random";
                }

                return;
            }

            orig(self);
        }

        private Texture2D LoadTexture(string name)
        {
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(File.ReadAllBytes(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location), name)));
            return texture;
        }

        private void SceneDirector_PlacePlayerSpawnsViaNodegraph(On.RoR2.SceneDirector.orig_PlacePlayerSpawnsViaNodegraph orig, SceneDirector self)
        {
            if (Run.instance.spawnWithPod && SceneCatalog.currentSceneDef.cachedName == "random")
            {
                bool oldValue = Stage.instance.usePod;
                try
                {
                    Stage.instance.usePod = false;
                    orig(self);

                }
                finally
                {
                    Stage.instance.usePod = oldValue;
                }

                return;
            }

            orig(self);


        }

        private void SceneDirector_DefaultPlayerSpawnPointGenerator(On.RoR2.SceneDirector.orig_DefaultPlayerSpawnPointGenerator orig, SceneDirector self)
        {
            if (Run.instance.spawnWithPod && SceneCatalog.currentSceneDef.cachedName == "random")
            {
                self.RemoveAllExistingSpawnPoints();
                self.PlacePlayerSpawnsViaNodegraph();
                return;
            }

            orig(self);
        }
        private void Run_PickNextStageScene(On.RoR2.Run.orig_PickNextStageScene orig, Run self, WeightedSelection<SceneDef> choices)
        {
            SceneDef scene = choices.choices
                .Select(x => x.value)
                .Where(x => x.cachedName == "random")
                .FirstOrDefault();

            if (ReplaceAllStages.Value && scene != null)
            {
                self.nextStageScene = scene;
            }
            else
            {
                orig(self, choices);
            }
        }

        private void WireMeshBuilder_GenerateMesh_Mesh(On.RoR2.WireMeshBuilder.orig_GenerateMesh_Mesh orig, WireMeshBuilder self, Mesh dest)
        {
            dest.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            orig(self, dest);
        }

        private void GiveToRoR2OurContentPackProviders(ContentManager.AddContentPackProviderDelegate addContentPackProvider)
        {
            addContentPackProvider(new ContentProvider(this));
        }

        private System.Collections.IEnumerator RoR2Application_LoadGameContent(On.RoR2.RoR2Application.orig_LoadGameContent orig, RoR2Application self)
        {
            InitSceneDef();

            return orig(self);
        }

        private void InitSceneDef()
        {

            for (int i = 1; i <= 5; i++)
            {
                int destinationIndex = (i % 5) + 1;
                SceneCollection stageSceneCollectionRequest = Addressables.LoadAssetAsync<SceneCollection>($"RoR2/Base/SceneGroups/sgStage{destinationIndex}.asset").WaitForCompletion();

                SceneDef sceneDef = ScriptableObject.CreateInstance<SceneDef>();
                sceneDef.cachedName = "random";
                sceneDef.sceneType = SceneType.Stage;
                sceneDef.isOfflineScene = false;
                sceneDef.stageOrder = i;
                sceneDef.nameToken = "MAP_RANDOM_TITLE";
                sceneDef.subtitleToken = "MAP_RANDOM_SUBTITLE";

                RoR2Application.onLoad += () =>
                {
                    sceneDef.previewTexture = ContentProvider.texScenePreview;
                    sceneDef.portalMaterial = StageRegistration.MakeBazaarSeerMaterial(sceneDef);
                };

                sceneDef.portalSelectionMessageString = "BAZAAR_SEER_RANDOM";
                sceneDef.shouldIncludeInLogbook = false;
                sceneDef.loreToken = null;
                sceneDef.dioramaPrefab = null;

                sceneDef.suppressPlayerEntry = false;
                sceneDef.suppressNpcEntry = false;
                sceneDef.blockOrbitalSkills = false;
                sceneDef.validForRandomSelection = false;
                sceneDef.destinationsGroup = stageSceneCollectionRequest;

                StageRegistration.AddSceneDef(sceneDef, Info);
                StageRegistration.RegisterSceneDefToLoop(sceneDef);
            }

            LanguageAPI.Add("MAP_RANDOM_TITLE", "Random Realm");
            LanguageAPI.Add("MAP_RANDOM_SUBTITLE", "Chaotic Landscape");
            LanguageAPI.Add("BAZAAR_SEER_RANDOM", "<style=cWorldEvent>You dream of dices.</style>");

            Log.Info("SceneDef initialised");
        }

        private void SceneCatalog_onMostRecentSceneDefChanged(SceneDef scene)
        {
            if (scene.cachedName == "random")
            {
                var stages = SceneCatalog.allStageSceneDefs
                    .Where(x => x.cachedName != "random")
                    .ToList();

                scene.mainTrack = stages[Run.instance.stageRng.RangeInt(0, stages.Count)].mainTrack;
                scene.bossTrack = stages[Run.instance.stageRng.RangeInt(0, stages.Count)].bossTrack;

                //todo: randomize tokens
            }
        }

        private void SceneManager_sceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "random")
            {
                InitScene();
            }
        }

        public void InitScene()
        {
            var rng = new Xoroshiro128Plus(Run.instance.stageRng.nextUint);

            int stageInLoop = (Run.instance.stageClearCount % 5) + 1;

            GameObject sceneObject = new GameObject();
            sceneObject.SetActive(false);
            sceneObject.name = "random";
            sceneObject.layer = 11;//World

            MapGenerator generator = sceneObject.AddComponent<MapGenerator>();
            generator.seed = rng.nextInt;
            Log.Info($"seed: {generator.seed}");
            generator.width = 35;
            generator.height = 80;
            generator.depth = 40;
            generator.mapScale = 2.5f;
            generator.cave2d.randomFillPercent = 0.54f;
            generator.cave2d.iterations = 8;
            generator.map2dToMap3d.squareScale = 5;
            generator.carver.frequency = 0.042f;
            generator.carver.verticalScale = 0.8f;
            generator.carver.maxNoise = 0.5f;
            generator.waller.floor.noise = 43.7f;
            generator.waller.floor.maxThickness = 10;
            generator.waller.walls.noise = 18;
            generator.waller.walls.maxThickness = 18;
            generator.waller.ceilling.noise = 33.7f;
            generator.waller.ceilling.maxThickness = 20;
            generator.waller.wallRoundingFactor = 8;
            generator.cave3d.smoothingInterations = 2;
            generator.map3dNoiser.frequency = 0.5f;
            generator.meshColorer.grassAngle = -0.15f;
            generator.meshColorer.baseFrequency = 0.005f;
            generator.meshColorer.frequency = 0.03f;
            generator.meshColorer.amplitude = 0.2f;
            generator.colorPatelette.size = 256;
            generator.colorPatelette.xSquareSize = 1000;
            generator.colorPatelette.ySquareSize = 6;
            generator.colorPatelette.perlinFrequency = 0.588f;
            generator.colorPatelette.floor.saturation = FloorSaturation.Value;
            generator.colorPatelette.floor.value = FloorValue.Value;
            generator.colorPatelette.floor.perlinAmplitude = 0.2f;
            generator.colorPatelette.walls.saturation = WallsSaturation.Value;
            generator.colorPatelette.walls.value = WallsValue.Value;
            generator.colorPatelette.walls.perlinAmplitude = 0.6f;
            generator.colorPatelette.ceilling.saturation = CeillingSaturation.Value;
            generator.colorPatelette.ceilling.value = CeillingValue.Value;
            generator.colorPatelette.ceilling.perlinAmplitude = 0.4f;
            generator.colorPatelette.light.saturation = LightSaturation.Value;
            generator.colorPatelette.light.value = LightValue.Value;
            generator.colorPatelette.minNoise = 0.2f;
            generator.colorPatelette.maxNoise = 0.25f;

            SurfaceDefProvider surfaceProvider = sceneObject.AddComponent<SurfaceDefProvider>();
            surfaceProvider.surfaceDef = ScriptableObject.CreateInstance<SurfaceDef>();
            surfaceProvider.surfaceDef.impactEffectPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/VFX/StoneImpact.prefab").WaitForCompletion();

            MeshCollider collider = sceneObject.AddComponent<MeshCollider>();

            MeshRenderer renderer = sceneObject.AddComponent<MeshRenderer>();
            renderer.material = new Material(Material.GetDefaultMaterial());
            renderer.material.color = new Color(0.8f, 0.8f, 0.8f);
            renderer.material.SetFloat("_Glossiness", 0.2f);
            renderer.material.SetFloat("_Metallic", 0f);
            sceneObject.AddComponent<MeshFilter>();

            SceneInfo sceneInfo = sceneObject.AddComponent<SceneInfo>();

            //Log.Info($"stage: {Stage.instance != null}");

            sceneObject.AddComponent<Stage>();

            List<string> dpMonsters = new List<string>()
            {
                "RoR2/DLC1/ancientloft/dpAncientLoftMonsters.asset",
                "RoR2/Base/blackbeach/dpBlackBeachMonsters.asset",
                "RoR2/Base/dampcave/dpDampCaveMonsters.asset",
                "RoR2/Base/foggyswamp/dpFoggySwampMonsters.asset",
                "RoR2/Base/frozenwall/dpFrozenWallMonsters.asset",
                "RoR2/Base/golemplains/dpGolemplainsMonsters.asset",
                "RoR2/Base/goolake/dpGooLakeMonsters.asset",
                "RoR2/Base/rootjungle/dpRootJungleMonsters.asset",
                "RoR2/Base/shipgraveyard/dpShipgraveyardMonsters.asset",
                "RoR2/Base/skymeadow/dpSkyMeadowMonsters.asset",
                "RoR2/DLC1/snowyforest/dpSnowyForestMonsters.asset",
                "RoR2/DLC1/sulfurpools/dpSulfurPoolsMonsters.asset",
                "RoR2/Base/wispgraveyard/dpWispGraveyardMonsters.asset"
            };

            List<string> dpInteratables = new List<string>()
            {
                "RoR2/DLC1/ancientloft/dpAncientLoftInteractables.asset",
                "RoR2/Base/blackbeach/dpBlackBeachInteractables.asset",
                "RoR2/Base/dampcave/dpDampCaveInteractables.asset",
                "RoR2/Base/foggyswamp/dpFoggySwampInteractables.asset",
                "RoR2/Base/frozenwall/dpFrozenWallInteractables.asset",
                "RoR2/Base/golemplains/dpGolemplainsInteractables.asset",
                "RoR2/Base/goolake/dpGooLakeInteractables.asset",
                "RoR2/Base/rootjungle/dpRootJungleInteractables.asset",
                "RoR2/Base/shipgraveyard/dpShipgraveyardInteractables.asset",
                "RoR2/Base/skymeadow/dpSkyMeadowInteractables.asset",
                "RoR2/DLC1/snowyforest/dpSnowyForestInteractables.asset",
                "RoR2/DLC1/sulfurpools/dpSulfurPoolsInteractables.asset",
                "RoR2/Base/wispgraveyard/dpWispGraveyardInteractables.asset"
            };

            string dpMonster = dpMonsters[rng.RangeInt(0, dpMonsters.Count)];
            string dpInteratable = dpInteratables[rng.RangeInt(0, dpInteratables.Count)];

            Log.Info(dpMonster);
            Log.Info(dpInteratable);

            ClassicStageInfo classicSceneInfo = sceneObject.AddComponent<ClassicStageInfo>();
            classicSceneInfo.monsterDccsPool = Addressables.LoadAssetAsync<DccsPool>(dpMonster).WaitForCompletion();
            classicSceneInfo.interactableDccsPool = Addressables.LoadAssetAsync<DccsPool>(dpInteratable).WaitForCompletion();
            classicSceneInfo.sceneDirectorInteractibleCredits = 75 * (stageInLoop + 2);
            classicSceneInfo.sceneDirectorMonsterCredits = 30 * (stageInLoop + 4);
            classicSceneInfo.bonusInteractibleCreditObjects = new ClassicStageInfo.BonusInteractibleCreditObject[0];
            classicSceneInfo.modifiableMonsterCategories = null;
            classicSceneInfo.interactableCategories = null;
            classicSceneInfo.monsterSelection = null;
            classicSceneInfo.monsterCategories = null;

            DirectorCore director = sceneObject.AddComponent<DirectorCore>();
            SceneDirector sceneDirector = sceneObject.AddComponent<SceneDirector>();

            bool useLunarPortal = stageInLoop == Run.stagesPerLoop;
            string portalPath = useLunarPortal
                ? "RoR2/Base/Teleporters/iscLunarTeleporter.asset"
                : "RoR2/Base/Teleporters/iscTeleporter.asset";

            sceneDirector.teleporterSpawnCard = Addressables.LoadAssetAsync<InteractableSpawnCard>(portalPath).WaitForCompletion();
            sceneDirector.expRewardCoefficient = 0.06666667f;
            sceneDirector.eliteBias = 2;
            sceneDirector.spawnDistanceMultiplier = 6;

            CombatDirector slowCombatDirector = sceneObject.AddComponent<CombatDirector>();
            slowCombatDirector.expRewardCoefficient = 0.2f;
            slowCombatDirector.goldRewardCoefficient = 1;
            slowCombatDirector.minSeriesSpawnInterval = 0.1f;
            slowCombatDirector.maxSeriesSpawnInterval = 1f;
            slowCombatDirector.minRerollSpawnInterval = 22.5f;
            slowCombatDirector.maxRerollSpawnInterval = 30f;
            slowCombatDirector.teamIndex = TeamIndex.Monster;
            slowCombatDirector.creditMultiplier = 0.75f;
            slowCombatDirector.spawnDistanceMultiplier = 1f;
            slowCombatDirector.maxSpawnDistance = float.PositiveInfinity;
            slowCombatDirector.minSpawnRange = 0;
            slowCombatDirector.targetPlayers = true;
            slowCombatDirector.skipSpawnIfTooCheap = true;
            slowCombatDirector.maxConsecutiveCheapSkips = int.MaxValue;
            slowCombatDirector.resetMonsterCardIfFailed = true;
            slowCombatDirector.maximumNumberToSpawnBeforeSkipping = 6;
            slowCombatDirector.eliteBias = 1;
            slowCombatDirector.moneyWaveIntervals = new RangeFloat[]
            {
                new RangeFloat
                {
                    min = 1,
                    max = 1
                }
            };
            slowCombatDirector.onSpawnedServer = new CombatDirector.OnSpawnedServer();
            slowCombatDirector.fallBackToStageMonsterCards = true;

            CombatDirector fastCombatDirector = sceneObject.AddComponent<CombatDirector>();
            fastCombatDirector.expRewardCoefficient = 0.2f;
            fastCombatDirector.goldRewardCoefficient = 1;
            fastCombatDirector.minSeriesSpawnInterval = 0.1f;
            fastCombatDirector.maxSeriesSpawnInterval = 1f;
            fastCombatDirector.minRerollSpawnInterval = 4.5f;
            fastCombatDirector.maxRerollSpawnInterval = 9.0f;
            fastCombatDirector.teamIndex = TeamIndex.Monster;
            fastCombatDirector.creditMultiplier = 0.75f;
            fastCombatDirector.spawnDistanceMultiplier = 1f;
            fastCombatDirector.maxSpawnDistance = float.PositiveInfinity;
            fastCombatDirector.minSpawnRange = 0;
            fastCombatDirector.targetPlayers = true;
            fastCombatDirector.skipSpawnIfTooCheap = true;
            fastCombatDirector.maxConsecutiveCheapSkips = int.MaxValue;
            fastCombatDirector.resetMonsterCardIfFailed = true;
            fastCombatDirector.maximumNumberToSpawnBeforeSkipping = 6;
            fastCombatDirector.eliteBias = 1;
            fastCombatDirector.moneyWaveIntervals = new RangeFloat[]
            {
                new RangeFloat
                {
                    min = 1,
                    max = 1
                }
            };
            fastCombatDirector.onSpawnedServer = new CombatDirector.OnSpawnedServer();
            fastCombatDirector.fallBackToStageMonsterCards = true;

            sceneObject.AddComponent<NetworkIdentity>();
            sceneObject.AddComponent<GlobalEventManager>();
            NewtPlacer newtPlacer = sceneObject.AddComponent<NewtPlacer>();
            newtPlacer.rng = rng;

            sceneObject.SetActive(true);
            Log.Info("Scene loaded!");
        }
    }
}

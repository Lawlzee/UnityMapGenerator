using BepInEx;
using BepInEx.Configuration;
using RiskOfOptions;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using RoR2;
using RoR2.ContentManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace ProceduralStages
{
    [BepInDependency("com.rune580.riskofoptions")]
    [BepInDependency("com.bepis.r2api.language")]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class Main : BaseUnityPlugin
    {
        public const string PluginGUID = "Lawlzee.ProceduralStages";
        public const string PluginAuthor = "Lawlzee";
        public const string PluginName = "ProceduralStages";
        public const string PluginVersion = "1.10.0";

        public void Awake()
        {
            Log.Init(Logger);

            ModConfig.Init(Config);

            var texture = LoadTexture("icon.png");
            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0, 0));
            ModSettingsManager.SetModIcon(sprite);

            ContentManager.collectContentPackProviders += GiveToRoR2OurContentPackProviders;
            On.RoR2.WireMeshBuilder.GenerateMesh_Mesh += WireMeshBuilder_GenerateMesh_Mesh;

            On.RoR2.Run.PickNextStageScene += Run_PickNextStageScene;
            On.RoR2.SceneDirector.DefaultPlayerSpawnPointGenerator += SceneDirector_DefaultPlayerSpawnPointGenerator;
            On.RoR2.SceneDirector.PlacePlayerSpawnsViaNodegraph += SceneDirector_PlacePlayerSpawnsViaNodegraph;

            On.RoR2.SceneExitController.SetState += SceneExitController_SetState;

            On.RoR2.SceneCatalog.FindSceneIndex += SceneCatalog_FindSceneIndex;

            On.RoR2.PreGameController.Awake += PreGameController_Awake;

            On.RoR2.SeerStationController.OnTargetSceneChanged += SeerStationController_OnTargetSceneChanged;
            On.RoR2.SeerStationController.SetRunNextStageToTarget += SeerStationController_SetRunNextStageToTarget;
            On.RoR2.BazaarController.SetUpSeerStations += BazaarController_SetUpSeerStations;

            Stage.onStageStartGlobal += Stage_onStageStartGlobal;

            Run.onRunDestroyGlobal += _ =>
            {
                if (RunConfig.instance != null)
                {
                    Destroy(RunConfig.instance);
                }
            };
        }

        private void BazaarController_SetUpSeerStations(On.RoR2.BazaarController.orig_SetUpSeerStations orig, BazaarController self)
        {
            var scenesSelection = new WeightedSelection<SceneDef>();
            if (Run.instance.nextStageScene != null)
            {
                SceneDef randomStage = null;

                int stageOrder = Run.instance.nextStageScene.stageOrder;
                foreach (SceneDef sceneDef in SceneCatalog.allSceneDefs)
                {
                    if (sceneDef.stageOrder == stageOrder
                        && (sceneDef.requiredExpansion == null || Run.instance.IsExpansionEnabled(sceneDef.requiredExpansion)))
                    {
                        if (sceneDef.cachedName == "random")
                        {
                            randomStage = sceneDef;
                        }
                        else
                        {
                            scenesSelection.AddChoice(sceneDef, 1f);
                        }
                    }
                }

                if (randomStage != null)
                {
                    var totalPercent = RunConfig.instance.terrainTypesPercents
                        .Where(x => x.StageIndex + 1 == stageOrder)
                        .Select(x => x.Percent)
                        .Sum();

                    if (totalPercent > 0)
                    {
                        float randomStageWeight = 1;

                        if (totalPercent >= 1)
                        {
                            scenesSelection.Clear();
                        }
                        else
                        {
                            randomStageWeight = scenesSelection.totalWeight / (1 - totalPercent) - scenesSelection.totalWeight;
                        }

                        scenesSelection.AddChoice(randomStage, randomStageWeight);
                    }
                }
            }

            WeightedSelection<SceneDef> specialStagesSelection = new WeightedSelection<SceneDef>();
            foreach (BazaarController.SeerSceneOverride seerSceneOverride in self.seerSceneOverrides)
            {
                bool hasReachMinStageClearCount = Run.instance.stageClearCount >= seerSceneOverride.minStagesCleared;
                bool hasExpansion = seerSceneOverride.requiredExpasion == null || Run.instance.IsExpansionEnabled(seerSceneOverride.requiredExpasion);
                bool isNotBanned = string.IsNullOrEmpty(seerSceneOverride.bannedEventFlag) || !Run.instance.GetEventFlag(seerSceneOverride.bannedEventFlag);

                if (hasReachMinStageClearCount && hasExpansion && isNotBanned)
                {
                    specialStagesSelection.AddChoice(seerSceneOverride.sceneDef, seerSceneOverride.overrideChance);
                }
            }

            foreach (SeerStationController seerStation in self.seerStations)
            {
                if (self.rng.nextNormalizedFloat < specialStagesSelection.totalWeight)
                {
                    int index = specialStagesSelection.EvaluateToChoiceIndex(self.rng.nextNormalizedFloat);
                    var specialScene = specialStagesSelection.choices[index].value;
                    specialStagesSelection.RemoveChoice(index);
                    seerStation.SetTargetScene(specialScene);
                }
                else if (scenesSelection.Count == 0)
                {
                    seerStation.GetComponent<PurchaseInteraction>().SetAvailable(false);
                }
                else
                {
                    var selectedSceneIndex = scenesSelection.EvaluateToChoiceIndex(self.rng.nextNormalizedFloat);
                    var selectedScene = scenesSelection.choices[selectedSceneIndex].value;
                    scenesSelection.RemoveChoice(selectedSceneIndex);
                    seerStation.SetTargetScene(selectedScene);
                }
            }
        }

        private void Stage_onStageStartGlobal(Stage obj)
        {
            if (NetworkServer.active)
            {
                int nextStageClearCount = Run.instance.stageClearCount;
                if (SceneCatalog.GetSceneDefForCurrentScene().sceneType == SceneType.Stage)
                {
                    nextStageClearCount++;
                }
                RunConfig.instance.nextStageClearCount = nextStageClearCount;
            }
        }

        private void SeerStationController_OnTargetSceneChanged(On.RoR2.SeerStationController.orig_OnTargetSceneChanged orig, SeerStationController self, SceneDef targetScene)
        {
            Material portalMaterial = null;
            if (targetScene)
            {
                if (targetScene.cachedName == "random")
                {
                    int stageIndex = Math.Max(0, Run.instance.stageClearCount) % Run.stagesPerLoop;

                    var typesWeights = RunConfig.instance.terrainTypesPercents
                        .Where(x => x.StageIndex == stageIndex)
                        .ToList();

                    WeightedSelection<TerrainType> selection = new WeightedSelection<TerrainType>(typesWeights.Count);

                    for (int i = 0; i < typesWeights.Count; i++)
                    {
                        var config = typesWeights[i];
                        selection.AddChoice(config.TerrainType, config.Percent);
                    }

                    TerrainType terrainType = selection.totalWeight > 0
                        ? selection.Evaluate(RunConfig.instance.seerRng.nextNormalizedFloat)
                        : TerrainType.OpenCaves;

                    portalMaterial = ContentProvider.SeerMaterialByTerrainType[terrainType];
                }
                else
                {
                    portalMaterial = targetScene.portalMaterial;
                }
            }

            self.SetPortalMaterial(portalMaterial);
        }

        private void SeerStationController_SetRunNextStageToTarget(On.RoR2.SeerStationController.orig_SetRunNextStageToTarget orig, SeerStationController self)
        {
            orig(self);
            var scene = SceneCatalog.GetSceneDef((SceneIndex)self.targetSceneDefIndex);
            if (scene.cachedName == "random")
            {
                self.targetRenderer.GetSharedMaterials(SeerStationController.sharedSharedMaterialsList);
                var material = SeerStationController.sharedSharedMaterialsList[self.materialIndexToAssign];

                var terrainType = ContentProvider.SeerMaterialByTerrainType
                    .Where(x => x.Value == material)
                    .Select(x => x.Key)
                    .First();

                RunConfig.instance.selectedTerrainType = terrainType;
                SeerStationController.sharedSharedMaterialsList.Clear();
            }
        }

        private SceneIndex SceneCatalog_FindSceneIndex(On.RoR2.SceneCatalog.orig_FindSceneIndex orig, string sceneName)
        {
            if (sceneName != "random")
            {
                return orig(sceneName);
            }

            if (Run.instance is InfiniteTowerRun)
            {
                return ContentProvider.ItSceneDef.sceneDefIndex;
            }

            return ContentProvider.LoopSceneDefs[Math.Max(0, Run.instance.stageClearCount) % 5].sceneDefIndex;
        }


        private void PreGameController_Awake(On.RoR2.PreGameController.orig_Awake orig, PreGameController self)
        {
            orig(self);

            if (NetworkServer.active)
            {
                var runConfig = Instantiate(ContentProvider.runConfigPrefab);
                runConfig.GetComponent<RunConfig>().InitHostConfig(self.runSeed);
                NetworkServer.Spawn(runConfig);
            }
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
                //Ugly hack to workaround the caching of R2API.DirectorAPI
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

        private void SceneExitController_SetState(On.RoR2.SceneExitController.orig_SetState orig, SceneExitController self, SceneExitController.ExitState newState)
        {
            if (!NetworkServer.active
                || !Run.instance.name.Contains("Judgement")
                || newState != SceneExitController.ExitState.Finished
                || SceneCatalog.currentSceneDef.cachedName != "bazaar"
                || Run.instance.stageClearCount >= 10)
            {
                orig(self, newState);
                return;
            }

            int stageIndex = (Run.instance.stageClearCount / 2) % Run.stagesPerLoop;

            float totalPercent = RunConfig.instance.terrainTypesPercents
                .Where(x => x.StageIndex == stageIndex)
                .Select(x => x.Percent)
                .Sum();

            if (Run.instance.stageRngGenerator.nextNormalizedFloat <= totalPercent)
            {
                Run.instance.nextStageScene = ContentProvider.ItSceneDef;
            }

            orig(self, newState);
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
            if (Run.instance.stageClearCount == 0 && self.name.Contains("Judgement"))
            {
                orig(self, choices);
                return;
            }

            int stageIndex = (Math.Max(0, Run.instance.stageClearCount) + 1) % Run.stagesPerLoop;

            float totalPercent = RunConfig.instance.terrainTypesPercents
                .Where(x => x.StageIndex == stageIndex)
                .Select(x => x.Percent)
                .Sum();

            SceneDef sceneDef = self is InfiniteTowerRun
                ? ContentProvider.ItSceneDef
                : ContentProvider.LoopSceneDefs[stageIndex];

            if (totalPercent >= 1)
            {
                self.nextStageScene = sceneDef;
            }
            else
            {
                float weight = choices.totalWeight / (1 - totalPercent) - choices.totalWeight;
                choices.AddChoice(sceneDef, weight);
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
            addContentPackProvider(new ContentProvider());
        }
    }
}
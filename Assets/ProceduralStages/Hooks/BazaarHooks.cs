﻿using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace ProceduralStages
{
    public static class BazaarHooks
    {
        public static void Init()
        {
            On.RoR2.SeerStationController.OnTargetSceneChanged += SeerStationController_OnTargetSceneChanged;
            On.RoR2.SeerStationController.SetRunNextStageToTarget += SeerStationController_SetRunNextStageToTarget;
            On.RoR2.BazaarController.SetUpSeerStations += BazaarController_SetUpSeerStations;
        }

        private static void BazaarController_SetUpSeerStations(On.RoR2.BazaarController.orig_SetUpSeerStations orig, BazaarController self)
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
                        if (sceneDef.cachedName == Main.SceneName)
                        {
                            randomStage = sceneDef;
                        }
                        else
                        {
                            scenesSelection.AddChoice(sceneDef, 1f);
                        }
                    }
                }

                if (randomStage != null && RunConfig.instance.minStageCount <= Run.instance.stageClearCount + 1)
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



        private static void SeerStationController_OnTargetSceneChanged(On.RoR2.SeerStationController.orig_OnTargetSceneChanged orig, SeerStationController self, SceneDef targetScene)
        {
            Material portalMaterial = null;
            if (targetScene)
            {
                if (targetScene.cachedName == Main.SceneName)
                {
                    int stageIndex = Math.Max(0, Run.instance.stageClearCount) % Run.stagesPerLoop;

                    var typesWeights = RunConfig.instance.terrainTypesPercents
                        .Where(x => x.StageIndex == stageIndex)
                        .ToList();

                    WeightedSelection<TerrainType> filteredSelection = new WeightedSelection<TerrainType>(typesWeights.Count);
                    WeightedSelection<TerrainType> allSelection = new WeightedSelection<TerrainType>(typesWeights.Count);

                    int loopIndex = (stageIndex - 1) / Run.stagesPerLoop;
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

                    TerrainType terrainType = filteredSelection.totalWeight > 0
                        ? filteredSelection.Evaluate(RunConfig.instance.seerRng.nextNormalizedFloat)
                        : allSelection.totalWeight > 0
                            ? allSelection.Evaluate(RunConfig.instance.seerRng.nextNormalizedFloat)
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


        private static void SeerStationController_SetRunNextStageToTarget(On.RoR2.SeerStationController.orig_SetRunNextStageToTarget orig, SeerStationController self)
        {
            if (!NetworkServer.active)
            {
                Debug.LogWarning("[Server] function 'System.Void RoR2.SeerStationController::SetRunNextStageToTarget()' called on client");
                return;
            }

            SceneDef sceneDef = SceneCatalog.GetSceneDef((SceneIndex)self.targetSceneDefIndex);
            if (!sceneDef)
            {
                return;
            }

            SceneExitController sceneExitController = self.explicitTargetSceneExitController;
            if (!sceneExitController && self.fallBackToFirstActiveExitController)
            {
                sceneExitController = InstanceTracker.FirstOrNull<SceneExitController>();
            }

            if (!sceneExitController)
            {
                return;
            }

            sceneExitController.destinationScene = sceneDef;
            sceneExitController.useRunNextStageScene = false;

            if (sceneDef.cachedName == Main.SceneName)
            {
                self.targetRenderer.GetSharedMaterials(SeerStationController.sharedSharedMaterialsList);
                var material = SeerStationController.sharedSharedMaterialsList[self.materialIndexToAssign];

                var terrainType = ContentProvider.SeerMaterialByTerrainType
                    .Where(x => x.Value == material)
                    .Select(x => x.Key)
                    .First();

                RunConfig.instance.selectedTerrainType = terrainType;
                SeerStationController.sharedSharedMaterialsList.Clear();

                sceneDef.portalSelectionMessageString = terrainType.GetDreamMessage();
            }

            Chat.SendBroadcastChat(new Chat.SimpleChatMessage()
            {
                baseToken = sceneDef.portalSelectionMessageString
            });
        }
    }
}

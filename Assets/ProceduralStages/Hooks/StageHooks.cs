using RoR2;
using RoR2.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering;

namespace ProceduralStages
{
    public static class StageHooks
    {
        public static void Init()
        {
            On.RoR2.Run.PickNextStageScene += Run_PickNextStageScene;
            On.RoR2.SceneDirector.DefaultPlayerSpawnPointGenerator += SceneDirector_DefaultPlayerSpawnPointGenerator;
            On.RoR2.SceneDirector.PlacePlayerSpawnsViaNodegraph += SceneDirector_PlacePlayerSpawnsViaNodegraph;
            On.RoR2.SceneExitController.SetState += SceneExitController_SetState;
            On.RoR2.SceneCatalog.FindSceneIndex += SceneCatalog_FindSceneIndex;
            On.RoR2.Run.FindSafeTeleportPosition_CharacterBody_Transform_float_float += Run_FindSafeTeleportPosition_CharacterBody_Transform_float_float;
            On.RoR2.DirectorSpawnRequest.ctor += DirectorSpawnRequest_ctor;
            On.RoR2.WireMeshBuilder.GenerateMesh_Mesh += WireMeshBuilder_GenerateMesh_Mesh;
        }

        public static void ClassicStageInfo_Start(On.RoR2.ClassicStageInfo.orig_Start orig, ClassicStageInfo self)
        {
            if (SceneCatalog.currentSceneDef.cachedName == Main.SceneName)
            {
                //Ugly hack to workaround the caching of R2API.DirectorAPI
                SceneCatalog.currentSceneDef.cachedName = Guid.NewGuid().ToString();
                try
                {
                    orig(self);
                }
                finally
                {
                    SceneCatalog.currentSceneDef.cachedName = Main.SceneName;
                }

                return;
            }

            orig(self);
        }

        private static SceneIndex SceneCatalog_FindSceneIndex(On.RoR2.SceneCatalog.orig_FindSceneIndex orig, string sceneName)
        {
            if (sceneName != Main.SceneName)
            {
                return orig(sceneName);
            }

            if (Run.instance is InfiniteTowerRun)
            {
                return ContentProvider.ItSceneDef.sceneDefIndex;
            }

            return ContentProvider.LoopSceneDefs[Math.Max(0, Run.instance.stageClearCount) % Run.stagesPerLoop].sceneDefIndex;
        }

        private static void SceneDirector_PlacePlayerSpawnsViaNodegraph(On.RoR2.SceneDirector.orig_PlacePlayerSpawnsViaNodegraph orig, SceneDirector self)
        {
            if (Run.instance.spawnWithPod && SceneCatalog.currentSceneDef.cachedName == Main.SceneName && MapGenerator.instance.stageType != StageType.Moon)
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

        private static void SceneDirector_DefaultPlayerSpawnPointGenerator(On.RoR2.SceneDirector.orig_DefaultPlayerSpawnPointGenerator orig, SceneDirector self)
        {
            if (Run.instance.spawnWithPod && SceneCatalog.currentSceneDef.cachedName == Main.SceneName)
            {
                self.RemoveAllExistingSpawnPoints();
                self.PlacePlayerSpawnsViaNodegraph();
                return;
            }

            orig(self);
        }

        private static void Run_PickNextStageScene(On.RoR2.Run.orig_PickNextStageScene orig, Run self, WeightedSelection<SceneDef> choices)
        {
            int nextStageIndex = RunConfig.instance.nextStageClearCount + 1;

            if ((Run.instance.stageClearCount == 0 && self.name.Contains(Main.Judgement))
                || nextStageIndex < RunConfig.instance.minStageCount)
            {
                orig(self, choices);
                return;
            }

            int stageIndexInLoop = nextStageIndex % Run.stagesPerLoop;

            float totalPercent = RunConfig.instance.terrainTypesPercents
                .Where(x => x.StageIndex == stageIndexInLoop)
                .Select(x => x.Percent)
                .Sum();

            SceneDef sceneDef = self is InfiniteTowerRun
                ? ContentProvider.ItSceneDef
                : ContentProvider.LoopSceneDefs[stageIndexInLoop];

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

        private static void SceneExitController_SetState(On.RoR2.SceneExitController.orig_SetState orig, SceneExitController self, SceneExitController.ExitState newState)
        {
            if (NetworkServer.active && newState == SceneExitController.ExitState.Finished)
            {
                if (Run.instance.name.Contains(Main.Judgement)
                    && SceneCatalog.currentSceneDef.cachedName == "bazaar"
                    && Run.instance.stageClearCount < 11)
                {
                    int stageIndex = (Run.instance.stageClearCount / 2) % Run.stagesPerLoop;

                    if (stageIndex < RunConfig.instance.minStageCount)
                    {
                        orig(self, newState);
                        return;
                    }

                    var typesWeights = RunConfig.instance.terrainTypesPercents
                        .Where(x => x.StageIndex == stageIndex)
                        .ToList();

                    float totalPercent = typesWeights
                        .Select(x => x.Percent)
                        .Sum();

                    if (Run.instance.stageRngGenerator.nextNormalizedFloat <= totalPercent)
                    {
                        Run.instance.nextStageScene = ContentProvider.ItSceneDef;

                        int loopIndex = (stageIndex - 1) / Run.stagesPerLoop;
                        TerrainType[] terrainTypesVisitedInLoop = RunConfig.instance.terrainTypeVisits
                            .Where(x => (x.stageCount - 1) / Run.stagesPerLoop == loopIndex)
                            .Select(x => x.terrainType)
                            .ToArray();

                        WeightedSelection<TerrainType> filteredSelection = new WeightedSelection<TerrainType>(typesWeights.Count);
                        WeightedSelection<TerrainType> allSelection = new WeightedSelection<TerrainType>(typesWeights.Count);

                        for (int i = 0; i < typesWeights.Count; i++)
                        {
                            var config = typesWeights[i];
                            if (RunConfig.instance.terrainRepetition == TerrainRepetition.NonePerLoop && !terrainTypesVisitedInLoop.Contains(config.TerrainType))
                            {
                                filteredSelection.AddChoice(config.TerrainType, config.Percent);
                            }
                            allSelection.AddChoice(config.TerrainType, config.Percent);
                        }

                        RunConfig.instance.selectedTerrainType = filteredSelection.totalWeight > 0
                            ? filteredSelection.Evaluate(Run.instance.nextStageRng.nextNormalizedFloat)
                            : allSelection.totalWeight > 0
                                ? allSelection.Evaluate(Run.instance.nextStageRng.nextNormalizedFloat)
                                : TerrainType.Random;
                    }
                }
                else if (self.gameObject.name == "LunarTeleporter Variant(Clone)" 
                    && !self.useRunNextStageScene
                    && Run.instance.nextStageRng.nextNormalizedFloat <= ModConfig.MoonSpawnRate.Value)
                {
                    RunConfig.instance.selectedTerrainType = TerrainType.Moon;
                    self.destinationScene = ContentProvider.LoopSceneDefs[0];
                }
            }

            orig(self, newState);
        }

        private static bool _inFindSafeTeleportPosition;

        private static Vector3 Run_FindSafeTeleportPosition_CharacterBody_Transform_float_float(On.RoR2.Run.orig_FindSafeTeleportPosition_CharacterBody_Transform_float_float orig, Run self, CharacterBody characterBody, Transform targetDestination, float idealMinDistance, float idealMaxDistance)
        {
            try
            {
                _inFindSafeTeleportPosition = SceneCatalog.currentSceneDef.cachedName == Main.SceneName;
                return orig(self, characterBody, targetDestination, idealMinDistance, idealMaxDistance);
            }
            finally
            {
                _inFindSafeTeleportPosition = false;
            }
        }

        private static void DirectorSpawnRequest_ctor(On.RoR2.DirectorSpawnRequest.orig_ctor orig, DirectorSpawnRequest self, SpawnCard spawnCard, DirectorPlacementRule placementRule, Xoroshiro128Plus rng)
        {
            orig(self, spawnCard, placementRule, rng);
            if (_inFindSafeTeleportPosition)
            {
                spawnCard.forbiddenFlags |= NodeFlags.NoCharacterSpawn;
            }
        }

        private static void WireMeshBuilder_GenerateMesh_Mesh(On.RoR2.WireMeshBuilder.orig_GenerateMesh_Mesh orig, WireMeshBuilder self, Mesh dest)
        {
            dest.indexFormat = IndexFormat.UInt32;
            orig(self, dest);
        }
    }
}

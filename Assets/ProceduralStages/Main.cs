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
        public const string PluginVersion = "1.7.2";

        public static ConfigEntry<bool> ReplaceAllStages;

        //public static ConfigEntry<string> Seed;
        //public static ConfigEntry<float> FloorSaturation;
        //public static ConfigEntry<float> FloorValue;
        //public static ConfigEntry<float> WallsSaturation;
        //public static ConfigEntry<float> WallsValue;
        //public static ConfigEntry<float> CeillingSaturation;
        //public static ConfigEntry<float> CeillingValue;
        //public static ConfigEntry<float> LightSaturation;
        //public static ConfigEntry<float> LightValue;
        //
        //public static ConfigEntry<float> FogSaturation;
        //public static ConfigEntry<float> FogValue;
        //public static ConfigEntry<float> FogColorStartAlpha;
        //public static ConfigEntry<float> FogColorMidAlpha;
        //public static ConfigEntry<float> FogColorEndAlpha;
        //public static ConfigEntry<float> FogZero;
        //public static ConfigEntry<float> FogOne;
        //public static ConfigEntry<float> FogIntensity;
        //public static ConfigEntry<float> FogPower;

        public void Awake()
        {
            Log.Init(Logger);

            ReplaceAllStages = Config.Bind("Configuration", "Replace all stages", true, "If enabled, all the stages will be procedurally generated. If disabled, normal stages and procedurally generated stages will be used.");
            ModSettingsManager.AddOption(new CheckBoxOption(ReplaceAllStages));

            //string debugDescrption = "This configuration is intended for debugging the map generation. Please refrain from making any changes unless you know what you are doing.";

            //Seed = Config.Bind("Debug", nameof(Seed), "", debugDescrption);
            //FloorSaturation = Config.Bind("Debug", nameof(FloorSaturation), 0.5f, debugDescrption);
            //FloorValue = Config.Bind("Debug", nameof(FloorValue), 0.36f, debugDescrption);
            //WallsSaturation = Config.Bind("Debug", nameof(WallsSaturation), 0.3f, debugDescrption);
            //WallsValue = Config.Bind("Debug", nameof(WallsValue), 0.27f, debugDescrption);
            //CeillingSaturation = Config.Bind("Debug", nameof(CeillingSaturation), 0.3f, debugDescrption);
            //CeillingValue = Config.Bind("Debug", nameof(CeillingValue), 0.15f, debugDescrption);
            //LightSaturation = Config.Bind("Debug", nameof(LightSaturation), 0.7f, debugDescrption);
            //LightValue = Config.Bind("Debug", nameof(LightValue), 0.7f, debugDescrption);
            //
            //FogSaturation = Config.Bind("Debug", nameof(FogSaturation), 0.7f, debugDescrption);
            //FogValue = Config.Bind("Debug", nameof(FogValue), 0.7f, debugDescrption);
            //FogColorStartAlpha = Config.Bind("Debug", nameof(FogColorStartAlpha), 0f, debugDescrption);
            //FogColorMidAlpha = Config.Bind("Debug", nameof(FogColorMidAlpha), 0.175f, debugDescrption);
            //FogColorEndAlpha = Config.Bind("Debug", nameof(FogColorEndAlpha), 0.35f, debugDescrption);
            //FogZero = Config.Bind("Debug", nameof(FogZero), 0f, debugDescrption);
            //FogOne = Config.Bind("Debug", nameof(FogOne), 0.1f, debugDescrption);
            //FogIntensity = Config.Bind("Debug", nameof(FogIntensity), 0.25f, debugDescrption);
            //FogPower = Config.Bind("Debug", nameof(FogPower), 0.75f, debugDescrption);
            //
            //ModSettingsManager.AddOption(new StringInputFieldOption(Seed));
            //ModSettingsManager.AddOption(new SliderOption(FloorSaturation, new SliderConfig { min = 0, max = 1, formatString = "{0:0.00}" }));
            //ModSettingsManager.AddOption(new SliderOption(FloorValue, new SliderConfig { min = 0, max = 1, formatString = "{0:0.00}" }));
            //ModSettingsManager.AddOption(new SliderOption(WallsSaturation, new SliderConfig { min = 0, max = 1, formatString = "{0:0.00}" }));
            //ModSettingsManager.AddOption(new SliderOption(WallsValue, new SliderConfig { min = 0, max = 1, formatString = "{0:0.00}" }));
            //ModSettingsManager.AddOption(new SliderOption(CeillingSaturation, new SliderConfig { min = 0, max = 1, formatString = "{0:0.00}" }));
            //ModSettingsManager.AddOption(new SliderOption(CeillingValue, new SliderConfig { min = 0, max = 1, formatString = "{0:0.00}" }));
            //ModSettingsManager.AddOption(new SliderOption(LightSaturation, new SliderConfig { min = 0, max = 1, formatString = "{0:0.00}" }));
            //ModSettingsManager.AddOption(new SliderOption(LightValue, new SliderConfig { min = 0, max = 1, formatString = "{0:0.00}" }));
            //
            //ModSettingsManager.AddOption(new SliderOption(FogSaturation, new SliderConfig { min = 0, max = 1, formatString = "{0:0.00}" }));
            //ModSettingsManager.AddOption(new SliderOption(FogValue, new SliderConfig { min = 0, max = 1, formatString = "{0:0.00}" }));
            //ModSettingsManager.AddOption(new SliderOption(FogColorStartAlpha, new SliderConfig { min = 0, max = 1, formatString = "{0:0.00}" }));
            //ModSettingsManager.AddOption(new SliderOption(FogColorMidAlpha, new SliderConfig { min = 0, max = 1, formatString = "{0:0.00}" }));
            //ModSettingsManager.AddOption(new SliderOption(FogColorEndAlpha, new SliderConfig { min = 0, max = 1, formatString = "{0:0.00}" }));
            //ModSettingsManager.AddOption(new SliderOption(FogZero, new SliderConfig { min = 0, max = 1, formatString = "{0:0.00}" }));
            //ModSettingsManager.AddOption(new SliderOption(FogOne, new SliderConfig { min = 0, max = 1, formatString = "{0:0.00}" }));
            //ModSettingsManager.AddOption(new SliderOption(FogIntensity, new SliderConfig { min = 0, max = 1, formatString = "{0:0.00}" }));
            //ModSettingsManager.AddOption(new SliderOption(FogPower, new SliderConfig { min = 0, max = 1, formatString = "{0:0.00}" }));

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
                var seedSyncerObject = Instantiate(ContentProvider.seedSyncerPrefab);
                seedSyncerObject.GetComponent<SeedSyncer>().seed = self.runSeed;
                NetworkServer.Spawn(seedSyncerObject);
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
                || Run.instance.stageClearCount >= 11)
            {
                orig(self, newState);
                return;
            }

            if (ReplaceAllStages.Value || Run.instance.stageRngGenerator.nextNormalizedFloat > 0.5f)
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

            SceneDef sceneDef = self is InfiniteTowerRun
                ? ContentProvider.ItSceneDef
                : ContentProvider.LoopSceneDefs[(Math.Max(0, Run.instance.stageClearCount) + 1) % 5];

            if (ReplaceAllStages.Value)
            {
                self.nextStageScene = sceneDef;
            }
            else
            {
                choices.AddChoice(sceneDef, 1f);
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




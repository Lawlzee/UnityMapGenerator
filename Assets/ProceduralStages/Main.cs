﻿using BepInEx;
using RiskOfOptions;
using RoR2;
using RoR2.ContentManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace ProceduralStages
{
    [BepInDependency("RiskOfResources.PublicRoRGauntlet", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.rune580.riskofoptions")]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class Main : BaseUnityPlugin
    {
        public const string PluginGUID = "Lawlzee.ProceduralStages";
        public const string PluginAuthor = "Lawlzee";
        public const string PluginName = "ProceduralStages";
        public const string PluginVersion = "2.0.2";

        public static string SceneName = "random";
        public static string Judgement = "Judgement";

        public void Awake()
        {
            Log.Init(Logger);

            var texture = LoadTexture("icon.png");
            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0, 0));
            ModSettingsManager.SetModIcon(sprite);
            ModSettingsManager.SetModDescription("Adds procedurally generated stages and custom themes to vanilla stages. Discover procedurally generated stages featuring 8 distinct terrain types alongside 5 captivating themes to explore.");

            PauseMenu.Init();

            BazaarHooks.Init();
            ConfigHooks.Init();
            StageHooks.Init();

            ContentManager.collectContentPackProviders += GiveToRoR2OurContentPackProviders;
            RoR2Application.onLoadFinished += () =>
            {
                ModConfig.Init(Config);

                ContentProvider.mapThemeCollection.WarmUp();
                SceneLoader.Init(new GameObject[] { ContentProvider.themeGeneratorPrefab });
            };
        }

        public void Start()
        {
            //This should be after DirectorPlugin.Awake();
            On.RoR2.ClassicStageInfo.Start += StageHooks.ClassicStageInfo_Start;
        }

        private Texture2D LoadTexture(string name)
        {
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(File.ReadAllBytes(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location), name)));
            return texture;
        }

        private void GiveToRoR2OurContentPackProviders(ContentManager.AddContentPackProviderDelegate addContentPackProvider)
        {
            addContentPackProvider(new ContentProvider());
        }
    }
}
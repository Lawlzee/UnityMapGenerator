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

namespace ProceduralStages
{
    [BepInDependency("com.rune580.riskofoptions")]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class ProceduralStagesPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "Lawlzee.ProceduralStages";
        public const string PluginAuthor = "Lawlzee";
        public const string PluginName = "ProceduralStages";
        public const string PluginVersion = "1.0.0";

        public static ConfigEntry<bool> ModEnabled;
        public static ConfigEntry<float> ItemLuck;

        public void Awake()
        {
            Log.Init(Logger);



            ModEnabled = Config.Bind("Configuration", "Mod enabled", true, "Mod enabled");
            ModSettingsManager.AddOption(new CheckBoxOption(ModEnabled));


            On.RoR2.RoR2Application.LoadGameContent += RoR2Application_LoadGameContent;

            //On.RoR2.SceneDirector.PlaceTeleporter += SceneDirector_PlaceTeleporter;

            var texture = LoadTexture("icon.png");
            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0, 0));
            ModSettingsManager.SetModIcon(sprite);

            ContentManager.collectContentPackProviders += GiveToRoR2OurContentPackProviders;
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;

            On.RoR2.SceneDirector.Start += SceneDirector_Start;
        }

        private void SceneDirector_Start(On.RoR2.SceneDirector.orig_Start orig, SceneDirector self)
        {
            if (Run.instance == null)
            {
                Log.Info("Run.instance == null");
            }

            if (Run.instance?.stageRng == null)
            {
                Log.Info("Run.instance.stageRng == null");
            }

            if (SceneInfo.instance == null)
            {
                Log.Info("SceneInfo.instance == null");
            }
            else
            {
                ClassicStageInfo component = SceneInfo.instance.GetComponent<ClassicStageInfo>();
                if (component == null)
                {
                    Log.Info("component == null");
                }
            }



            orig(self);
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
            MusicTrackDef mainTrack = Addressables.LoadAssetAsync<MusicTrackDef>("RoR2/Base/Common/muSong13.asset").WaitForCompletion();
            MusicTrackDef bossTrack = Addressables.LoadAssetAsync<MusicTrackDef>("RoR2/Base/Common/muSong05.asset").WaitForCompletion();
            SceneCollection stageSceneCollectionRequest = Addressables.LoadAssetAsync<SceneCollection>("RoR2/Base/SceneGroups/sgStage1.asset").WaitForCompletion();

            string stageName = "random";

            SceneDef sceneDef = ScriptableObject.CreateInstance<SceneDef>();
            sceneDef.cachedName = stageName;
            sceneDef.sceneType = SceneType.Stage;
            sceneDef.isOfflineScene = false;
            sceneDef.stageOrder = 2;
            sceneDef.nameToken = "MAP_DAMPCAVE_TITLE";
            sceneDef.subtitleToken = "MAP_DAMPCAVE_TITLE";
            sceneDef.previewTexture = null;
            sceneDef.portalMaterial = Material.GetDefaultMaterial();
            sceneDef.portalSelectionMessageString = "BAZAAR_SEER_DAMPCAVESIMPLE";
            sceneDef.shouldIncludeInLogbook = false;
            sceneDef.loreToken = "MAP_DAMPCAVE_LORE";
            sceneDef.dioramaPrefab = null;
            sceneDef.mainTrack = mainTrack;
            sceneDef.bossTrack = bossTrack;
            sceneDef.suppressPlayerEntry = false;
            sceneDef.suppressNpcEntry = false;
            sceneDef.blockOrbitalSkills = false;
            sceneDef.validForRandomSelection = false;
            sceneDef.destinationsGroup = stageSceneCollectionRequest;

            StageRegistration.AddSceneDef(sceneDef, Info);
            StageRegistration.RegisterSceneDefToLoop(sceneDef);

            Log.Info("SceneDef inited");
        }

        private void SceneManager_sceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "random")
            {
                InitScene(scene);
            }
        }

        public void InitScene(Scene scene)
        {
            int i = 0;

            Log.Info(i++);


            Log.Info(scene.IsValid().ToString());

            Log.Info(i++);

            //RenderSettings.skybox = Material.GetDefaultMaterial();

            GameObject sceneObject = new GameObject();
            sceneObject.SetActive(false);
            sceneObject.layer = 11;
            MapGenerator generator = sceneObject.AddComponent<MapGenerator>();
            generator.width = 60;
            generator.height = 80;
            generator.depth = 50;
            generator.mapScale = 1;
            generator.cave2d.randomFillPercent = 0.47f;
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
            generator.meshColorer.baseFrequency = 0.1f;
            generator.meshColorer.frequency = 0.7f;
            generator.meshColorer.amplitude = 0.3f;
            generator.colorPatelette.size = 256;
            generator.colorPatelette.transitionSize = 50;
            generator.colorPatelette.floor.saturation = 0.836f;
            generator.colorPatelette.floor.value = 0.659f;
            generator.colorPatelette.walls.saturation = 0.795f;
            generator.colorPatelette.walls.value = 0.372f;
            generator.colorPatelette.ceilling.saturation = 0.285f;
            generator.colorPatelette.ceilling.value = 0.062f;
            generator.colorPatelette.noise = 0.02f;

            Log.Info(i++);

            MeshCollider collider = sceneObject.AddComponent<MeshCollider>();
            for (int j = 0; j < 31; j++)
            {
                string name = LayerMask.LayerToName(j);
                Log.Info(j);
                Log.Info(name ?? "null");
            }

            Log.Info(i++);
            MeshRenderer renderer = sceneObject.AddComponent<MeshRenderer>();
            Log.Info(i++);
            renderer.material = Material.GetDefaultMaterial();
            Log.Info(i++);
            sceneObject.AddComponent<MeshFilter>();
            Log.Info(i++);

            SceneInfo sceneInfo = sceneObject.AddComponent<SceneInfo>();

            Stage stage = sceneObject.AddComponent<Stage>();

            Log.Info(i++);
            //sceneInfo.groundNodeGroup = new MapNodeGroup();
            //sceneInfo.airNodeGroup = new MapNodeGroup();
            //sceneInfo.groundNodesAsset = ScriptableObject.CreateInstance<NodeGraph>();
            //sceneInfo.airNodesAsset = ScriptableObject.CreateInstance<NodeGraph>();
            ClassicStageInfo classicSceneInfo = sceneObject.AddComponent<ClassicStageInfo>();
            classicSceneInfo.monsterDccsPool = Addressables.LoadAssetAsync<DccsPool>("RoR2/Base/rootjungle/dpRootJungleMonsters.asset").WaitForCompletion();
            classicSceneInfo.interactableDccsPool = Addressables.LoadAssetAsync<DccsPool>("RoR2/Base/rootjungle/dpRootJungleInteractables.asset").WaitForCompletion();
            classicSceneInfo.sceneDirectorInteractibleCredits = 200;
            classicSceneInfo.sceneDirectorMonsterCredits = 20;
            classicSceneInfo.bonusInteractibleCreditObjects = new ClassicStageInfo.BonusInteractibleCreditObject[0];

            Log.Info(i++);

            DirectorCore director = sceneObject.AddComponent<DirectorCore>();
            Log.Info(i++);
            SceneDirector sceneDirector = sceneObject.AddComponent<SceneDirector>();
            sceneDirector.teleporterSpawnCard = Addressables.LoadAssetAsync<InteractableSpawnCard>("RoR2/Base/Teleporters/iscTeleporter.asset").WaitForCompletion();
            sceneDirector.expRewardCoefficient = 0.066666f;
            sceneDirector.eliteBias = 2;
            sceneDirector.spawnDistanceMultiplier = 6;


            CombatDirector combatDirector = sceneObject.AddComponent<CombatDirector>();
            combatDirector.expRewardCoefficient = 0.2f;
            combatDirector.goldRewardCoefficient = 1;
            combatDirector.minSeriesSpawnInterval = 0.1f;
            combatDirector.maxSeriesSpawnInterval = 1f;
            combatDirector.minRerollSpawnInterval = 4.5f;
            combatDirector.maxRerollSpawnInterval = 9.0f;
            combatDirector.teamIndex = TeamIndex.Monster;
            combatDirector.creditMultiplier = 0.75f;
            combatDirector.spawnDistanceMultiplier = 1f;
            combatDirector.maxSpawnDistance = float.PositiveInfinity;
            combatDirector.minSpawnRange = 0;
            combatDirector.targetPlayers = true;
            combatDirector.skipSpawnIfTooCheap = true;
            combatDirector.maxConsecutiveCheapSkips = int.MaxValue;
            combatDirector.resetMonsterCardIfFailed = true;
            combatDirector.maximumNumberToSpawnBeforeSkipping = 6;
            combatDirector.eliteBias = 1;
            combatDirector.moneyWaveIntervals = new RangeFloat[0];
            combatDirector.onSpawnedServer = new CombatDirector.OnSpawnedServer();

            generator.onGenerated += (MeshResult meshResult) =>
            {
                var groundNodes = ScriptableObject.CreateInstance<NodeGraph>();

                Log.Info("A");
                
                Log.Info("A");

                var triangles = meshResult.triangles;
                Log.Info("A");
                var vertices = meshResult.vertices;
                Log.Info("A");
                var normals = meshResult.normals;
                Log.Info("A");

                var nodes = new NodeGraph.Node[vertices.Count];
                Log.Info("A");

                Log.Info("1");

                int index = 0;
                for (int i = 0; i < vertices.Count; i++)
                {
                    //Log.Info("2");
                    var vertex = vertices[i];
                    var normal = normals[i];
                    //Log.Info("3");

                    bool valid = Vector3.Dot(Vector3.up, normal) > 0.4f;


                    var node = new NodeGraph.Node
                    {
                        position = vertex,
                        linkListIndex = new NodeGraph.LinkListIndex()
                        {
                            index = valid ? index : - 1,
                            size = 0
                        },
                        forbiddenHulls = HullMask.None,
                        gateIndex = 0,
                        flags = NodeFlags.TeleporterOK
                    };

                    if (valid)
                    {
                        index++;
                    }

                    nodes[i] = node;

                }
                Log.Info("A");

                List<NodeGraph.Link>[] links = new List<NodeGraph.Link>[index];

                for (int i = 0; i < index; i++)
                {
                    links[i] = new List<NodeGraph.Link>();
                }

                for (int i = 0; i < triangles.Count; i += 3)
                {
                    ref var node1 = ref nodes[triangles[i]];
                    ref var node2 = ref nodes[triangles[i + 1]];
                    ref var node3 = ref nodes[triangles[i + 2]];

                    if (node1.linkListIndex.index != -1)
                    {
                        if (node2.linkListIndex.index != -1)
                        {
                            //node1.linkListIndex.size++;

                            links[node1.linkListIndex.index].Add(new NodeGraph.Link
                            {
                                nodeIndexA = new NodeGraph.NodeIndex(node1.linkListIndex.index),
                                nodeIndexB = new NodeGraph.NodeIndex(node2.linkListIndex.index),
                                distanceScore = 1,
                                minJumpHeight = 0,
                                hullMask = 31,
                                jumpHullMask = 31,
                                gateIndex = 0
                            });

                            //links[node2.linkListIndex.index].Add(new NodeGraph.Link
                            //{
                            //    nodeIndexA = new NodeGraph.NodeIndex(node2.linkListIndex.index),
                            //    nodeIndexB = new NodeGraph.NodeIndex(node1.linkListIndex.index),
                            //    distanceScore = 1,
                            //    minJumpHeight = 0,
                            //    hullMask = 31,
                            //    jumpHullMask = 31,
                            //    gateIndex = 0
                            //});
                        }

                        if (node3.linkListIndex.index != -1)
                        {
                            links[node1.linkListIndex.index].Add(new NodeGraph.Link
                            {
                                nodeIndexA = new NodeGraph.NodeIndex(node1.linkListIndex.index),
                                nodeIndexB = new NodeGraph.NodeIndex(node3.linkListIndex.index),
                                distanceScore = 1,
                                minJumpHeight = 0,
                                hullMask = 31,
                                jumpHullMask = 31,
                                gateIndex = 0
                            });

                            //links[node3.linkListIndex.index].Add(new NodeGraph.Link
                            //{
                            //    nodeIndexA = new NodeGraph.NodeIndex(node3.linkListIndex.index),
                            //    nodeIndexB = new NodeGraph.NodeIndex(node1.linkListIndex.index),
                            //    distanceScore = 1,
                            //    minJumpHeight = 0,
                            //    hullMask = 31,
                            //    jumpHullMask = 31,
                            //    gateIndex = 0
                            //});
                        }
                    }

                    if (node2.linkListIndex.index != -1 && node3.linkListIndex.index != -1)
                    {
                        links[node2.linkListIndex.index].Add(new NodeGraph.Link
                        {
                            nodeIndexA = new NodeGraph.NodeIndex(node2.linkListIndex.index),
                            nodeIndexB = new NodeGraph.NodeIndex(node3.linkListIndex.index),
                            distanceScore = 1,
                            minJumpHeight = 0,
                            hullMask = 31,
                            jumpHullMask = 31,
                            gateIndex = 0
                        });

                        //links[node3.linkListIndex.index].Add(new NodeGraph.Link
                        //{
                        //    nodeIndexA = new NodeGraph.NodeIndex(node3.linkListIndex.index),
                        //    nodeIndexB = new NodeGraph.NodeIndex(node2.linkListIndex.index),
                        //    distanceScore = 1,
                        //    minJumpHeight = 0,
                        //    hullMask = 31,
                        //    jumpHullMask = 31,
                        //    gateIndex = 0
                        //});
                    }
                }
                Log.Info("A");

                NodeGraph.Node[] allNodes = nodes
                    .Where(x => x.linkListIndex.index != -1)
                    .ToArray();

                List<NodeGraph.Link> linkList = new List<NodeGraph.Link>();

                for (int i = 0; i < allNodes.Length; i++)
                {
                    var currentLinks = links[i];
                    int position = linkList.Count;
                    SerializableBitArray bitArray = new SerializableBitArray(currentLinks.Count);

                    for (int j = 0; j < currentLinks.Count; j++)
                    {
                        linkList.Add(currentLinks[j]);
                        bitArray[j] = true;
                    }

                    ref NodeGraph.Node node = ref allNodes[i];
                    node.linkListIndex.index = position;
                    node.linkListIndex.size = (uint)currentLinks.Count;
                    node.lineOfSightMask = bitArray;
                }

                Log.Info("A");

                Log.Info("A");
                //for (int i = 0; i < allNodes.Length; i++)
                //{
                //    var node = allNodes[i];
                //    SerializableBitArray bitArray = new SerializableBitArray(node.links.Count);
                //    for (int j = 0; j < node.links.Count; j++)
                //    {
                //        bitArray[j] = true;
                //    }
                //
                //    lineOfSightMasks[i] = bitArray;
                //}
                //Log.Info("A");

                groundNodes.nodes = allNodes;
                groundNodes.links = linkList.ToArray();

                groundNodes.Awake();

                //groundNodes.SetNodes(allNodes, lineOfSightMasks.AsReadOnly());

                Log.Info("A");
                sceneInfo.groundNodes = groundNodes;
                Log.Info("A");
                sceneInfo.airNodes = groundNodes;
                Log.Info("A");
            };



            Log.Info(i++);
            sceneObject.AddComponent<GlobalEventManager>();
            Log.Info(i++);

            

            sceneObject.SetActive(true);
            Log.Info(sceneObject.layer);


            //SceneManager.MoveGameObjectToScene(sceneObject, scene);


            //SceneManager.LoadSceneAsync(stageName);

            Log.Info(i++);
            Log.Info("Loaded!");
        }

        private void SceneDirector_PlaceTeleporter(On.RoR2.SceneDirector.orig_PlaceTeleporter orig, RoR2.SceneDirector self)
        {
            if (!ModEnabled.Value)
            {
                orig(self);
                return;
            }

            var oldTeleporterSpawnCard = self.teleporterSpawnCard;
            try
            {
                string path = (Run.instance.stageClearCount + 1) % 5 == 0
                    ? "RoR2/Base/Teleporters/iscLunarTeleporter.asset"
                    : "RoR2/Base/Teleporters/iscTeleporter.asset";

                self.teleporterSpawnCard = Addressables.LoadAssetAsync<InteractableSpawnCard>(path).WaitForCompletion();
                orig(self);
            }
            finally
            {
                self.teleporterSpawnCard = oldTeleporterSpawnCard;
            }
        }

        private Texture2D LoadTexture(string name)
        {
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(File.ReadAllBytes(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location), name)));
            return texture;
        }
    }
}

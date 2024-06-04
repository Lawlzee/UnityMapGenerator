using R2API;
using RoR2;
using RoR2.ContentManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace ProceduralStages
{
    public class ContentProvider : IContentPackProvider
    {
        public string identifier => Main.PluginGUID + "." + nameof(ContentProvider);

        public static string assetDirectory;

        public static ContentPack ContentPack = new ContentPack();

        public static Dictionary<TerrainType, Material> SeerMaterialByTerrainType = new Dictionary<TerrainType, Material>();

        public static Texture texOpenCavePreview;
        public static Texture texTunnelCavesPreview;
        public static Texture texIslandPreview;
        public static Texture texCanyonsPreview;
        public static Texture texBasaltPreview;
        public static GameObject runConfigPrefab;

        public static SceneDef[] LoopSceneDefs = new SceneDef[5];
        public static SceneDef ItSceneDef;

        public ContentProvider()
        {
        }

        public IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args)
        {
            ContentPack.identifier = identifier;

            var assetsFolderFullPath = System.IO.Path.GetDirectoryName(typeof(ContentProvider).Assembly.Location);
            assetDirectory = assetsFolderFullPath;

            AssetBundle scenesAssetBundle = null;
            yield return LoadAssetBundle(
                System.IO.Path.Combine(assetsFolderFullPath, "proceduralStage"),
                args.progressReceiver,
                (assetBundle) => scenesAssetBundle = assetBundle);

            AssetBundle assetsBundle = null;
            yield return LoadAssetBundle(
                System.IO.Path.Combine(assetsFolderFullPath, "proceduralAssets"),
                args.progressReceiver,
                (assetBundle) => assetsBundle = assetBundle);

            yield return LoadAllAssetsAsync(assetsBundle, args.progressReceiver, (Action<Texture[]>)((assets) =>
            {
                texOpenCavePreview = assets.First(a => a.name == "texOpenCavePreview");
                texTunnelCavesPreview = assets.First(a => a.name == "texTunnelCavesPreview");
                texIslandPreview = assets.First(a => a.name == "texIslandPreview");
                texCanyonsPreview = assets.First(a => a.name == "texCanyonsPreview");
                texBasaltPreview = assets.First(a => a.name == "texBasaltPreview");
            }));

            yield return LoadAllAssetsAsync(assetsBundle, args.progressReceiver, (Action<GameObject[]>)((assets) =>
            {
                runConfigPrefab = assets.First(a => a.name == "Run Config");

                foreach (var asset in assets)
                {
                    ClientScene.RegisterPrefab(asset);
                }
            }));

            var seerRequest = Addressables.LoadAssetAsync<Material>("RoR2/Base/bazaar/matBazaarSeerWispgraveyard.mat");
            while (!seerRequest.IsDone)
            {
                yield return null;
            }

            AddSeerMaterial(texOpenCavePreview, TerrainType.OpenCaves);
            AddSeerMaterial(texTunnelCavesPreview, TerrainType.TunnelCaves);
            AddSeerMaterial(texIslandPreview, TerrainType.Islands);
            AddSeerMaterial(texCanyonsPreview, TerrainType.Mines);
            AddSeerMaterial(texBasaltPreview, TerrainType.Basalt);

            SceneDef[] sceneDefs = new SceneDef[6];

            for (int i = 1; i <= 6; i++)
            {
                SceneDef sceneDef = ScriptableObject.CreateInstance<SceneDef>();
                sceneDef.cachedName = Main.SceneName;
                sceneDef.sceneType = SceneType.Stage;
                sceneDef.isOfflineScene = false;
                sceneDef.nameToken = "Random Realm";
                sceneDef.subtitleToken = "Chaotic Landscape";
                sceneDef.previewTexture = texOpenCavePreview;
                sceneDef.portalMaterial = SeerMaterialByTerrainType[TerrainType.OpenCaves];
                sceneDef.portalSelectionMessageString = "<style=cWorldEvent>You dream of dices.</style>";
                sceneDef.shouldIncludeInLogbook = false;
                sceneDef.loreToken = null;
                sceneDef.dioramaPrefab = null;
                sceneDef.suppressPlayerEntry = false;
                sceneDef.suppressNpcEntry = false;
                sceneDef.blockOrbitalSkills = false;
                sceneDef.validForRandomSelection = false;

                if (i < 6)
                {
                    sceneDef.stageOrder = i;

                    int nextStage = (i % 5) + 1;
                    var sceneCollectionRequest = Addressables.LoadAssetAsync<SceneCollection>($"RoR2/Base/SceneGroups/sgStage{nextStage}.asset");
                    while (!sceneCollectionRequest.IsDone)
                    {
                        yield return null;
                    }

                    sceneDef.destinationsGroup = sceneCollectionRequest.Result;
                    LoopSceneDefs[i - 1] = sceneDef;
                }
                else
                {
                    sceneDef.stageOrder = 99;
                    var sceneCollectionRequest = Addressables.LoadAssetAsync<SceneCollection>("RoR2/DLC1/GameModes/InfiniteTowerRun/SceneGroups/sgInfiniteTowerStageX.asset");
                    while (!sceneCollectionRequest.IsDone)
                    {
                        yield return null;
                    }

                    sceneDef.destinationsGroup = sceneCollectionRequest.Result;
                    ItSceneDef = sceneDef;
                }

                sceneDefs[i - 1] = sceneDef;
            }

            ContentPack.sceneDefs.Add(sceneDefs);

            Log.Info("SceneDef initialised");
            yield break;

            void AddSeerMaterial(Texture previewTexture, TerrainType terrainType)
            {
                var seerMaterial = UnityEngine.Object.Instantiate(seerRequest.Result);
                seerMaterial.mainTexture = previewTexture;

                SeerMaterialByTerrainType[terrainType] = seerMaterial;
            }
        }

        private IEnumerator LoadAssetBundle(string assetBundleFullPath, IProgress<float> progress, Action<AssetBundle> onAssetBundleLoaded)
        {
            var assetBundleCreateRequest = AssetBundle.LoadFromFileAsync(assetBundleFullPath);
            while (!assetBundleCreateRequest.isDone)
            {
                progress.Report(assetBundleCreateRequest.progress);
                yield return null;
            }

            onAssetBundleLoaded(assetBundleCreateRequest.assetBundle);

            yield break;
        }

        private static IEnumerator LoadAllAssetsAsync<T>(AssetBundle assetBundle, IProgress<float> progress, Action<T[]> onAssetsLoaded) where T : UnityEngine.Object
        {
            var sceneDefsRequest = assetBundle.LoadAllAssetsAsync<T>();
            while (!sceneDefsRequest.isDone)
            {
                progress.Report(sceneDefsRequest.progress);
                yield return null;
            }

            onAssetsLoaded(sceneDefsRequest.allAssets.Cast<T>().ToArray());

            yield break;
        }

        public IEnumerator GenerateContentPackAsync(GetContentPackAsyncArgs args)
        {
            ContentPack.Copy(ContentPack, args.output);

            args.ReportProgress(1f);
            yield break;
        }

        public IEnumerator FinalizeAsync(FinalizeAsyncArgs args)
        {
            args.ReportProgress(1f);
            yield break;
        }
    }
}

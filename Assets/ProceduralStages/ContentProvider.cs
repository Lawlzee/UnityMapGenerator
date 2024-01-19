using RoR2.ContentManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace ProceduralStages
{
    public class ContentProvider : IContentPackProvider
    {
        public string identifier => Main.PluginGUID + "." + nameof(ContentProvider);

        //private readonly ContentPack _contentPack = new ContentPack();

        public static string assetDirectory;

        public static Texture texScenePreview;
        public static GameObject seedSyncerPrefab;

        public ContentProvider()
        {

        }

        public IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args)
        {
            var assetsFolderFullPath = Path.GetDirectoryName(typeof(ContentProvider).Assembly.Location);
            assetDirectory = assetsFolderFullPath;

            AssetBundle scenesAssetBundle = null;
            yield return LoadAssetBundle(
                Path.Combine(assetsFolderFullPath, "proceduralStage"),
                args.progressReceiver,
                (assetBundle) => scenesAssetBundle = assetBundle);

            AssetBundle assetsBundle = null;
            yield return LoadAssetBundle(
                Path.Combine(assetsFolderFullPath, "proceduralAssets"),
                args.progressReceiver,
                (assetBundle) => assetsBundle = assetBundle);

            yield return LoadAllAssetsAsync(assetsBundle, args.progressReceiver, (Action<Texture[]>)((assets) =>
            {
                texScenePreview = assets.First(a => a.name == "texScenePreview");
            }));

            yield return LoadAllAssetsAsync(assetsBundle, args.progressReceiver, (Action<GameObject[]>)((assets) =>
            {
                seedSyncerPrefab = assets.First(a => a.name == "Seed Syncer");
                ClientScene.RegisterPrefab(seedSyncerPrefab);
            }));

            yield break;
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

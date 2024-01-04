using RoR2.ContentManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProceduralStages
{
    public class ContentProvider : IContentPackProvider
    {
        public string identifier => ProceduralStagesPlugin.PluginGUID + "." + nameof(ContentProvider);

        //private readonly ContentPack _contentPack = new ContentPack();

        public static String assetDirectory;

        private ProceduralStagesPlugin _plugin;

        public ContentProvider(ProceduralStagesPlugin plugin)
        {
            _plugin = plugin;
        }

        public IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args)
        {
            Log.Info("LoadStaticContentAsync");
            //_contentPack.identifier = identifier;

            var assetsFolderFullPath = Path.GetDirectoryName(typeof(ContentProvider).Assembly.Location);
            assetDirectory = assetsFolderFullPath;


            AssetBundle scenesAssetBundle = null;
            yield return LoadAssetBundle(
                Path.Combine(assetsFolderFullPath, "procedural"),
                args.progressReceiver,
                (assetBundle) => scenesAssetBundle = assetBundle);

            //var scene = SceneManager.LoadScene("random", new LoadSceneParameters
            //{
            //    loadSceneMode = LoadSceneMode.Additive
            //});
            //
            //_plugin.InitScene(scene);

            Log.Info("LoadStaticContentAsync done");
            //AssetBundle assetsAssetBundle = null;
            //yield return LoadAssetBundle(
            //    Path.Combine(assetsFolderFullPath, WaffleHouseContent.AssetsAssetBundleFileName),
            //    args.progressReceiver,
            //    (assetBundle) => assetsAssetBundle = assetBundle);
            //
            //yield return WaffleHouseContent.LoadAssetBundlesAsync(
            //    scenesAssetBundle, assetsAssetBundle,
            //    args.progressReceiver,
            //    _contentPack);

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

        public IEnumerator GenerateContentPackAsync(GetContentPackAsyncArgs args)
        {
            //ContentPack.Copy(_contentPack, args.output);

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

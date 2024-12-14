using ProceduralStages;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace ProceduralStages
{
    public class SceneLoader : MonoBehaviour
    {
        public string sceneName;
        public GameObject[] prefabs;

        void Start()
        {
            AsyncOperationHandle<SceneInstance> handler = Addressables.LoadSceneAsync(sceneName);
            handler.Completed += OnSceneLoaded;
        }

        private void OnSceneLoaded(AsyncOperationHandle<SceneInstance> obj)
        {
            GameObject gameObject = new GameObject("DelayedInstantier");
            DelayedInstantier delayedInstantier = gameObject.AddComponent<DelayedInstantier>();

            delayedInstantier.prefabs = prefabs;
        }
    }
}
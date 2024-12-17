using ProceduralStages;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace ProceduralStages
{
    public class SceneLoader : MonoBehaviour
    {
        public string sceneName;
        public GameObject[] prefabs;

        void Start()
        {
            Init(prefabs);
            Addressables.LoadSceneAsync(sceneName);
        }

        public static void Init(GameObject[] prefabs)
        {
            SceneManager.sceneLoaded += (Scene scene, LoadSceneMode mode) =>
            {
                GameObject gameObject = new GameObject("DelayedInstantier");
                DelayedInstantier delayedInstantier = gameObject.AddComponent<DelayedInstantier>();

                delayedInstantier.prefabs = prefabs;
            };
        }
    }
}
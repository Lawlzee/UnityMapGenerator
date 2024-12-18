using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace ProceduralStages
{
    public class SceneLoader : MonoBehaviour
    {
        public VanillaStageDef scene;
        public GameObject[] prefabs;

        void Start()
        {
            Init(prefabs);
            Addressables.LoadSceneAsync(scene.assetKey);
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
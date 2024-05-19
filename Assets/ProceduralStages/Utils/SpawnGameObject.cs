using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace ProceduralStages
{
    [DefaultExecutionOrder(-1)]
    public class SpawnGameObject : MonoBehaviour
    {
        public string objectName;
        public string asset;
        public Vector3 localPosition;
        public Vector3 localEulerAngles;
        public Vector3 localScale = new Vector3(1, 1, 1);
        public bool active = true;
        public bool spawnServer;

        public void Awake()
        {
            var prefab = Addressables.LoadAssetAsync<GameObject>(asset).WaitForCompletion();
            GameObject gameObject = Instantiate(prefab, transform);
            gameObject.name = objectName;
            gameObject.transform.localPosition = localPosition;
            gameObject.transform.localEulerAngles = localEulerAngles;
            gameObject.transform.localScale = localScale;
            gameObject.SetActive(active);

            if (spawnServer && NetworkServer.active)
            {
                NetworkServer.Spawn(gameObject);
            }
        }
    }
}

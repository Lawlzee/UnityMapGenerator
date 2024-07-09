using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ProceduralStages
{
    [CreateAssetMenu(fileName = "PrefabBackdrop", menuName = "ProceduralStages/PrefabBackdrop", order = 2)]
    public class PrefabBackdrop : BackdropTerrainGenerator
    {
        public string assetKey;
        public GameObject prefab;

        public override GameObject Generate(BackdropParams args)
        {
            GameObject prefabObject = prefab ?? Addressables.LoadAssetAsync<GameObject>(assetKey).WaitForCompletion();

            GameObject gameObject = Instantiate(prefabObject);
            gameObject.transform.position += args.center;

            return gameObject;
        }
    }
}

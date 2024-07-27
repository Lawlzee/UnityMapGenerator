using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.Networking;

namespace ProceduralStages
{
    public static class MoonDropship
    {
        public static GameObject Place(Vector3 position)
        {
            if (!NetworkServer.active)
            {
                return null;
            }

            GameObject prefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/moon2/Moon2DropshipZone.prefab").WaitForCompletion();
            
            GameObject dropship = Object.Instantiate(prefab);
            dropship.transform.position = position;

            NetworkServer.Spawn(dropship);
            return dropship;
        }
    }
}
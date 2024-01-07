using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UIElements;

namespace ProceduralStages
{
    public class NewtPlacer : MonoBehaviour
    {
        public Vector3 position;
        public Quaternion rotation;

        public void Start()
        {
            GameObject newt = Instantiate(Addressables.LoadAssetAsync<GameObject>("RoR2/Base/NewtStatue/NewtStatue.prefab").WaitForCompletion());
            var newPosition = position;
            newPosition.y++;
            newt.transform.position = newPosition;
            newt.transform.rotation = rotation;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine;

namespace ProceduralStages
{
    [DefaultExecutionOrder(-1)]
    public class MeshFilterMeshSetter : MonoBehaviour
    {
        public MeshFilter meshFilter;
        public string asset;
        public string childPath;

        public void Awake()
        {
            if (childPath != "")
            {
                GameObject prefab = Addressables.LoadAssetAsync<GameObject>(asset).WaitForCompletion();
                meshFilter.mesh = prefab.transform.Find(childPath).GetComponent<MeshFilter>().sharedMesh;
            }
            else
            {
                meshFilter.mesh = Addressables.LoadAssetAsync<Mesh>(asset).WaitForCompletion();
            }
        }
    }
}

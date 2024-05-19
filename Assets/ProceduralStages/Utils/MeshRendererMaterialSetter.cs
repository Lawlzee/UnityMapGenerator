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
    public class MeshRendererMaterialSetter : MonoBehaviour
    {
        public MeshRenderer meshRenderer;
        public string asset;

        public void Awake()
        {
            meshRenderer.material = Addressables.LoadAssetAsync<Material>(asset).WaitForCompletion();
        }
    }
}

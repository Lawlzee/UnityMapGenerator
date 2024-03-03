using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ProceduralStages
{
    [CreateAssetMenu(fileName = "skybox", menuName = "ProceduralStages/Skybox", order = 1)]
    public class SkyboxDef : ScriptableObject
    {
        public string asset;

        private Material _material;
        public Material material => _material
            ? _material
            : (_material = Addressables.LoadAssetAsync<Material>(asset).WaitForCompletion());
    }
}

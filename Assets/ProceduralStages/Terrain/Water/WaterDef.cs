using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ProceduralStages
{
    [CreateAssetMenu(fileName = "water", menuName = "ProceduralStages/Water", order = 1)]
    public class WaterDef : ScriptableObject
    {
        public string asset;

        [Range(0f, 1f)]
        public float reflection = 0.3f;
        [Range(0f, 1f)]
        public float distortion = 0f;

        [Range(0f, 1f)]
        public float minSaturation = 0f;
        [Range(0f, 1f)]
        public float maxSaturation = 1f;

        [Range(0f, 1f)]
        public float minValue = 0f;
        [Range(0f, 1f)]
        public float maxValue = 1f;

        private Material _material;
        public Material material => _material
            ? _material
            : (_material = Addressables.LoadAssetAsync<Material>(asset).WaitForCompletion());
    }
}

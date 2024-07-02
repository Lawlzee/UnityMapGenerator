using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ProceduralStages
{
    [CreateAssetMenu(fileName = "props", menuName = "ProceduralStages/Props", order = 0)]
    public class PropsDefinition : ScriptableObject
    {
        public string asset;
        public float minScale = 1;
        public float maxScale = 1;
        public bool ground;
        public int count;
        public bool changeColor;
        public bool isRock;
        public MaterialType material = MaterialType.Stone;
        public Vector3 normal;
        public Vector3 offset;
        public bool isSolid;
        public bool addCollision;
        public bool isBig;
        public Vector3 initialRotation;

        private GameObject _prefab;
        public GameObject prefab => _prefab
            ? _prefab
            : (_prefab = Addressables.LoadAssetAsync<GameObject>(asset).WaitForCompletion());

        private SurfaceDef _surfaceDef;
        public SurfaceDef surfaceDef => _surfaceDef
            ? _surfaceDef
            : (_surfaceDef = Addressables.LoadAssetAsync<SurfaceDef>("RoR2/Base/Common/sd" + material + ".asset").WaitForCompletion());
    }
}

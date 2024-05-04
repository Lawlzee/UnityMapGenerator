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
        public Vector3 normal;
        public Vector3 offset;
        public bool isSolid;
        public bool addCollision;

        private GameObject _prefab;
        public GameObject prefab => _prefab
            ? _prefab
            : (_prefab = Addressables.LoadAssetAsync<GameObject>(asset).WaitForCompletion());
    }
}

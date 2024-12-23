using RoR2;
using RoR2.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace ProceduralStages
{
    [CreateAssetMenu(fileName = "VanillaStageDefConfig", menuName = "ProceduralStages/VanillaStageDefConfig", order = 10)]
    public class VanillaStageDefConfig : ScriptableObject
    {
        public float minFloorAngle = 0.35f;
        public MeshReadabilityTransformer meshTransformer;
    }
}

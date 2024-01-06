using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RoR2
{
    [CreateAssetMenu]
    [Serializable]
    public class SurfaceDef : ScriptableObject
    {
        public Color approximateColor;
        public GameObject impactEffectPrefab;
        public GameObject footstepEffectPrefab;
        public string impactSoundString;
        public string materialSwitchString;
        public bool isSlippery;
    }
}

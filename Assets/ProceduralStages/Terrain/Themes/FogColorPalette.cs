using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProceduralStages
{
    [CreateAssetMenu(fileName = "fog", menuName = "ProceduralStages/Fog", order = 1)]
    public class FogColorPalette : ScriptableObject
    {
        [Range(0, 1)]
        public float saturation;
        [Range(0, 1)]
        public float value;

        [Range(0, 1)]
        public float colorStartAlpha;
        [Range(0, 1)]
        public float colorMidAlpha;
        [Range(0, 1)]
        public float colorEndAlpha;

        [Range(0, 1)]
        public float zero;
        [Range(0, 1)]
        public float one;

        [Range(0, 1)]
        public float intensity;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProceduralStages
{
    [CreateAssetMenu(fileName = "surfaceColor", menuName = "ProceduralStages/SurfaceColor", order = 4)]
    public class SurfaceColor : ScriptableObject
    {
        [Range(0, 1)]
        public float saturation;
        [Range(0, 1)]
        public float value;
        [Range(0, 1)]
        public float perlinAmplitude;
        [Range(0, 1)]
        public float detailPerlinAmplitude;
    }
}

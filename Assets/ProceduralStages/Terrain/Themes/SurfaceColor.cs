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
        public Color minColor;
        public Color maxColor;
    }
}

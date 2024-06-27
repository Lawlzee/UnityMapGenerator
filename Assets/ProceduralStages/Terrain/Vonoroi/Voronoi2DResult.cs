using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProceduralStages
{
    [Serializable]
    public struct Voronoi2DResult
    {
        public Vector2 displacement1;
        public Vector2 displacement2;
        public float weight;
    }
}

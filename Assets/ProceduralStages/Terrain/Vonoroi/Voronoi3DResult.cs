using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProceduralStages
{
    [Serializable]
    public struct Voronoi3DResult
    {
        public Vector3 displacement1;
        public Vector3 displacement2;
        public float weight;
    }
}

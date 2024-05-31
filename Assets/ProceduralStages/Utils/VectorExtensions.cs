using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProceduralStages
{
    public static class VectorExtensions
    {
        public static Vector2 Frac(this Vector2 v)
        {
            return new Vector2(
                v.x - Mathf.FloorToInt(v.x),
                v.y - Mathf.FloorToInt(v.y));
        }

        public static Vector3 Frac(this Vector3 v)
        {
            return new Vector3(
                v.x - Mathf.FloorToInt(v.x),
                v.y - Mathf.FloorToInt(v.y),
                v.z - Mathf.FloorToInt(v.z));
        }
    }
}

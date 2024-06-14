using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProceduralStages
{
    public static class VectorExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Frac(this Vector2 v)
        {
            return new Vector2(
                v.x - Mathf.Floor(v.x),
                v.y - Mathf.Floor(v.y));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Frac(this Vector3 v)
        {
            return new Vector3(
                v.x - (float)Math.Truncate(v.x),
                v.y - (float)Math.Truncate(v.y),
                v.z - (float)Math.Truncate(v.z));
        }
    }
}

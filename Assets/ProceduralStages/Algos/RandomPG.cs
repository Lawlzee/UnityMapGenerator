using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProceduralStages
{
    //https://github.com/patriciogonzalezvivo/lygia/blob/main/generative/random.hlsl
    public static class RandomPG
    {
        public static readonly Vector4 _randomScale = new Vector4(443.897f, 441.423f, 0.0973f, 1.6334f);

        public static float Random(Vector2 point)
        {
            Vector3 point2 = (new Vector3(point.x * _randomScale.x, point.y * _randomScale.y, point.x * _randomScale.z)).Frac();
            var dot = Vector3.Dot(point2, new Vector3(point2.y + 33.33f, point2.z + 33.33f, point2.x + 33.33f));
            Vector3 point3 = new Vector3(point2.x + dot, point2.y + dot, point2.z + dot);
            float result = (point3.x + point3.y) * point3.z;
            return result - Mathf.Floor(result);
        }

        public static Vector2 Random2(Vector2 point)
        {
            Vector3 point2 = (new Vector3(point.x * _randomScale.x, point.y * _randomScale.y, point.x * _randomScale.z)).Frac();
            var dot = Vector3.Dot(point2, new Vector3(point2.y + 19.19f, point2.z + 19.19f, point2.x + 19.19f));
            Vector3 point3 = new Vector3(point2.x + dot, point2.y + dot, point2.z + dot);
            return ((new Vector2(point3.x, point3.x) + new Vector2(point3.y, point3.z)) * new Vector2(point3.z, point3.y)).Frac();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Random3(Vector3 point)
        {
            Vector3 point2 = new Vector3(
                point.x * _randomScale.x,
                point.y * _randomScale.y,
                point.z * _randomScale.z).Frac();

            float dot = Vector3.Dot(point2, new Vector3(point2.y + 19.19f, point2.z + 19.19f, point2.x + 19.19f));
            Vector3 point3 = new Vector3(point2.x + dot, point2.y + dot, point2.z + dot);
            Vector3 point4 = new Vector3(point3.x + point3.y, point3.x + point3.z, point3.y + point3.z);
            return new Vector3(point4.x * point3.z, point4.y * point3.y, point4.z * point3.x).Frac();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Random4(Vector4 point)
        {
            Vector4 point2 = new Vector4(
                point.x * _randomScale.x,
                point.y * _randomScale.y,
                point.z * _randomScale.z,
                point.w * _randomScale.w).Frac();

            float dot = Vector4.Dot(point2, new Vector4(point2.w + 19.19f, point2.z + 19.19f, point2.y + 19.19f, point2.x + 19.19f));
            Vector4 point3 = new Vector4(point2.x + dot, point2.y + dot, point2.z + dot, point2.w + dot);
            Vector4 point4 = new Vector4(point3.x + point3.y, point3.x + point3.z, point3.y + point3.z, point3.z + point3.w);
            return new Vector4(point4.x * point3.z, point4.y * point3.y, point4.z * point3.w, point4.z * point4.x).Frac();
        }
    }
}

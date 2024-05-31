using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProceduralStages
{
    //https://github.com/patriciogonzalezvivo/lygia/blob/main/generative/random.hlsl
    public static class RandomPG
    {
        private static readonly Vector3 _randomScale = new Vector3(443.897f, 441.423f, 0.0973f);

        public static Vector2 Random2(Vector2 point)
        {
            Vector3 point2 = (new Vector3(point.x * _randomScale.x, point.y * _randomScale.y, point.x * _randomScale.z)).Frac();
            var dot = Vector3.Dot(point2, new Vector3(point2.y + 19.19f, point2.z + 19.19f, point2.x + 19.19f));
            Vector3 point3 = new Vector3(point2.x + dot, point2.y + dot, point2.z + dot);
            return ((new Vector2(point3.x, point3.x) + new Vector2(point3.y, point3.z)) * new Vector2(point3.z, point3.y)).Frac();
        }

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
    }
}

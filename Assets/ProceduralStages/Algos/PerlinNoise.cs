using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProceduralStages
{
    public class PerlinNoise
    {
        private static int[] permutation =
        {
            151,160,137, 91, 90, 15,131, 13,201, 95, 96, 53,194,233,  7,225,
            140, 36,103, 30, 69,142,  8, 99, 37,240, 21, 10, 23,190,  6,148,
            247,120,234, 75,  0, 26,197, 62, 94,252,219,203,117, 35, 11, 32,
            57,177, 33, 88,237,149, 56, 87,174, 20,125,136,171,168, 68,175,
            74,165, 71,134,139, 48, 27,166, 77,146,158,231, 83,111,229,122,
            60,211,133,230,220,105, 92, 41, 55, 46,245, 40,244,102,143, 54,
            65, 25, 63,161,  1,216, 80, 73,209, 76,132,187,208, 89, 18,169,
            200,196,135,130,116,188,159, 86,164,100,109,198,173,186,  3, 64,
            52,217,226,250,124,123,  5,202, 38,147,118,126,255, 82, 85,212,
            207,206, 59,227, 47, 16, 58, 17,182,189, 28, 42,223,183,170,213,
            119,248,152,  2, 44,154,163, 70,221,153,101,155,167, 43,172,  9,
            129, 22, 39,253, 19, 98,108,110, 79,113,224,232,178,185,112,104,
            218,246, 97,228,251, 34,242,193,238,210,144, 12,191,179,162,241,
            81, 51,145,235,249, 14,239,107, 49,192,214, 31,181,199,106,157,
            184, 84,204,176,115,121, 50, 45,127,  4,150,254,138,236,205, 93,
            222,114, 67, 29, 24, 72,243,141,128,195, 78, 66,215, 61,156,180,

            151,160,137, 91, 90, 15,131, 13,201, 95, 96, 53,194,233,  7,225,
            140, 36,103, 30, 69,142,  8, 99, 37,240, 21, 10, 23,190,  6,148,
            247,120,234, 75,  0, 26,197, 62, 94,252,219,203,117, 35, 11, 32,
            57,177, 33, 88,237,149, 56, 87,174, 20,125,136,171,168, 68,175,
            74,165, 71,134,139, 48, 27,166, 77,146,158,231, 83,111,229,122,
            60,211,133,230,220,105, 92, 41, 55, 46,245, 40,244,102,143, 54,
            65, 25, 63,161,  1,216, 80, 73,209, 76,132,187,208, 89, 18,169,
            200,196,135,130,116,188,159, 86,164,100,109,198,173,186,  3, 64,
            52,217,226,250,124,123,  5,202, 38,147,118,126,255, 82, 85,212,
            207,206, 59,227, 47, 16, 58, 17,182,189, 28, 42,223,183,170,213,
            119,248,152,  2, 44,154,163, 70,221,153,101,155,167, 43,172,  9,
            129, 22, 39,253, 19, 98,108,110, 79,113,224,232,178,185,112,104,
            218,246, 97,228,251, 34,242,193,238,210,144, 12,191,179,162,241,
            81, 51,145,235,249, 14,239,107, 49,192,214, 31,181,199,106,157,
            184, 84,204,176,115,121, 50, 45,127,  4,150,254,138,236,205, 93,
            222,114, 67, 29, 24, 72,243,141,128,195, 78, 66,215, 61,156,180
        };

        const int permutationCount = 255;

        private static readonly Vector3[] _gradiants =
        {
            new Vector3( 1f, 1f, 0f),
            new Vector3(-1f, 1f, 0f),
            new Vector3( 1f,-1f, 0f),
            new Vector3(-1f,-1f, 0f),
            new Vector3( 1f, 0f, 1f),
            new Vector3(-1f, 0f, 1f),
            new Vector3( 1f, 0f,-1f),
            new Vector3(-1f, 0f,-1f),
            new Vector3( 0f, 1f, 1f),
            new Vector3( 0f,-1f, 1f),
            new Vector3( 0f, 1f,-1f),
            new Vector3( 0f,-1f,-1f),
            new Vector3( 1f, 1f, 0f),
            new Vector3(-1f, 1f, 0f),
            new Vector3( 0f,-1f, 1f),
            new Vector3( 0f,-1f,-1f)
        };

        private const int gradiantsCount = 15;

        public static float Get(Vector3 point, float frequency)
        {
            point *= frequency;

            int flooredPointX0 = Mathf.FloorToInt(point.x);
            int flooredPointY0 = Mathf.FloorToInt(point.y);
            int flooredPointZ0 = Mathf.FloorToInt(point.z);

            float x0 = point.x - flooredPointX0;
            float y0 = point.y - flooredPointY0;
            float z0 = point.z - flooredPointZ0;

            float x1 = x0 - 1f;
            float y1 = y0 - 1f;
            float z1 = z0 - 1f;

            flooredPointX0 &= permutationCount;
            flooredPointY0 &= permutationCount;
            flooredPointZ0 &= permutationCount;

            int flooredPointX1 = flooredPointX0 + 1;
            int flooredPointY1 = flooredPointY0 + 1;
            int flooredPointZ1 = flooredPointZ0 + 1;

            int permutationX0 = permutation[flooredPointX0];
            int permutationX1 = permutation[flooredPointX1];

            int permutationY00 = permutation[permutationX0 + flooredPointY0];
            int permutationY10 = permutation[permutationX1 + flooredPointY0];
            int permutationY01 = permutation[permutationX0 + flooredPointY1];
            int permutationY11 = permutation[permutationX1 + flooredPointY1];
            /*
            int permutationZ000 = permutation[permutationY00 + flooredPointZ0];
            int permutationZ100 = permutation[permutationY10 + flooredPointZ0];
            int permutationZ010 = permutation[permutationY01 + flooredPointZ0];
            int permutationZ110 = permutation[permutationY11 + flooredPointZ0];
            int permutationZ001 = permutation[permutationY00 + flooredPointZ1];
            int permutationZ101 = permutation[permutationY01 + flooredPointZ1];
            int permutationZ011 = permutation[permutationY10 + flooredPointZ1];
            int permutationZ111 = permutation[permutationY11 + flooredPointZ1];
            */
            Vector3 gradiant000 = _gradiants[permutation[permutationY00 + flooredPointZ0] & gradiantsCount];
            Vector3 gradiant100 = _gradiants[permutation[permutationY10 + flooredPointZ0] & gradiantsCount];
            Vector3 gradiant010 = _gradiants[permutation[permutationY01 + flooredPointZ0] & gradiantsCount];
            Vector3 gradiant110 = _gradiants[permutation[permutationY11 + flooredPointZ0] & gradiantsCount];
            Vector3 gradiant001 = _gradiants[permutation[permutationY00 + flooredPointZ1] & gradiantsCount];
            Vector3 gradiant101 = _gradiants[permutation[permutationY10 + flooredPointZ1] & gradiantsCount];
            Vector3 gradiant011 = _gradiants[permutation[permutationY01 + flooredPointZ1] & gradiantsCount];
            Vector3 gradiant111 = _gradiants[permutation[permutationY11 + flooredPointZ1] & gradiantsCount];

            /*
            Vector3 direction000 = directions[permutationZ000 & directionCount];
            Vector3 direction100 = directions[permutationZ100 & directionCount];
            Vector3 direction010 = directions[permutationZ010 & directionCount];
            Vector3 direction110 = directions[permutationZ110 & directionCount];
            Vector3 direction001 = directions[permutationZ001 & directionCount];
            Vector3 direction101 = directions[permutationZ101 & directionCount];
            Vector3 direction011 = directions[permutationZ011 & directionCount];
            Vector3 direction111 = directions[permutationZ111 & directionCount];
            */

            float dot000 = Scalar(gradiant000, new Vector3(x0, y0, z0));
            float dot100 = Scalar(gradiant100, new Vector3(x1, y0, z0));
            float dot010 = Scalar(gradiant010, new Vector3(x0, y1, z0));
            float dot110 = Scalar(gradiant110, new Vector3(x1, y1, z0));
            float dot001 = Scalar(gradiant001, new Vector3(x0, y0, z1));
            float dot101 = Scalar(gradiant101, new Vector3(x1, y0, z1));
            float dot011 = Scalar(gradiant011, new Vector3(x0, y1, z1));
            float dot111 = Scalar(gradiant111, new Vector3(x1, y1, z1));

            float u = SmoothDistance(x0);
            float v = SmoothDistance(y0);
            float w = SmoothDistance(z0);

            return Mathf.Lerp(
                Mathf.Lerp(
                    Mathf.Lerp(dot000, dot100, u),
                    Mathf.Lerp(dot010, dot110, u),
                    v),
                Mathf.Lerp(
                    Mathf.Lerp(dot001, dot101, u),
                    Mathf.Lerp(dot011, dot111, u),
                    v),
                w);

            float Scalar(Vector3 a, Vector3 b)
            {
                return a.x * b.x + a.y * b.y + a.z * b.z;
            }

            float SmoothDistance(float d)
            {
                return d * d * d * (d * (d * 6f - 15f) + 10f);
            }
        }

        //https://stackoverflow.com/questions/4297024/3d-perlin-noise-analytical-derivative
        public static Vector4 GetWithDerivative(Vector3 point, float frequency)
        {
            point *= frequency;

            int flooredPointX0 = Mathf.FloorToInt(point.x);
            int flooredPointY0 = Mathf.FloorToInt(point.y);
            int flooredPointZ0 = Mathf.FloorToInt(point.z);

            float x0 = point.x - flooredPointX0;
            float y0 = point.y - flooredPointY0;
            float z0 = point.z - flooredPointZ0;

            float x1 = x0 - 1f;
            float y1 = y0 - 1f;
            float z1 = z0 - 1f;

            flooredPointX0 &= permutationCount;
            flooredPointY0 &= permutationCount;
            flooredPointZ0 &= permutationCount;

            int flooredPointX1 = flooredPointX0 + 1;
            int flooredPointY1 = flooredPointY0 + 1;
            int flooredPointZ1 = flooredPointZ0 + 1;

            int permutationX0 = permutation[flooredPointX0];
            int permutationX1 = permutation[flooredPointX1];

            int permutationY00 = permutation[permutationX0 + flooredPointY0];
            int permutationY10 = permutation[permutationX1 + flooredPointY0];
            int permutationY01 = permutation[permutationX0 + flooredPointY1];
            int permutationY11 = permutation[permutationX1 + flooredPointY1];
            /*
            int permutationZ000 = permutation[permutationY00 + flooredPointZ0];
            int permutationZ100 = permutation[permutationY10 + flooredPointZ0];
            int permutationZ010 = permutation[permutationY01 + flooredPointZ0];
            int permutationZ110 = permutation[permutationY11 + flooredPointZ0];
            int permutationZ001 = permutation[permutationY00 + flooredPointZ1];
            int permutationZ101 = permutation[permutationY01 + flooredPointZ1];
            int permutationZ011 = permutation[permutationY10 + flooredPointZ1];
            int permutationZ111 = permutation[permutationY11 + flooredPointZ1];
            */
            Vector3 gradiant000 = _gradiants[permutation[permutationY00 + flooredPointZ0] & gradiantsCount];
            Vector3 gradiant100 = _gradiants[permutation[permutationY10 + flooredPointZ0] & gradiantsCount];
            Vector3 gradiant010 = _gradiants[permutation[permutationY01 + flooredPointZ0] & gradiantsCount];
            Vector3 gradiant110 = _gradiants[permutation[permutationY11 + flooredPointZ0] & gradiantsCount];
            Vector3 gradiant001 = _gradiants[permutation[permutationY00 + flooredPointZ1] & gradiantsCount];
            Vector3 gradiant101 = _gradiants[permutation[permutationY10 + flooredPointZ1] & gradiantsCount];
            Vector3 gradiant011 = _gradiants[permutation[permutationY01 + flooredPointZ1] & gradiantsCount];
            Vector3 gradiant111 = _gradiants[permutation[permutationY11 + flooredPointZ1] & gradiantsCount];

            /*
            Vector3 direction000 = directions[permutationZ000 & directionCount];
            Vector3 direction100 = directions[permutationZ100 & directionCount];
            Vector3 direction010 = directions[permutationZ010 & directionCount];
            Vector3 direction110 = directions[permutationZ110 & directionCount];
            Vector3 direction001 = directions[permutationZ001 & directionCount];
            Vector3 direction101 = directions[permutationZ101 & directionCount];
            Vector3 direction011 = directions[permutationZ011 & directionCount];
            Vector3 direction111 = directions[permutationZ111 & directionCount];
            */

            float dot000 = Scalar(gradiant000, new Vector3(x0, y0, z0));
            float dot100 = Scalar(gradiant100, new Vector3(x1, y0, z0));
            float dot010 = Scalar(gradiant010, new Vector3(x0, y1, z0));
            float dot110 = Scalar(gradiant110, new Vector3(x1, y1, z0));
            float dot001 = Scalar(gradiant001, new Vector3(x0, y0, z1));
            float dot101 = Scalar(gradiant101, new Vector3(x1, y0, z1));
            float dot011 = Scalar(gradiant011, new Vector3(x0, y1, z1));
            float dot111 = Scalar(gradiant111, new Vector3(x1, y1, z1));

            float u = SmoothDistance(x0);
            float v = SmoothDistance(y0);
            float w = SmoothDistance(z0);

            float noise = Mathf.Lerp(
                Mathf.Lerp(
                    Mathf.Lerp(dot000, dot100, u),
                    Mathf.Lerp(dot010, dot110, u),
                    v),
                Mathf.Lerp(
                    Mathf.Lerp(dot001, dot101, u),
                    Mathf.Lerp(dot011, dot111, u),
                    v),
                w);

            float up = 30 * x0 * x0 * (x0 - 1) * (x0 - 1);
            float vp = 30 * y0 * y0 * (y0 - 1) * (y0 - 1);
            float wp = 30 * z0 * z0 * (z0 - 1) * (z0 - 1);

            float nx = gradiant000.x
               + up * (dot100 - dot000)
               + u * (gradiant100.x - gradiant000.x)
               + v * (gradiant010.x - gradiant000.x)
               + w * (gradiant001.x - gradiant000.x)
               + up * v * (dot110 - dot010 - dot100 + dot000)
               + u * v * (gradiant110.x - gradiant010.x - gradiant100.x + gradiant000.x)
               + u * w * (gradiant101.x - gradiant001.x - gradiant100.x - gradiant000.x)
               + up * w * (dot101 - dot001 - dot100 + dot000)
               + v * w * (gradiant011.x - gradiant001.x - gradiant010.x + gradiant000.x)
               + up * v * w * (dot111 - dot011 - dot101 + dot001 - dot110 + dot010 + dot100 - dot000)
               + u * v * w * (gradiant111.x - gradiant011.x - gradiant101.x + gradiant001.x - gradiant110.x + gradiant010.x + gradiant100.x - gradiant000.x);

            float ny = gradiant000.y
               + u * (gradiant100.y - gradiant000.y)
               + vp * (dot010 - dot000)
               + v * (gradiant010.y - gradiant000.y)
               + w * (gradiant001.y - gradiant000.y)
               + u * vp * (dot110 - dot010 - dot100 + dot000)
               + u * v * (gradiant110.y - gradiant010.y - gradiant100.y + gradiant000.y)
               + u * w * (gradiant101.y - gradiant001.y - gradiant100.y + gradiant000.y)
               + vp * w * (dot011 - dot001 - dot010 + dot000)
               + v * w * (gradiant011.y - gradiant001.y - gradiant010.y + gradiant000.y)
               + u * vp * w * (dot111 - dot011 - dot101 + dot001 - dot110 + dot010 + dot100 - dot000)
               + u * v * w * (gradiant111.y - gradiant011.y - gradiant101.y + gradiant001.y - gradiant110.y + gradiant010.y + gradiant100.y - gradiant000.y);

            float nz = gradiant000.z
               + u * (gradiant100.z - gradiant000.z)
               + v * (gradiant010.z - gradiant000.z)
               + wp * (dot001 - dot000)
               + w * (gradiant001.z - gradiant000.z)
               + u * v * (gradiant110.z - gradiant010.z - gradiant100.z + gradiant000.z)
               + u * wp * (dot101 - dot001 - dot100 + dot000)
               + u * w * (gradiant101.z - gradiant001.z - gradiant100.z + gradiant000.z)
               + v * wp * (dot011 - dot001 - dot010 + dot000)
               + v * w * (gradiant011.z - gradiant001.z - gradiant010.z + gradiant000.z)
               + u * v * wp * (dot111 - dot011 - dot101 + dot001 - dot110 + dot010 + dot100 - dot000)
               + u * v * w * (gradiant111.z - gradiant011.z - gradiant101.z + gradiant001.z - gradiant110.z + gradiant010.z + gradiant100.z - gradiant000.z);

            return new Vector4(nx, ny, nz, noise);

            float Scalar(Vector3 a, Vector3 b)
            {
                return a.x * b.x + a.y * b.y + a.z * b.z;
            }

            float SmoothDistance(float d)
            {
                return d * d * d * (d * (d * 6f - 15f) + 10f);
            }
        }
    }
}

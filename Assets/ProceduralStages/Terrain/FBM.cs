using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProceduralStages
{
    [CreateAssetMenu(fileName = "FBM", menuName = "ProceduralStages/FBM", order = 2)]
    public class FBM : ScriptableObject
    {
        public float amplitude = 1f;
        [Range(0, 1)]
        public float frequency = 0.05f;
        [Range(0, 10)]
        public int octaves = 4;
        [Range(0, 1)]
        public float persistence = 0.5f;
        [Range(0, 100)]
        public float lacunarity = 2f;

        public float Evaluate(Vector2 point)
        {
            return Evaluate(point.x, point.y);
        }

        public float Evaluate(float x, float y)
        {
            float value = 0;

            float currentAmplitude = amplitude;
            float currentFrequency = frequency;

            for (int i = 0; i < octaves; i++)
            {
                value += currentAmplitude * (Mathf.PerlinNoise(x * currentFrequency, y * currentFrequency) * 2 - 1);
                currentAmplitude *= persistence;
                currentFrequency *= lacunarity;
            }

            return value;
        }

        public float Evaluate(float x, float y, float z)
        {
            return Evaluate(new Vector3(x, y, z));
        }

        public float Evaluate(Vector3 point)
        {
            float value = 0;

            float currentAmplitude = amplitude;
            float currentFrequency = frequency;

            for (int i = 0; i < octaves; i++)
            {
                value += currentAmplitude * PerlinNoise.Get(point, currentFrequency);
                currentAmplitude *= persistence;
                currentFrequency *= lacunarity;
            }

            return value;
        }

        public (float Noise, Vector2 Derivative) EvaluateWithDerivative(float x, float y)
        {
            return EvaluateWithDerivative(new Vector3(x, y, 0));
        }

        public (float Noise, Vector2 Derivative) EvaluateWithDerivative(Vector2 point)
        {
            Vector3 point3d = point;
            return EvaluateWithDerivative(point3d);
        }

        public (float Noise, Vector3 Derivative) EvaluateWithDerivative(float x, float y, float z)
        {
            return EvaluateWithDerivative(new Vector3(x, y, z));
        }

        public (float Noise, Vector3 Derivative) EvaluateWithDerivative(Vector3 point)
        {
            Vector4 value = new Vector4();

            float currentAmplitude = amplitude;
            float currentFrequency = frequency;

            for (int i = 0; i < octaves; i++)
            {
                Vector4 noiseWithDerivative = PerlinNoise.GetWithDerivative(point, currentFrequency);
                value += currentAmplitude * noiseWithDerivative;
                currentAmplitude *= persistence;
                currentFrequency *= lacunarity;
            }

            Vector3 derivative = value;
            return (value.w, derivative);
        }
    }
}

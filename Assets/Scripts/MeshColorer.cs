using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts
{
    public class MeshColorer
    {
        private const float grassAngle = -0.15f;
        private const float grassMultiplier = 0.5f / (1 - grassAngle);
        private const float stoneMultiplier = -0.5f / (1 + grassAngle);

        private readonly Vector3 _seed;
        private readonly float _frequency;
        private readonly float _frequency2;
        private readonly float _textureFrequency2Amplitude;

        public MeshColorer(System.Random rng, float frequency, float frequency2, float textureFrequency2Amplitude)
        {
            _seed = new Vector3((float)rng.NextDouble(), (float)rng.NextDouble(), (float)rng.NextDouble());
            _frequency = frequency;
            _frequency2 = frequency2;
            _textureFrequency2Amplitude = textureFrequency2Amplitude;
        }

        public Vector2 GetUV(Vector3 vertex, Vector3 normal)
        {

            float dot = Vector3.Dot(new Vector3(0, -1, 0), normal);
            float angle;
            if (dot < grassAngle)
            {
                angle = 0.5f * ((dot + 1) / (1 + grassAngle));
                //angle = dot + grassAngle;
                //dot = 1 - (dot + 1);
                //angle = ((dot - grassAngle) * stoneMultiplier) + 0.5f;
            }
            else
            {
                //angle = Mathf.SmoothStep(grassAngle, 1, dot);

                //angle = (dot - grassAngle) * grassMultiplier;
                angle = (dot - grassAngle) / (2 * (1 - grassAngle)) + 0.5f;
            }


            //angle = (dot + 1) / 2f;

            //float dot = ( + 1) / 2f;
            //float dot = Mathf.Lerp(Vector3.Dot(new Vector3(0, 1, 0), normal), 0 , 1);
            float noise = (PerlinNoise.Get(vertex + _seed, _frequency) + 1) / 2f;
            noise += _textureFrequency2Amplitude * PerlinNoise.Get(vertex + _seed, _frequency2);

            Vector2 uv = new Vector2(angle, noise);


            return uv;
        }
    }
}

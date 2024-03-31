using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProceduralStages
{
    //https://www.shadertoy.com/view/stccDB
    [Serializable]
    public class SinCaver
    {
        public float a = 1;
        public float b = 1;
        public float c = 1;

        public float layersDistance;
        public float layersAmplitude;
        public float layersFrequency;
        public ThreadSafeCurve surfaceCurve;

        public float[,,] Create(Vector3Int size)
        {
            float[,,] map = new float[size.x, size.y, size.z];

            float inverseMaxDistance = 1 / (layersAmplitude + layersFrequency);
            int layerCount = Mathf.CeilToInt(size.y / layersDistance);
            Vector2Int[] seeds = new Vector2Int[layerCount];
            for (int i = 0; i < layerCount; i++)
            {
                seeds[i] = new Vector2Int(
                    MapGenerator.rng.RangeInt(0, short.MaxValue),
                    MapGenerator.rng.RangeInt(0, short.MaxValue));
            }


            Parallel.For(0, size.x, x =>
            {
                float[] layerHeights = new float[layerCount];

                for (int z = 0; z < size.z; z++)
                {
                    for (int i = 0; i < layerCount; i++)
                    {
                        layerHeights[i] = i * layersDistance + layersAmplitude * Mathf.PerlinNoise(layersFrequency * x + seeds[i].x, layersFrequency * z + seeds[i].y);
                    }

                    for (int y = 0; y < size.y; y++)
                    {
                        float minDistance = float.MaxValue;

                        for (int i = 0; i < layerCount; i++)
                        {
                            float distance = Math.Abs(y - layerHeights[i]);
                            if (distance < minDistance)
                            {
                                minDistance = distance;
                            }
                        }

                        map[x, y, z] = surfaceCurve.Evaluate(minDistance * inverseMaxDistance);

                        //int sign = (Mathf.FloorToInt(a * y / (2 * Mathf.PI)) % 2) * 2 - 1;
                        //
                        //float noiseXZ = (Mathf.Cos(b * x + c * z) + 1) * 0.5f;
                        //float noiseY = a * y + noiseXZ;
                        //
                        //
                        ////int sign = (Mathf.FloorToInt(0.5f * (noiseY - 1)) % 2) * 2 - 1;
                        //
                        //map[x, y, z] = 0.5f * (sign * Mathf.Cos(noiseY) + 1);
                    }
                }
            });
            return map;
        }
    }
}

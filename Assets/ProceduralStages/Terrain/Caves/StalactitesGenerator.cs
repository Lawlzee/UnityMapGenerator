using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProceduralStages
{
    [CreateAssetMenu(fileName = "StalactitesGenerator", menuName = "ProceduralStages/StalactitesGenerator", order = 2)]
    public class StalactitesGenerator : ScriptableObject
    {
        public FBM fbm;
        public ThreadSafeCurve curve;
        public float blendFactor;

        public void AddStalactites(float[,,] map)
        {
            int seedX = MapGenerator.rng.RangeInt(0, short.MaxValue);
            int seedZ = MapGenerator.rng.RangeInt(0, short.MaxValue);

            int width3d = map.GetLength(0);
            int height3d = map.GetLength(1);
            int depth3d = map.GetLength(2);

            Parallel.For(0, width3d, x =>
            {
                for (int z = 0; z < depth3d; z++)
                {
                    float noise = 0.5f * (1 + fbm.Evaluate(x + seedX, z + seedZ));
                    float minHeigth = height3d * (1 - curve.Evaluate(noise));

                    for (int y = height3d - 1; y >= 0; y--)
                    {
                        float relativeNoise = Mathf.Clamp01((y - minHeigth) * blendFactor + 0.5f);
                        if (relativeNoise == 0f)
                        {
                            break;
                        }

                        map[x, y, z] = Mathf.Max(relativeNoise, map[x, y, z]);
                    }
                }
            });
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProceduralStages
{
    [Serializable]
    public class MeshColorer
    {
        [Range(-1, 1)]
        public float grassAngle = -0.15f;

        [Range(0, 1000)]
        public float detailSize = 100f;

        [Range(0, 1)]
        public float baseFrequency;
        [Range(0, 1)]
        public float frequency;
        [Range(0, 1)]
        public float amplitude;

        public void ColorMesh(MeshResult meshResult)
        {
            Vector3Int seed = new Vector3Int(
                MapGenerator.rng.RangeInt(0, short.MaxValue), 
                MapGenerator.rng.RangeInt(0, short.MaxValue), 
                MapGenerator.rng.RangeInt(0, short.MaxValue));

            var uvs = new Vector2[meshResult.verticesLength];
            ParallelPG.For(0, meshResult.verticesLength, 8, (band, min, max) =>
            {
                for (int i = min; i < max; i++)
                {
                    var vertex = meshResult.vertices[i];
                    var normal = meshResult.normals[i];

                    uvs[i] = GetUV(vertex, normal, seed);
                }
            });

            meshResult.mesh.uv = uvs;
            meshResult.mesh.RecalculateTangents();
        }

        public Vector2 GetUV(Vector3 vertex, Vector3 normal, Vector3Int seed)
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
            float noise = (PerlinNoise.Get((vertex + seed), baseFrequency) + 1) / 2f;
            noise += amplitude * PerlinNoise.Get(vertex + seed, frequency);

            Vector2 uv = new Vector2(angle, noise);
            return uv;
        }
    }
}

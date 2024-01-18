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

        public void ColorMesh(MeshResult meshResult, System.Random rng)
        {
            Vector3Int seed = new Vector3Int(rng.Next() % short.MaxValue, rng.Next() % short.MaxValue, rng.Next() % short.MaxValue);

            var uvs = new Vector2[meshResult.vertices.Length];
            var uvs2 = new Vector2[meshResult.vertices.Length];
            Parallel.For(0, meshResult.vertices.Length, i =>
            {
                var vertex = meshResult.vertices[i];
                var normal = meshResult.normals[i];

                uvs[i] = GetUV(vertex, normal, seed);

                //todo: better uv mapping
                double upDot = Vector3.Dot(Vector3.up, meshResult.normals[i]);

                float x = vertex.x;
                float y = vertex.z + vertex.y;
                if (upDot > 0.5f || upDot < -0.5f)
                {
                    x = (vertex.x / detailSize) % 1;
                    y = (vertex.z / detailSize) % 1;
                }
                else
                {
                    double sideDot = Vector3.Dot(Vector3.right, meshResult.normals[i]);

                    if (sideDot > 0.5f || sideDot < -0.5f)
                    {
                        x = (vertex.z/ detailSize) % 1;
                        y = (vertex.y/ detailSize) % 1;
                    }
                    else
                    {
                        x = (vertex.x/ detailSize) % 1;
                        y = (vertex.y / detailSize) % 1;
                    }
                    
                }
                
                uvs2[i] = new Vector2(x, y);
            });

            meshResult.mesh.uv = uvs;
            meshResult.mesh.uv2 = uvs2;
            meshResult.mesh.RecalculateTangents();
        }

        private Vector2 GetUV(Vector3 vertex, Vector3 normal, Vector3Int seed)
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

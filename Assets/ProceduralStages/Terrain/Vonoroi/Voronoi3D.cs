using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProceduralStages
{
    [CreateAssetMenu(fileName = "Voronoi3D", menuName = "ProceduralStages/Voronoi3D", order = 10)]
    public class Voronoi3D : ScriptableObject
    {
        public Vector3Int size;
        public Vector3Int cellCounts;

        [SerializeField]
        [HideInInspector]
        private Voronoi3DResult[] voronoi;

        [ContextMenu("Bake")]
        public void Bake()
        {
            voronoi = new Voronoi3DResult[size.x * size.y * size.z];

            Vector3 scale = new Vector3(
                cellCounts.x / (float)size.x,
                cellCounts.y / (float)size.y,
                cellCounts.z / (float)size.z);

            Vector3 scaleReciprocal = new Vector3(
                1 / scale.x,
                1 / scale.y,
                1 / scale.z);

            Parallel.For(0, size.x, x =>
            {
                for (int y = 0; y < size.y; y++)
                {
                    for (int z = 0; z < size.z; z++)
                    {
                        Vector3 uvw = new Vector3(
                            x * scale.x,
                            y * scale.y,
                            z * scale.z);

                        Vector3 uvwIntegral = new Vector3(
                            Mathf.Floor(uvw.x),
                            Mathf.Floor(uvw.y),
                            Mathf.Floor(uvw.z));

                        Vector3 uvwFractional = uvw - uvwIntegral;

                        float minDistance1 = float.MaxValue;
                        Vector3 minDistanceDisplacement1 = default;

                        float minDistance2 = float.MaxValue;
                        Vector3 minDistanceDisplacement2 = default;

                        for (int i = -1; i <= 1; i++)
                        {
                            for (int j = -1; j <= 1; j++)
                            {
                                for (int k = -1; k <= 1; k++)
                                {
                                    Vector3 neighbor = new Vector3(i, j, k);
                                    Vector3 pos = uvwIntegral + neighbor;
                                    Vector3 clampedPos = new Vector3(
                                        ((pos.x % cellCounts.x) + cellCounts.x) % cellCounts.x,
                                        ((pos.y % cellCounts.y) + cellCounts.y) % cellCounts.y,
                                        ((pos.z % cellCounts.z) + cellCounts.z) % cellCounts.z);

                                    Vector3 displacement = RandomPG.Random3(clampedPos);
                                    Vector3 diff = neighbor + displacement - uvwFractional;
                                    float dist = diff.sqrMagnitude;
                                    if (dist < minDistance1)
                                    {
                                        minDistanceDisplacement2 = minDistanceDisplacement1;
                                        minDistance2 = minDistance1;

                                        minDistanceDisplacement1 = diff;
                                        minDistance1 = dist;
                                    }
                                    else if (dist < minDistance2)
                                    {
                                        minDistanceDisplacement2 = diff;
                                        minDistance2 = dist;
                                    }
                                }
                            }
                        }

                        Vector3 displacement1 = new Vector3(
                            scaleReciprocal.x * minDistanceDisplacement1.x,
                            scaleReciprocal.y * minDistanceDisplacement1.y,
                            scaleReciprocal.z * minDistanceDisplacement1.z);

                        Vector3 displacement2 = new Vector3(
                            scaleReciprocal.x * minDistanceDisplacement2.x,
                            scaleReciprocal.y * minDistanceDisplacement2.y,
                            scaleReciprocal.z * minDistanceDisplacement2.z);

                        //https://www.youtube.com/watch?v=PMltMdi1Wzg
                        Vector3 pa = -minDistanceDisplacement1;
                        Vector3 ba = minDistanceDisplacement2 - minDistanceDisplacement1;

                        float weigth = Mathf.Clamp01(Vector3.Dot(pa, ba) / (Vector3.Dot(ba, ba)));
                        voronoi[x * size.y * size.z + y * size.z + z] = new Voronoi3DResult
                        {
                            displacement1 = displacement1,
                            displacement2 = displacement2,
                            weight = weigth
                        };
                    }
                }
            });

            Log.Debug("Voronoi baked");
        }

        public Voronoi3DResult this[int x, int y, int z]
        {
            get
            {
                x = ((x % size.x) + size.x) % size.x;
                y = ((y % size.y) + size.y) % size.y;
                z = ((z % size.z) + size.z) % size.z;

                return voronoi[x * size.y * size.z + y * size.z + z];
            }
        }
    }
}

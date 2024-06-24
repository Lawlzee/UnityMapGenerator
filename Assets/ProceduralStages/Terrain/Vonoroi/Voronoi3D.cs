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
        public Metric metric = Metric.Euclidean;

        [SerializeField]
        [HideInInspector]
        private Voronoi3DResult[] voronoi;

        [ContextMenu("BakeEuclidean")]
        public void BakeEuclidean()
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

                        
                        Vector3 pa = -minDistanceDisplacement1;
                        Vector3 ba = minDistanceDisplacement2 - minDistanceDisplacement1;

                        //https://www.youtube.com/watch?v=PMltMdi1Wzg
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

        private static readonly Vector3[] _offsets = new Vector3[]
        {
            new Vector3(1, 0, 0),
            new Vector3(0, 1, 0),
            new Vector3(0, 0, 1),
        };

        [ContextMenu("BakeChebyshev")]
        public void BakeChebyshev()
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

                        float minDistance = float.MaxValue;
                        Vector3 minDisplacement = default;
                        Vector3 minNeighborDisplacement = default;
                        Vector3 minNeighbor = default;

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
                                    Vector3 neighborDisplacement = neighbor + displacement;
                                    Vector3 diff = neighborDisplacement - uvwFractional;

                                    float dist = Mathf.Max(Mathf.Abs(diff.x), Mathf.Abs(diff.y), Mathf.Abs(diff.z));

                                    if (dist < minDistance)
                                    {
                                        minDisplacement = diff;
                                        minDistance = dist;
                                        minNeighborDisplacement = neighborDisplacement;
                                        minNeighbor = neighbor;
                                    }
                                }
                            }
                        }

                        float minWeight = float.MaxValue;
                        Vector3 minDisplacement2 = default;

                        for (int i = 0; i < 3; i++)
                        {
                            int offset = minDisplacement[i] < 0 ? 1 : -1;
                            Vector3 neighbor = offset * _offsets[i];
                            Vector3 pos = uvwIntegral + minNeighbor + neighbor;
                            Vector3 clampedPos = new Vector3(
                                ((pos.x % cellCounts.x) + cellCounts.x) % cellCounts.x,
                                ((pos.y % cellCounts.y) + cellCounts.y) % cellCounts.y,
                                ((pos.z % cellCounts.z) + cellCounts.z) % cellCounts.z);

                            Vector3 displacement = RandomPG.Random3(clampedPos);
                            Vector3 neighborDisplacement = neighbor + minNeighbor + displacement;
                            Vector3 diff = neighborDisplacement - minNeighborDisplacement;

                            int maxAxisIndex = i;
                            //int maxAxisIndex = 0;
                            //float maxAxisDistance = float.MinValue;
                            //
                            //for (int j = 0; j < 3; j++)
                            //{
                            //    float axisDistance = Mathf.Abs(diff[j]);
                            //    if (axisDistance > maxAxisDistance)
                            //    {
                            //        maxAxisIndex = j;
                            //        maxAxisDistance = axisDistance;
                            //    }
                            //}

                            //float axisMinValue = Mathf.Min(neighborDisplacement[maxAxisIndex], minNeighborDisplacement[maxAxisIndex]); 
                            //float axisMaxValue = Mathf.Max(neighborDisplacement[maxAxisIndex], minNeighborDisplacement[maxAxisIndex]); 
                            
                            float min = minDisplacement[i] < 0 ? neighborDisplacement[i] : minNeighborDisplacement[i];
                            float max = minDisplacement[i] < 0 ? minNeighborDisplacement[i] : neighborDisplacement[i];

                            float weigth = Mathf.InverseLerp(max, min, uvwFractional[maxAxisIndex]);

                            if (weigth > 0.5f)
                            {
                                weigth = 1 - weigth;
                            }

                            if (weigth < minWeight)
                            {
                                minWeight = weigth;
                                minDisplacement2 = neighborDisplacement - uvwFractional;
                            }
                        }

                        Vector3 displacement1 = new Vector3(
                            scaleReciprocal.x * minDisplacement.x,
                            scaleReciprocal.y * minDisplacement.y,
                            scaleReciprocal.z * minDisplacement.z);

                        Vector3 displacement2 = new Vector3(
                            scaleReciprocal.x * minDisplacement2.x,
                            scaleReciprocal.y * minDisplacement2.y,
                            scaleReciprocal.z * minDisplacement2.z);

                        /*
                        
                        Vector3 pa = -minDisplacement;
                        Vector3 ba = minDistanceDisplacement2 - minDisplacement;

                        float weigth;
                        if (metric == Metric.Euclidean)
                        {
                            //https://www.youtube.com/watch?v=PMltMdi1Wzg
                            minWeigth = Mathf.Clamp01(Vector3.Dot(pa, ba) / (Vector3.Dot(ba, ba)));
                        }
                        else
                        {
                            float deltaX = Mathf.Abs(ba.x);
                            float deltaY = Mathf.Abs(ba.y);
                            float deltaZ = Mathf.Abs(ba.z);

                            float deltaMax = Mathf.Max(deltaX, deltaY, deltaZ);

                            if (deltaX == deltaMax)
                            {
                                minWeigth = Mathf.Clamp01(Mathf.Abs(pa.x) / deltaX);
                            }
                            else if (deltaY == deltaMax)
                            {
                                minWeigth = Mathf.Clamp01(Mathf.Abs(pa.y) / deltaY);
                            }
                            else
                            {
                                minWeigth = Mathf.Clamp01(Mathf.Abs(pa.y) / deltaZ);
                            }

                            //float h = Mathf.Clamp01(Vector3.Dot(pa, ba) / (Vector3.Dot(ba, ba)));
                            //Vector3 q = ba.normalized * h;
                            ////weigth = Mathf.Max(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
                            //
                            //weigth = Mathf.Max(Mathf.Abs(q.x), Mathf.Abs(q.y), Mathf.Abs(q.z))
                            //    / Mathf.Max(Mathf.Abs(ba.x), Mathf.Abs(ba.y), Mathf.Abs(ba.z));
                        }
                        */
                        voronoi[x * size.y * size.z + y * size.z + z] = new Voronoi3DResult
                        {
                            displacement1 = displacement1,
                            displacement2 = displacement2,
                            weight = minWeight
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

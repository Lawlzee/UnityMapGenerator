using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProceduralStages
{
    public static class KMeans
    {
        public static int[][] Cluster(Vector3[] points, int clusterCount, int maxIterations, int seed)
        {
            int[] pointsClusterIndex = InitializeClustering(points.Length, clusterCount, seed);
            int[] pointsCentroid = new int[clusterCount];
            int[] pointCountByClusterIndex = new int[clusterCount];

            bool hasChanges = true;
            for (int i = 0; hasChanges && i < maxIterations; i++)
            {
                pointCountByClusterIndex = new int[clusterCount];
                UpdateCentroid(points, pointsClusterIndex, pointsCentroid, clusterCount, pointCountByClusterIndex);
                hasChanges = AssignPointCluster(points, pointsClusterIndex, pointsCentroid, clusterCount);
            }

            int[][] clusters = new int[clusterCount][];
            for (int i = 0; i < clusters.Length; i++)
            {
                clusters[i] = new int[pointCountByClusterIndex[i]];
            }

            int[] clustersCurrentIndex = new int[clusterCount];
            for (int i = 0; i < pointsClusterIndex.Length; i++)
            {
                clusters[pointsClusterIndex[i]][clustersCurrentIndex[pointsClusterIndex[i]]] = i;
                ++clustersCurrentIndex[pointsClusterIndex[i]];
            }

            return clusters;
        }

        private static int[] InitializeClustering(int numData, int clusterCount, int seed)
        {
            var rnd = new System.Random(seed);
            var clustering = new int[numData];

            for (int i = 0; i < numData; ++i)
            {
                clustering[i] = rnd.Next(0, clusterCount);
            }

            return clustering;
        }

        private static void UpdateCentroid(
            Vector3[] points, 
            int[] pointsClusterIndex, 
            int[] pointsCentroid, 
            int clusterCount, 
            int[] pointCountByClusterIndex)
        {
            Vector3[] means = new Vector3[clusterCount];

            for (int i = 0; i < points.Length; i++)
            {
                int clusterIdx = pointsClusterIndex[i];
                pointCountByClusterIndex[clusterIdx]++;

                means[clusterIdx] += points[i];
            }

            for (int i = 0; i < means.Length; i++)
            {
                int itemCount = pointCountByClusterIndex[i];
                means[i] /= itemCount > 0 ? itemCount : 1;
            }

            float[] minDistances = new float[clusterCount];
            for (int i = 0; i < clusterCount; i++)
            {
                minDistances[i] = float.MaxValue;
            }

            for (int i = 0; i < points.Length; i++)
            {
                int clusterIndex = pointsClusterIndex[i];
                float distance = (points[i] - means[clusterIndex]).sqrMagnitude;
                if (distance < minDistances[clusterIndex])
                {
                    minDistances[clusterIndex] = distance;
                    pointsCentroid[clusterIndex] = i;
                }
            }
        }

        private static bool AssignPointCluster(
            Vector3[] points, 
            int[] pointsClusterIndex, 
            int[] pointsCentroid, 
            int clusterCount)
        {
            bool changed = false;

            for (int i = 0; i < points.Length; i++)
            {
                float minDistance = float.MaxValue;
                int minClusterIndex = -1;

                for (int k = 0; k < clusterCount; k++)
                {
                    float distance = (points[i] - points[pointsCentroid[k]]).sqrMagnitude;
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        minClusterIndex = k;
                    }
                }

                if (pointsClusterIndex[i] != minClusterIndex)
                {
                    changed = true;
                    pointsClusterIndex[i] = minClusterIndex;
                }
            }

            return changed;
        }
    }
}

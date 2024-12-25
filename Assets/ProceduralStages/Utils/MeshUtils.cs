using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Rendering;
using UnityEngine;

namespace ProceduralStages
{
    public static class MeshUtils
    {
        //https://gamedev.stackexchange.com/questions/165643/how-to-calculate-the-surface-area-of-a-mesh
        public static float GetSurfaceArea(this Mesh mesh)
        {
            var triangles = mesh.triangles;
            var vertices = mesh.vertices;

            float sum = 0f;

            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 corner = vertices[triangles[i]];
                Vector3 a = vertices[triangles[i + 1]] - corner;
                Vector3 b = vertices[triangles[i + 2]] - corner;

                sum += Vector3.Cross(a, b).magnitude;
            }

            return 0.5f * sum;
        }

        public static Mesh CreateMeshWithDensity(
            Vector3[] vertices,
            Vector3[] normals,
            int[] triangles,
            float splitDensity,
            float trimDensity,
            Bounds bounds)
        {
            float sqrSplitDensity = splitDensity * splitDensity;
            float sqrTrimDensity = trimDensity * trimDensity;

            List<Vector3> resultVertices = new List<Vector3>(vertices);
            List<Vector3> resultNormals = new List<Vector3>(normals);
            List<int> resultTriangles = new List<int>(triangles);
            HashSet<int> deadTriangles = new HashSet<int>();

            bool found = true;

            while (found)
            {
                found = false;

                for (int i = 0; i < resultTriangles.Count; i += 3)
                {
                    if (deadTriangles.Contains(i))
                    {
                        continue;
                    }

                    int t0 = resultTriangles[i];
                    int t1 = resultTriangles[i + 1];
                    int t2 = resultTriangles[i + 2];

                    Vector3 v0 = resultVertices[t0];
                    Vector3 v1 = resultVertices[t1];
                    Vector3 v2 = resultVertices[t2];

                    Vector3 a = v1 - v0;
                    Vector3 b = v2 - v0;

                    float triangleArea = 0.25f * Vector3.Cross(a, b).sqrMagnitude;
                    if (triangleArea < sqrTrimDensity)
                    {
                        deadTriangles.Add(i);
                    }
                    else if (bounds.Contains(v0) && triangleArea > sqrSplitDensity)
                    {
                        found = true;
                        deadTriangles.Add(i);

                        int t3 = resultVertices.Count;

                        float sideLength01 = (v0 - v1).sqrMagnitude;
                        float sideLength02 = (v0 - v2).sqrMagnitude;
                        float sideLength12 = (v1 - v2).sqrMagnitude;

                        Vector3 v3;
                        Vector3 normal;

                        int i0;
                        int i1;
                        int i2;

                        if (sideLength01 > sideLength02)
                        {
                            //0 - 1
                            if (sideLength01 > sideLength12)
                            {
                                i0 = t0;
                                i1 = t1;
                                i2 = t2;
                            }
                            //1 - 2
                            else
                            {
                                i0 = t1;
                                i1 = t2;
                                i2 = t0;
                            }
                        }
                        else
                        {
                            //0 - 2
                            if (sideLength02 > sideLength12)
                            {
                                i0 = t2;
                                i1 = t0;
                                i2 = t1;
                            }
                            //1 - 2
                            else
                            {
                                i0 = t1;
                                i1 = t2;
                                i2 = t0;
                            }
                        }

                        v3 = (resultVertices[i0] + resultVertices[i1]) * 0.5f;
                        resultVertices.Add(v3);

                        normal = (resultNormals[i0] + resultNormals[i1]) * 0.5f;
                        resultNormals.Add(normal);

                        resultTriangles.Add(i0);
                        resultTriangles.Add(t3);
                        resultTriangles.Add(i2);

                        resultTriangles.Add(i1);
                        resultTriangles.Add(i2);
                        resultTriangles.Add(t3);

                        //Vector3 v3 = (v0 + v1 + v2) / 3;
                        //resultVertices.Add(v3);
                        //
                        //Vector3 normal = (resultNormals[t0] + resultNormals[t1] + resultNormals[t2]) / 3;
                        //resultNormals.Add(normal);
                        //
                        //resultTriangles.Add(t0);
                        //resultTriangles.Add(t1);
                        //resultTriangles.Add(t3);
                        //
                        //resultTriangles.Add(t2);
                        //resultTriangles.Add(t3);
                        //resultTriangles.Add(t1);
                        //
                        //resultTriangles.Add(t0);
                        //resultTriangles.Add(t3);
                        //resultTriangles.Add(t2);
                    }
                }
            }

            int[] trimmedTriangles = new int[resultTriangles.Count - deadTriangles.Count * 3];
            bool[] usedVertices = new bool[resultVertices.Count];
            int vertexCount = 0;

            int newIndex = 0;
            for (int i = 0; i < resultTriangles.Count; i += 3)
            {
                if (!deadTriangles.Contains(i))
                {
                    int t0 = resultTriangles[i];
                    int t1 = resultTriangles[i + 1];
                    int t2 = resultTriangles[i + 2];

                    if (!usedVertices[t0])
                    {
                        usedVertices[t0] = true;
                        vertexCount++;
                    }

                    if (!usedVertices[t1])
                    {
                        usedVertices[t1] = true;
                        vertexCount++;
                    }

                    if (!usedVertices[t2])
                    {
                        usedVertices[t2] = true;
                        vertexCount++;
                    }

                    trimmedTriangles[newIndex] = t0;
                    trimmedTriangles[newIndex + 1] = t1;
                    trimmedTriangles[newIndex + 2] = t2;

                    newIndex += 3;
                }
            }

            Vector3[] trimmedVertices = new Vector3[vertexCount];
            Vector3[] trimmedNormals = new Vector3[vertexCount];
            int[] vertexReIndex = new int[resultVertices.Count];

            int vertexIndex = 0;
            for (int i = 0; i < resultVertices.Count; i++)
            {
                if (usedVertices[i])
                {
                    trimmedVertices[vertexIndex] = resultVertices[i];
                    trimmedNormals[vertexIndex] = resultNormals[i];
                    vertexReIndex[i] = vertexIndex;

                    vertexIndex++;
                }
            }

            for (int i = 0; i < trimmedTriangles.Length; i++)
            {
                trimmedTriangles[i] = vertexReIndex[trimmedTriangles[i]];
            }

            Mesh mesh = new Mesh();
            if (trimmedTriangles.Length > ushort.MaxValue) 
            { 
                mesh.indexFormat = IndexFormat.UInt32;
            }
            mesh.SetVertices(trimmedVertices);
            mesh.SetNormals(trimmedNormals);
            mesh.SetTriangles(trimmedTriangles, 0);

            mesh.RecalculateBounds();

            return mesh;
        }
    }
}

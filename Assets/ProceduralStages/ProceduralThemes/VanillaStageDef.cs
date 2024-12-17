using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProceduralStages
{
    [CreateAssetMenu(fileName = "VanillaStageDef", menuName = "ProceduralStages/VanillaStageDef", order = 1)]
    public class VanillaStageDef : ScriptableObject
    {
        public string sceneName;
        public string[] gameObjectsToDisable;
        public TerrainRendererDef[] terrainRenderers;
        public string[] terrainMeshColliders;
        public string sceneInfo;
        public float minFloorAngle = 0.35f;

        public Mesh floorMesh;
        public Mesh ceilMesh;
        public Bounds mapBounds;

        public void DisableProps()
        {
            foreach (string path in gameObjectsToDisable)
            {
                foreach (Transform tranform in FindMany(path))
                {
                    tranform.gameObject.SetActive(false);
                }
            }
        }

        public void ApplyTerrainMaterial(Material terrainMaterial, MaterialInfo materialInfo)
        {
            SurfaceDef surfaceDef = materialInfo.floorTexture.surfaceDef;

            foreach (TerrainRendererDef terrainRendererDef in terrainRenderers)
            {
                foreach (Transform tranform in FindMany(terrainRendererDef.path))
                {
                    Renderer renderer = tranform.GetComponent<Renderer>();
                    renderer.material = new Material(terrainMaterial);

                    materialInfo.ApplyTo(renderer.material, terrainRendererDef.useUV);

                    SurfaceDefProvider surfaceDefProvider = tranform.GetComponent<SurfaceDefProvider>();
                    if (surfaceDef != null && surfaceDefProvider != null)
                    {
                        surfaceDefProvider.surfaceDef = surfaceDef;
                    }
                }
            }
        }

        private IEnumerable<Transform> FindMany(string path)
        {
            GameObject gameObject = GameObject.Find(path);
            if (gameObject != null)
            {
                var parent = gameObject.transform.parent;
                if (parent == null)
                {
                    GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();

                    for (int i = 0; i < rootObjects.Length; i++)
                    {
                        GameObject child = rootObjects[i];
                        if (child.name == gameObject.name)
                        {
                            yield return child.transform;
                        }
                    }
                }
                else
                {
                    int siblingCount = gameObject.transform.parent.childCount;

                    for (int i = 0; i < siblingCount; i++)
                    {
                        Transform child = parent.GetChild(i);
                        if (child.name == gameObject.name)
                        {
                            yield return child;
                        }
                    }
                }

            }
            else
            {
                Log.Warning($"GameObject {path} not found");
            }
        }

#if UNITY_EDITOR

        [ContextMenu("Bake map size")]
        public void BakeMapSize()
        {
            var info = GameObject.Find(sceneInfo).GetComponent<SceneInfo>();
            var groundNodes = info.groundNodesAsset.nodes;
            var airNodes = info.airNodesAsset.nodes;

            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            for (int i = 0; i < groundNodes.Length; i++)
            {
                var pos = groundNodes[i].position;

                min.x = Mathf.Min(min.x, pos.x);
                min.y = Mathf.Min(min.y, pos.y);
                min.z = Mathf.Min(min.z, pos.z);

                max.x = Mathf.Max(max.x, pos.x);
                max.y = Mathf.Max(max.y, pos.y);
                max.z = Mathf.Max(max.z, pos.z);
            }

            for (int i = 0; i < airNodes.Length; i++)
            {
                var pos = airNodes[i].position;

                min.x = Mathf.Min(min.x, pos.x);
                min.y = Mathf.Min(min.y, pos.y);
                min.z = Mathf.Min(min.z, pos.z);

                max.x = Mathf.Max(max.x, pos.x);
                max.y = Mathf.Max(max.y, pos.y);
                max.z = Mathf.Max(max.z, pos.z);
            }

            mapBounds = new Bounds((min + max) / 2, max - min);

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private struct MeshInfo
        {
            public Matrix4x4 localToWorldMatrix;
            public int[] triangles;
            public Vector3[] normals;
            public Vector3[] vertices;
        }

        private struct MeshState
        {
            public Vector3[] normals;
            public Vector3[] vertices;

            public int triangleCount;
            public int[] triangles;
            public int vertexCount;
        }

        [ContextMenu("Bake Graph Meshes")]
        public void BakeGraphs()
        {
            ProfilerLog.Reset();
            using (ProfilerLog.CreateScope("total"))
            {
                List<MeshInfo> meshes = new List<MeshInfo>();

                foreach (string path in terrainMeshColliders)
                {
                    foreach (Transform tranform in FindMany(path))
                    {
                        Mesh mesh = tranform.GetComponent<MeshFilter>().sharedMesh;
                        meshes.Add(new MeshInfo
                        {
                            localToWorldMatrix = tranform.localToWorldMatrix,
                            vertices = mesh.vertices,
                            triangles = mesh.triangles,
                            normals = mesh.normals
                        });
                    }
                }

                ProfilerLog.Info($"{meshes.Count} meshes found");

                MeshState[] floorStates = new MeshState[meshes.Count];
                MeshState[] ceilStates = new MeshState[meshes.Count];

                Parallel.For(0, meshes.Count, i =>
                {
                    MeshInfo meshInfo = meshes[i];

                    ref MeshState floorState = ref floorStates[i];
                    ref MeshState ceilState = ref ceilStates[i];

                    Matrix4x4 normalMatrix = meshInfo.localToWorldMatrix.inverse.transpose;

                    int[] triangles = meshInfo.triangles;
                    Vector3[] vertices = meshInfo.vertices;
                    Vector3[] normals = meshInfo.normals;

                    int floorVertexCount = 0;
                    int ceilVertexCount = 0;

                    Vector3[] floorNormals = new Vector3[normals.Length];
                    Vector3[] ceilNormals = new Vector3[normals.Length];

                    Vector3[] floorVertices = new Vector3[vertices.Length];
                    Vector3[] ceilVertices = new Vector3[vertices.Length];

                    int[] floorReIndex = new int[vertices.Length];
                    int[] ceilReIndex = new int[vertices.Length];

                    for (int j = 0; j < vertices.Length; j++)
                    {
                        Vector3 worldVertex = meshInfo.localToWorldMatrix.MultiplyPoint3x4(vertices[j]);
                        Vector3 worldNormal = normalMatrix.MultiplyVector(normals[j]).normalized;

                        float angle = Vector3.Dot(Vector3.up, worldNormal);
                        if (angle > minFloorAngle)
                        {
                            floorReIndex[j] = floorVertexCount;
                            floorNormals[floorVertexCount] = worldNormal;
                            floorVertices[floorVertexCount] = worldVertex;
                            floorVertexCount++;
                        }
                        else
                        {
                            floorReIndex[j] = -1;
                        }


                        if (angle < -minFloorAngle)
                        {
                            ceilReIndex[j] = ceilVertexCount;
                            ceilNormals[ceilVertexCount] = worldNormal;
                            ceilVertices[ceilVertexCount] = worldVertex;
                            ceilVertexCount++;
                        }
                        else
                        {
                            ceilReIndex[j] = -1;
                        }
                    }

                    int[] floorTriangles = new int[triangles.Length];
                    int[] ceilTriangles = new int[triangles.Length];

                    int floorTriangleCount = 0;
                    int ceilTriangleCount = 0;

                    for (int j = 0; j < triangles.Length; j += 3)
                    {
                        int a = floorReIndex[triangles[j]];
                        int b = floorReIndex[triangles[j + 1]];
                        int c = floorReIndex[triangles[j + 2]];

                        if (a != -1 && b != -1 && c != -1)
                        {
                            floorTriangles[floorTriangleCount] = a;
                            floorTriangles[floorTriangleCount + 1] = b;
                            floorTriangles[floorTriangleCount + 2] = c;
                            floorTriangleCount += 3;
                        }

                        int d = ceilReIndex[triangles[j]];
                        int e = ceilReIndex[triangles[j + 1]];
                        int f = ceilReIndex[triangles[j + 2]];

                        if (d != -1 && e != -1 && f != -1)
                        {
                            ceilTriangles[ceilTriangleCount] = d;
                            ceilTriangles[ceilTriangleCount + 1] = e;
                            ceilTriangles[ceilTriangleCount + 2] = f;
                            ceilTriangleCount += 3;
                        }
                    }

                    floorState.vertices = floorVertices;
                    floorState.normals = floorNormals;
                    floorState.triangleCount = floorTriangleCount;
                    floorState.triangles = floorTriangles;
                    floorState.vertexCount = floorVertexCount;

                    ceilState.vertices = ceilVertices;
                    ceilState.normals = ceilNormals;
                    ceilState.triangleCount = ceilTriangleCount;
                    ceilState.triangles = ceilTriangles;
                    ceilState.vertexCount = ceilVertexCount;

                });

                ProfilerLog.Info($"floor/ceil meshes computed");

                MeshState[] resulState = new MeshState[2];

                Parallel.For(0, 2, index =>
                {
                    MeshState[] states = index == 0
                        ? floorStates
                        : ceilStates;

                    int triangleCount = states.Sum(x => x.triangleCount);
                    int vertexCount = states.Sum(x => x.vertexCount);

                    int[] triangles = new int[triangleCount];
                    Vector3[] vertices = new Vector3[vertexCount];
                    Vector3[] normals = new Vector3[vertexCount];

                    int triangleIndex = 0;
                    int vertexIndex = 0;

                    for (int i = 0; i < states.Length; i++)
                    {
                        MeshState state = states[i];

                        for (int j = 0; j < state.triangleCount; j++)
                        {
                            triangles[triangleIndex + j] = vertexIndex + state.triangles[j];
                        }

                        Array.Copy(state.vertices, 0, vertices, vertexIndex, state.vertexCount);
                        Array.Copy(state.normals, 0, normals, vertexIndex, state.vertexCount);

                        triangleIndex += state.triangleCount;
                        vertexIndex += state.vertexCount;
                    }

                    resulState[index] = new MeshState
                    {
                        triangles = triangles,
                        vertices = vertices,
                        normals = normals
                    };
                });

                ProfilerLog.Info($"final floor/ceil meshes computed");

                Mesh floorMesh = new Mesh
                {
                    vertices = resulState[0].vertices,
                    normals = resulState[0].normals,
                    triangles = resulState[0].triangles
                };

                floorMesh.RecalculateBounds();

                SaveMeshToFile(floorMesh, "FloorMesh.asset");
                ProfilerLog.Info($"Floor mesh saved. (vertices: {resulState[0].vertices.Length}, triangles: {resulState[0].triangles.Length})");

                Mesh ceilMesh = new Mesh
                {
                    vertices = resulState[1].vertices,
                    normals = resulState[1].normals,
                    triangles = resulState[1].triangles
                };

                ceilMesh.RecalculateBounds();

                SaveMeshToFile(ceilMesh, "CeilMesh.asset");
                ProfilerLog.Info($"Ceil mesh saved. (vertices: {resulState[1].vertices.Length}, triangles: {resulState[1].triangles.Length})");

                this.floorMesh = floorMesh;
                this.ceilMesh = ceilMesh;
                EditorUtility.SetDirty(this);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private void SaveMeshToFile(Mesh mesh, string fileName)
        {
            string path = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(this)), fileName);
            AssetDatabase.CreateAsset(mesh, path);

            Debug.Log("Mesh saved to " + path);
        }
#endif
    }
}

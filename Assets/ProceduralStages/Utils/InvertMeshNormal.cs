using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.XR;

namespace ProceduralStages
{
    public class InvertMeshNormal : MonoBehaviour
    {
        public MeshFilter meshFilter;

        public void Awake()
        {

        }

#if UNITY_EDITOR
        [MenuItem("Tools/Invert Normals")]
        private static void InvertMeshNormals()
        {
            // Get the selected object in the editor
            if (Selection.activeGameObject != null)
            {
                MeshFilter meshFilter = Selection.activeGameObject.GetComponent<MeshFilter>();

                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    Mesh originalMesh = meshFilter.sharedMesh;
                    Mesh invertedMesh = new Mesh();
                    invertedMesh.name = originalMesh.name + "_Inverted";

                    // Copy vertices, UVs, and other attributes
                    invertedMesh.vertices = originalMesh.vertices;
                    invertedMesh.uv = originalMesh.uv;
                    invertedMesh.uv2 = originalMesh.uv2;
                    invertedMesh.colors = originalMesh.colors;
                    invertedMesh.tangents = originalMesh.tangents;

                    // Invert normals
                    Vector3[] normals = originalMesh.normals;
                    for (int i = 0; i < normals.Length; i++)
                    {
                        normals[i] = -normals[i];
                    }
                    invertedMesh.normals = normals;

                    // Copy triangles and reverse winding order
                    for (int i = 0; i < originalMesh.subMeshCount; i++)
                    {
                        int[] triangles = originalMesh.GetTriangles(i);
                        System.Array.Reverse(triangles);
                        invertedMesh.SetTriangles(triangles, i);
                    }

                    // Save the new inverted mesh as an asset
                    string path = "Assets/" + invertedMesh.name + ".asset";
                    AssetDatabase.CreateAsset(invertedMesh, path);
                    AssetDatabase.SaveAssets();

                    Debug.Log("Inverted mesh saved as: " + path);
                }
                else
                {
                    Debug.LogError("Selected object does not have a MeshFilter with a valid mesh.");
                }
            }
            else
            {
                Debug.LogError("No object selected. Please select an object with a MeshFilter component.");
            }
        }
#endif
    }
}

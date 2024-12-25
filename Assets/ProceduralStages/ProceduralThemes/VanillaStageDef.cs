using RoR2;
using RoR2.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace ProceduralStages
{
    [CreateAssetMenu(fileName = "VanillaStageDef", menuName = "ProceduralStages/VanillaStageDef", order = 1)]
    public class VanillaStageDef : ScriptableObject
    {
        public string sceneName;
        public string assetKey;
        public VanillaStageDefConfig config;
        public string nameToken;
        public int propKindCount = 12;
        public float propCeillingWeight = 0.5f;
        public float propCountWeight = 1f;
        public string[] gameObjectsToDisable;
        public string[] gameObjectsToEnable;
        public string[] meshesToColor;
        public TerrainMeshGateDef[] terrainMeshes;
        public string sceneInfo = "SceneInfo";

        public float meshSplitDensity = -1;
        public float meshTrimDensity = -1;
        public Bounds mapBounds;

        public void DisableProps()
        {
            foreach (string path in gameObjectsToDisable)
            {
                foreach (GameObject gameObject in GameObjectUtils.FindMany(path))
                {
                    gameObject.SetActive(false);
                }
            }

            foreach (string path in gameObjectsToEnable)
            {
                foreach (GameObject gameObject in GameObjectUtils.FindMany(path))
                {
                    gameObject.SetActive(true);
                }
            }
        }

        public void ApplyTerrainMaterial(
            Material terrainMaterial,
            MaterialInfo materialInfo,
            MeshColorer meshColorer,
            Xoroshiro128Plus rng)
        {
            SurfaceDef surfaceDef = materialInfo.floorTexture.surfaceDef;

            List<string> paths = meshesToColor
                .Concat(terrainMeshes
                    .SelectMany(x => x.paths))
                .ToList();

            foreach (var path in paths)
            {
                foreach (GameObject gameObject in GameObjectUtils.FindMany(path))
                {
                    if (!gameObject.TryGetComponent(out Renderer renderer))
                    {
                        continue;
                    }
                    renderer.material = new Material(terrainMaterial);

                    materialInfo.ApplyTo(renderer.material);

                    SurfaceDefProvider surfaceDefProvider = gameObject.GetComponent<SurfaceDefProvider>();
                    if (surfaceDef != null && surfaceDefProvider != null)
                    {
                        surfaceDefProvider.surfaceDef = surfaceDef;
                    }

                    MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
                    Mesh baseMesh = meshFilter.mesh.isReadable
                        ? meshFilter.mesh
                        : gameObject.TryGetComponent(out MeshCollider meshCollider) && meshCollider.sharedMesh.isReadable
                            ? meshCollider.sharedMesh
                            : config.meshTransformer.CreateReadableCopy(meshFilter.mesh);

                    Mesh mesh = Instantiate(baseMesh);

                    meshColorer.ColorMesh(
                        new MeshResult
                        {
                            mesh = mesh,
                            vertices = mesh.vertices,
                            normals = mesh.normals,
                            verticesLength = mesh.vertexCount
                        },
                        gameObject.transform.localToWorldMatrix,
                        rng);

                    meshFilter.sharedMesh = mesh;
                }
            }
        }

        public (Mesh floorMesh, Mesh ceilMesh) CreateMeshes()
        {
            NodeGraph nodeGraph = sceneInfo == ""
                ? null
                : GameObject.Find(sceneInfo).GetComponent<SceneInfo>().groundNodes;

            List<Mesh> floorMeshes = new List<Mesh>();
            List<Mesh> ceilMeshes = new List<Mesh>();

            foreach (var terrainMesh in terrainMeshes)
            {
                if (!terrainMesh.floorMesh)
                {
                    continue;
                }

                if (terrainMesh.gateName == ""
                    || nodeGraph == null
                    || nodeGraph.IsGateOpen(terrainMesh.gateName))
                {
                    floorMeshes.Add(terrainMesh.floorMesh);
                    ceilMeshes.Add(terrainMesh.ceilMesh);
                }
            }

            if (floorMeshes.Count == 0)
            {
                throw new Exception("No mesh were found");
            }
            else if (floorMeshes.Count == 1)
            {
                return (floorMeshes[0], ceilMeshes[0]);
            }

            int floorVertexCount = 0;
            CombineInstance[] combine = new CombineInstance[floorMeshes.Count];
            for (int i = 0; i < floorMeshes.Count; i++)
            {
                combine[i].mesh = floorMeshes[i];
                combine[i].transform = Matrix4x4.identity;

                floorVertexCount += floorMeshes[i].vertexCount;
            }

            Mesh floorMesh = new Mesh();
            floorMesh.indexFormat = floorVertexCount <= ushort.MaxValue
                ? IndexFormat.UInt16
                : IndexFormat.UInt32;
            floorMesh.CombineMeshes(combine);

            int ceilVertexCount = 0;
            for (int i = 0; i < ceilMeshes.Count; i++)
            {
                combine[i].mesh = ceilMeshes[i];
                ceilVertexCount += ceilMeshes[i].vertexCount;
            }

            Mesh ceilMesh = new Mesh();
            ceilMesh.indexFormat = ceilVertexCount <= ushort.MaxValue
                ? IndexFormat.UInt16
                : IndexFormat.UInt32;
            ceilMesh.CombineMeshes(combine);

            return (floorMesh, ceilMesh);
        }

#if UNITY_EDITOR

        [ContextMenu("Load Scene")]
        public void LoadScene()
        {
            Addressables.LoadSceneAsync(assetKey).WaitForCompletion();
        }

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

        [ContextMenu("Bake Graph Meshes")]
        public void BakeGraphs()
        {
            if (meshSplitDensity < 0)
            {
                meshSplitDensity = config.meshSplitDensity;
            }

            if (meshTrimDensity < 0)
            {
                meshTrimDensity = config.meshTrimDensity;
            }

            foreach (TerrainMeshGateDef terrainMeshDef in terrainMeshes)
            {
                terrainMeshDef.BakeGraphs(this);
            }

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [ContextMenu("Bake Gate Names")]
        public void BakeGateNames()
        {
            //SceneManager.GetActiveScene()
            //    .GetRootGameObjects()
            //    .SelectMany(x => x.)

            GateStateSetter[] gateStateSetters = FindObjectsByType<GateStateSetter>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            HashSet<string> gateNames = new HashSet<string>()
            {
                ""
            };

            for (int i = 0; i < gateStateSetters.Length; i++)
            {
                gateNames.Add(gateStateSetters[i].gateToEnableWhenEnabled);
                gateNames.Add(gateStateSetters[i].gateToDisableWhenEnabled);
            }

            //NodeGraph nodeGraph = GameObject.Find(sceneInfo).GetComponent<SceneInfo>().groundNodes;
            List<TerrainMeshGateDef> newDefs = new List<TerrainMeshGateDef>();

            foreach (var gateName in gateNames)
            {
                TerrainMeshGateDef def = terrainMeshes.FirstOrDefault(x => gateName == x.gateName);
                if (def == null)
                {
                    def = new TerrainMeshGateDef
                    {
                        gateName = gateName,
                        paths = new string[0]
                    };
                }

                newDefs.Add(def);
            }

            terrainMeshes = newDefs.ToArray();

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [ContextMenu("Bake Props Ceil Weights")]
        public void BakePropWeights()
        {
            int ceilVertexCount = terrainMeshes
                .Where(x => x.ceilMesh)
                .Select(x => x.ceilMesh.vertexCount)
                .Sum();

            int floorVertexCount = terrainMeshes
                .Where(x => x.floorMesh)
                .Select(x => x.floorMesh.vertexCount)
                .Sum();

            propCeillingWeight = ceilVertexCount / (float)floorVertexCount;

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
#endif
    }
}

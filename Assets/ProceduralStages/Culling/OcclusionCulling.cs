using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProceduralStages
{
    public class OcclusionCulling : MonoBehaviour
    {
        private static readonly Vector3Int[] _verticesIndexes = {
            new Vector3Int (0, 0, 0),
            new Vector3Int (1, 0, 0),
            new Vector3Int (1, 1, 0),
            new Vector3Int (0, 1, 0),
            new Vector3Int (0, 1, 1),
            new Vector3Int (1, 1, 1),
            new Vector3Int (1, 0, 1),
            new Vector3Int (0, 0, 1),
        };

        private static readonly int[] _trianglesIndexes = {
            0, 2, 1,
            0, 3, 2,
            2, 3, 4,
            2, 4, 5,
            1, 2, 5,
            1, 5, 6,
            0, 7, 4,
            0, 4, 3,
            5, 4, 7,
            5, 7, 6,
            0, 6, 7,
            0, 1, 6
        };

        [Range(1, 50)]
        public int updateFrameDelay;

        public int clusterCount = 100;
        public int clusterMaxIterations = 100;

        public bool debug;

        private ComputeBuffer _visibleClustersBuffer;

        private MeshRenderer[][] _meshRenderersByClusterIndex;
        private int[] _visibleClusters;

        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;

        public void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
        }

        private Bounds GetBounds(MeshRenderer[] meshRenderers)
        {
            Bounds bounds = meshRenderers[0].bounds;

            for (int i = 1; i < meshRenderers.Length; i++)
            {
                Bounds bound = meshRenderers[i].bounds;
                bounds.Encapsulate(bound);
            }

            return bounds;
        }

        private Mesh GenerateMesh(Bounds[] bounds)
        {
            int[] triangles = new int[bounds.Length * _trianglesIndexes.Length];
            Vector3[] vertices = new Vector3[bounds.Length * _verticesIndexes.Length];

            Vector3[] pos = new Vector3[2];

            for (int i = 0; i < bounds.Length; i++)
            {
                for (int j = 0; j < _trianglesIndexes.Length; j++)
                {
                    triangles[i * _trianglesIndexes.Length + j] = _verticesIndexes.Length * i + _trianglesIndexes[j];
                }

                pos[0] = bounds[i].min;
                pos[1] = bounds[i].max;

                for (int j = 0; j < _verticesIndexes.Length; j++)
                {
                    Vector3Int ix = _verticesIndexes[j];
                    vertices[i * _verticesIndexes.Length + j] = new Vector3(pos[ix.x].x, pos[ix.y].y, pos[ix.z].z);
                }
            }

            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = triangles;

            return mesh;
        }

        public void SetTargets(List<GameObject> gameObjects)
        {
            MeshRenderer[][] meshRenderers = new MeshRenderer[gameObjects.Count][];
            Bounds[] bounds = new Bounds[gameObjects.Count];
            Vector3[] boundsCenter = new Vector3[gameObjects.Count];

            for (int i = 0; i < gameObjects.Count; i++)
            {
                GameObject gameObject = gameObjects[i];
                MeshRenderer[] renderers = gameObject.GetComponentsInChildren<MeshRenderer>();
                Bounds bound = GetBounds(renderers);

                meshRenderers[i] = renderers;
                bounds[i] = bound;
                boundsCenter[i] = bound.center;
            }

            int[][] clusters = KMeans.Cluster(boundsCenter, clusterCount, clusterMaxIterations, 0);

            Bounds[] boundsByCluster = new Bounds[clusterCount];
            _meshRenderersByClusterIndex = new MeshRenderer[clusterCount][];

            for (int i = 0; i < clusterCount; i++)
            {
                int[] cluster = clusters[i];

                int rendererCount = 0;
                for (int j = 0; j < cluster.Length; j++)
                {
                    rendererCount += meshRenderers[cluster[j]].Length;
                }

                MeshRenderer[] clusterRenderers = new MeshRenderer[rendererCount];

                Bounds clusterBounds = default;

                int rendererIndex = 0;
                for (int j = 0; j < cluster.Length; j++)
                {
                    int index = cluster[j];

                    if (j == 0)
                    {
                        clusterBounds = bounds[index];
                    }
                    else
                    {
                        clusterBounds.Encapsulate(bounds[index]);
                    }

                    var renderers = meshRenderers[index];

                    for (int k = 0; k < renderers.Length; k++)
                    {
                        clusterRenderers[rendererIndex] = renderers[k];
                        rendererIndex++;
                    }
                }

                boundsByCluster[i] = clusterBounds;
                _meshRenderersByClusterIndex[i] = clusterRenderers;
            }

            _meshFilter.mesh = GenerateMesh(boundsByCluster);

            _visibleClusters = new int[clusterCount];
            _visibleClustersBuffer = new ComputeBuffer(clusterCount, 4, ComputeBufferType.Default);

            Graphics.ClearRandomWriteTargets();
            Graphics.SetRandomWriteTarget(1, _visibleClustersBuffer, false);

            _meshRenderer.material.SetBuffer("_VisibleClusters", _visibleClustersBuffer);
            _meshRenderer.material.SetInt("_Debug", Convert.ToInt32(debug));

            enabled = true;
        }

        public void Update()
        {
            if (Time.frameCount % updateFrameDelay != 0)
            {
                return;
            }

            _visibleClustersBuffer.GetData(_visibleClusters);

            for (int i = 0; i < _meshRenderersByClusterIndex.Length; i++)
            {
                bool visible = _visibleClusters[i] != 0;
                _visibleClusters[i] = 0;

                for (int j = 0; j < _meshRenderersByClusterIndex[i].Length; j++)
                {
                    _meshRenderersByClusterIndex[i][j].enabled = visible;
                }
            }

            _visibleClustersBuffer.SetData(_visibleClusters);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProceduralStages
{
    public class OcclusionCulling : MonoBehaviour
    {
        private struct Index4
        {
            public short value0;
            public short value1;
            public short value2;
            public short value3;

            public short this[int index]
            {
                get
                {
                    switch (index)
                    {
                        case 0:
                            return value0;
                        case 1:
                            return value1;
                        case 2:
                            return value2;
                        case 3:
                            return value3;
                    }

                    return -1;
                }
                set
                {
                    switch (index)
                    {
                        case 0:
                            value0 = value;
                            break;
                        case 1:
                            value1 = value;
                            break;
                        case 2:
                            value2 = value;
                            break;
                        case 3:
                            value3 = value;
                            break;
                    }
                }
            }
        }

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

        [Range(1, 60)]
        public int updateFrameDelay;

        public int clusterCount = 100;
        public int clusterMaxIterations = 100;

        public bool debug;
        public Vector3 boundsBuffer;

        private ComputeBuffer _visibleClustersBuffer;

        private MeshRenderer[][] _meshRenderersByClusterIndex;
        private uint[] _visibleClusters;

        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;

        private Index4[,,] _clusterIndexByPos;
        private Vector3Int _clustersLength;

        public float cellSize = 1;
        private float cellSizeReciprocal;

        public void Awake()
        {
            if (!Application.isEditor)
            {
                updateFrameDelay = ModConfig.OcclusionCullingDelay.Value;
            }

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

            bounds.size += boundsBuffer;

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

        public void SetTargets(List<GameObject> gameObjects, Vector3 mapSize)
        {
            using (ProfilerLog.CreateScope("OcclusionCulling.SetTargets"))
            {
                cellSizeReciprocal = 1 / cellSize;

                MeshRenderer[][] meshRenderers = new MeshRenderer[gameObjects.Count][];
                Bounds[] bounds = new Bounds[gameObjects.Count];
                Vector3[] boundsCenter = new Vector3[gameObjects.Count];

                for (int i = 0; i < gameObjects.Count; i++)
                {
                    GameObject gameObject = gameObjects[i];
                    MeshRenderer[] renderers = gameObject.GetComponentsInChildren<MeshRenderer>();
                    if (renderers.Length == 0)
                    {
                        meshRenderers[i] = renderers;
                        continue;
                    }

                    Bounds bound = GetBounds(renderers);

                    meshRenderers[i] = renderers;
                    bounds[i] = bound;
                    boundsCenter[i] = bound.center;
                }

                ProfilerLog.Debug("bounds");

                int[][] clusters = KMeans.Cluster(boundsCenter, clusterCount, clusterMaxIterations, 0);
                ProfilerLog.Debug("KMeans");

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

                ProfilerLog.Debug("Encapsulate");

                int sizeX = Mathf.CeilToInt(mapSize.x * cellSizeReciprocal);
                int sizeY = Mathf.CeilToInt(mapSize.y * cellSizeReciprocal);
                int sizeZ = Mathf.CeilToInt(mapSize.z * cellSizeReciprocal);

                _clustersLength = new Vector3Int(sizeX, sizeY, sizeZ);
                _clusterIndexByPos = new Index4[sizeX, sizeY, sizeZ];

                ProfilerLog.Debug("Index4 0");

                Parallel.For(0, sizeX, x =>
                {
                    for (int y = 0; y < sizeY; y++)
                    {
                        for (int z = 0; z < sizeZ; z++)
                        {
                            _clusterIndexByPos[x, y, z] = new Index4
                            {
                                value0 = -1,
                                value1 = -1,
                                value2 = -1,
                                value3 = -1
                            };
                        }
                    }
                });

                ProfilerLog.Debug("Index4");

                for (int i = 0; i < clusterCount; i++)
                {
                    Bounds bound = boundsByCluster[i];

                    int startX = Math.Max(0, Mathf.FloorToInt(bound.min.x * cellSizeReciprocal));
                    int startY = Math.Max(0, Mathf.FloorToInt(bound.min.y * cellSizeReciprocal));
                    int startZ = Math.Max(0, Mathf.FloorToInt(bound.min.z * cellSizeReciprocal));

                    int endX = Math.Min(sizeX - 1, Mathf.CeilToInt(bound.max.x * cellSizeReciprocal));
                    int endY = Math.Min(sizeY - 1, Mathf.CeilToInt(bound.max.y * cellSizeReciprocal));
                    int endZ = Math.Min(sizeZ - 1, Mathf.CeilToInt(bound.max.z * cellSizeReciprocal));

                    for (int x = startX; x <= endX; x++)
                    {
                        for (int y = startY; y <= endY; y++)
                        {
                            for (int z = startZ; z <= endZ; z++)
                            {
                                ref Index4 index = ref _clusterIndexByPos[x, y, z];

                                for (int j = 0; j < 4; j++)
                                {
                                    if (index[j] == -1)
                                    {
                                        index[j] = (short)i;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                };

                ProfilerLog.Debug("Index4 2");

                _meshFilter.mesh = GenerateMesh(boundsByCluster);

                _visibleClusters = new uint[clusterCount];
                _visibleClustersBuffer = new ComputeBuffer(clusterCount, 4, ComputeBufferType.Default);

                Graphics.ClearRandomWriteTargets();
                Graphics.SetRandomWriteTarget(1, _visibleClustersBuffer, false);

                _meshRenderer.material.SetBuffer("_VisibleClusters", _visibleClustersBuffer);
                _meshRenderer.material.SetInt("_Debug", Convert.ToInt32(debug));

                enabled = true;
                ProfilerLog.Debug("Index4 2");
            }
        }

        public void Update()
        {
            if (Time.frameCount % updateFrameDelay != 0)
            {
                return;
            }

            _visibleClustersBuffer.GetData(_visibleClusters);


            Vector3 cameraPostion = Camera.main.transform.position;

            int cellX = Mathf.FloorToInt(cameraPostion.x * cellSizeReciprocal);
            int cellY = Mathf.FloorToInt(cameraPostion.y * cellSizeReciprocal);
            int cellZ = Mathf.FloorToInt(cameraPostion.z * cellSizeReciprocal);

            Index4 currentClusterIndex = new Index4
            {
                value0 = -1,
                value1 = -1,
                value2 = -1,
                value3 = -1,
            };

            if (cellX >= 0
                && cellY >= 0
                && cellZ >= 0
                && cellX < _clustersLength.x
                && cellY < _clustersLength.y
                && cellZ < _clustersLength.z)
            {
                currentClusterIndex = _clusterIndexByPos[cellX, cellY, cellZ];
            }

            for (int i = 0; i < _meshRenderersByClusterIndex.Length; i++)
            {
                bool visible = _visibleClusters[i] != 0
                    || i == currentClusterIndex.value0
                    || i == currentClusterIndex.value1
                    || i == currentClusterIndex.value2
                    || i == currentClusterIndex.value3;

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

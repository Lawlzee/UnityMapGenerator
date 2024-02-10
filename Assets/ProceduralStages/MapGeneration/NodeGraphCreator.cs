using RoR2.Navigation;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using System.Diagnostics;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace ProceduralStages
{
    public class Graphs
    {
        public NodeGraph ground;
        public NodeGraph air;
        public List<PropsNode> floorProps;
        public List<PropsNode> ceilingProps;
    }

    public struct PropsNode
    {
        public Vector3 position;
        public Vector3 normal;

        public GameObject Place(
            GameObject prefab, 
            GameObject parent, 
            Material material, 
            Color? color,
            Vector3? normal,
            float scale)
        {
            var rotation = Quaternion.FromToRotation(Vector3.up, normal ?? this.normal) 
                * prefab.transform.rotation
                * Quaternion.FromToRotation(Vector3.up, new Vector3(0, MapGenerator.rng.nextNormalizedFloat * 360f, 0));

            GameObject gameObject = GameObject.Instantiate(prefab, position, rotation, parent.transform);

            //Quaternion rotation = Quaternion.Euler(0.0f, MapGenerator.rng.nextNormalizedFloat * 360f, 0.0f);
            //gameObject.transform.up = normal ?? this.normal;
            gameObject.transform.localScale = new Vector3(scale, scale, scale);

            if (Application.isEditor)
            {
                LODGroup[] lodGroups = gameObject.GetComponentsInChildren<LODGroup>();
                foreach (LODGroup lodGroup in lodGroups)
                {
                    var lods = lodGroup.GetLODs();
            
                    lods[lods.Length - 1].screenRelativeTransitionHeight = 0;
                    lodGroup.SetLODs(lods);
                }
            }

            if (material != null)
            {
                var meshRenderers = gameObject.transform.GetComponentsInChildren<MeshRenderer>();
                for (int i = 0; i < meshRenderers.Length; i++)
                {
                    meshRenderers[i].material = material;
                }
            }

            if (color.HasValue)
            {
                var meshRenderers = gameObject.transform.GetComponentsInChildren<MeshRenderer>();
                for (int i = 0; i < meshRenderers.Length; i++)
                {
                    foreach (var m in meshRenderers[i].materials)
                    {
                        m.SetColor("_Color", color.Value);
                    }
                }
            }

            //gameObject.transform.Rotate(Vector3.up, MapGenerator.rng.RangeFloat(0.0f, 360f), Space.Self);
            return gameObject;
        }
    }

    [Serializable]
    public class NodeGraphCreator
    {
        public float minFloorAngle = 0.4f;
        public float airNodeheight = 20f;

        public int maxGroundheight = 30;
        public DensityMap densityMap = new DensityMap();

        public Graphs CreateGraphs(MeshResult meshResult, float[,,] map, float[,,] floorMap, float mapScale)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            //MapDensity mapDensity = densityMap.Create(map);
            ///LogStats("mapDensity");

            (NodeGraph groundNodes, List<PropsNode> floorProps, List<PropsNode> ceilingProps) = CreateGroundNodes(meshResult, floorMap, mapScale);
            LogStats("groundNodes");

            HashSet<int> mainIsland = GetMainIsland(groundNodes);
            //HashSet<int> mainIsland = new HashSet<int>();
            LogStats("mainIsland");

            SetMainIslandFlags(groundNodes, mainIsland, floorMap, mapScale);
            LogStats("SetMainIslandFlags");

            groundNodes.Awake();

            NodeGraph airNodes = CreateAirNodes(groundNodes, mainIsland, map, mapScale);
            LogStats("CreateAirNodes");

            return new Graphs
            {
                ground = groundNodes,
                air = airNodes,
                floorProps = floorProps,
                ceilingProps = ceilingProps
            };

            void LogStats(string name)
            {
                Log.Debug($"{name}: {stopwatch.Elapsed}");
                stopwatch.Restart();
            }
        }

        private (NodeGraph groundGraph, List<PropsNode> floorProps, List<PropsNode> ceilingProps) CreateGroundNodes(MeshResult meshResult, float[,,] floorMap, float mapScale)
        {
            var groundNodes = ScriptableObject.CreateInstance<NodeGraph>();

            var triangles = meshResult.triangles;
            var vertices = meshResult.vertices;
            var normals = meshResult.normals;

            List<PropsNode> floorProps = new List<PropsNode>();
            List<PropsNode> ceilingProps = new List<PropsNode>();

            var nodes = new NodeGraph.Node[vertices.Length];

            int index = 0;
            for (int i = 0; i < vertices.Length; i++)
            {
                var vertex = vertices[i];
                var normal = normals[i];

                float angle = Vector3.Dot(Vector3.up, normal);
                
                bool isFloor = angle > minFloorAngle;
                bool isCeiling = angle < -minFloorAngle;

                var node = new NodeGraph.Node
                {
                    position = vertex,
                    linkListIndex = new NodeGraph.LinkListIndex()
                    {
                        index = isFloor ? index : -1,
                        size = 0
                    },
                    forbiddenHulls = HullMask.None,
                    flags = NodeFlags.NoCharacterSpawn | NodeFlags.NoChestSpawn | NodeFlags.NoShrineSpawn
                };

                if (isFloor)
                {
                    floorProps.Add(new PropsNode
                    {
                        normal = normal,
                        position = vertex
                    });

                    index++;

                    float density = densityMap.GetDensity(floorMap, vertex / mapScale);

                    HullMask forbiddenHulls = HullMask.None;

                    if (density > densityMap.ground.maxHumanDensity)
                    {
                        forbiddenHulls |= HullMask.Human;
                    }

                    if (density > densityMap.ground.maxGolemDensity)
                    {
                        forbiddenHulls |= HullMask.Golem;
                    }

                    if (density > densityMap.ground.maxBeetleQueenDensity)
                    {
                        forbiddenHulls |= HullMask.BeetleQueen;
                    }

                    node.forbiddenHulls = forbiddenHulls;
                }

                if (isCeiling)
                {
                    ceilingProps.Add(new PropsNode
                    {
                        normal = normal,
                        position = vertex
                    });
                }

                nodes[i] = node;
            }

            List<NodeGraph.Link>[] links = new List<NodeGraph.Link>[index];

            for (int i = 0; i < index; i++)
            {
                links[i] = new List<NodeGraph.Link>();
            }

            for (int i = 0; i < triangles.Length; i += 3)
            {
                ref var node1 = ref nodes[triangles[i]];
                ref var node2 = ref nodes[triangles[i + 1]];
                ref var node3 = ref nodes[triangles[i + 2]];

                if (node1.linkListIndex.index != -1)
                {
                    if (node2.linkListIndex.index != -1)
                    {
                        float distance = (node1.position - node2.position).magnitude;

                        links[node1.linkListIndex.index].Add(new NodeGraph.Link
                        {
                            nodeIndexA = new NodeGraph.NodeIndex(node1.linkListIndex.index),
                            nodeIndexB = new NodeGraph.NodeIndex(node2.linkListIndex.index),
                            distanceScore = distance,
                            minJumpHeight = 0,
                            hullMask = 0xFFFFFFF,
                            jumpHullMask = 0xFFFFFFF,
                            gateIndex = 0
                        });

                        links[node2.linkListIndex.index].Add(new NodeGraph.Link
                        {
                            nodeIndexA = new NodeGraph.NodeIndex(node2.linkListIndex.index),
                            nodeIndexB = new NodeGraph.NodeIndex(node1.linkListIndex.index),
                            distanceScore = distance,
                            minJumpHeight = 0,
                            hullMask = 0xFFFFFFF,
                            jumpHullMask = 0xFFFFFFF,
                            gateIndex = 0
                        });
                    }

                    if (node3.linkListIndex.index != -1)
                    {
                        float distance = (node2.position - node3.position).magnitude;

                        links[node1.linkListIndex.index].Add(new NodeGraph.Link
                        {
                            nodeIndexA = new NodeGraph.NodeIndex(node1.linkListIndex.index),
                            nodeIndexB = new NodeGraph.NodeIndex(node3.linkListIndex.index),
                            distanceScore = distance,
                            minJumpHeight = 0,
                            hullMask = 0xFFFFFFF,
                            jumpHullMask = 0xFFFFFFF,
                            gateIndex = 0
                        });

                        links[node3.linkListIndex.index].Add(new NodeGraph.Link
                        {
                            nodeIndexA = new NodeGraph.NodeIndex(node3.linkListIndex.index),
                            nodeIndexB = new NodeGraph.NodeIndex(node1.linkListIndex.index),
                            distanceScore = distance,
                            minJumpHeight = 0,
                            hullMask = 0xFFFFFFF,
                            jumpHullMask = 0xFFFFFFF,
                            gateIndex = 0
                        });
                    }
                }

                if (node2.linkListIndex.index != -1 && node3.linkListIndex.index != -1)
                {
                    float distance = (node2.position - node3.position).magnitude;

                    links[node2.linkListIndex.index].Add(new NodeGraph.Link
                    {
                        nodeIndexA = new NodeGraph.NodeIndex(node2.linkListIndex.index),
                        nodeIndexB = new NodeGraph.NodeIndex(node3.linkListIndex.index),
                        distanceScore = distance,
                        minJumpHeight = 0,
                        hullMask = 0xFFFFFFF,
                        jumpHullMask = 0xFFFFFFF,
                        gateIndex = 0
                    });

                    links[node3.linkListIndex.index].Add(new NodeGraph.Link
                    {
                        nodeIndexA = new NodeGraph.NodeIndex(node3.linkListIndex.index),
                        nodeIndexB = new NodeGraph.NodeIndex(node2.linkListIndex.index),
                        distanceScore = distance,
                        minJumpHeight = 0,
                        hullMask = 0xFFFFFFF,
                        jumpHullMask = 0xFFFFFFF,
                        gateIndex = 0
                    });
                }
            };

            NodeGraph.Node[] allNodes = nodes
                .Where(x => x.linkListIndex.index != -1)
                .ToArray();

            List<NodeGraph.Link> linkList = new List<NodeGraph.Link>();

            for (int i = 0; i < allNodes.Length; i++)
            {
                var currentLinks = links[i];
                int position = linkList.Count;

                for (int j = 0; j < currentLinks.Count; j++)
                {
                    linkList.Add(currentLinks[j]);
                }

                ref NodeGraph.Node node = ref allNodes[i];
                node.linkListIndex.index = position;
                node.linkListIndex.size = (uint)currentLinks.Count;
            }

            groundNodes.nodes = allNodes;
            groundNodes.links = linkList.ToArray();

            return (groundNodes, floorProps, ceilingProps);
        }

        private HashSet<int> GetMainIsland(NodeGraph groundGraph)
        {
            var islands = GetNodeIslands(groundGraph.nodes, groundGraph.links)
                .OrderByDescending(x => x.Count)
                .ToList();

            return islands[0];
        }

        private List<HashSet<int>> GetNodeIslands(NodeGraph.Node[] allNodes, NodeGraph.Link[] links)
        {
            HashSet<int> nodesNotUsed = new HashSet<int>(Enumerable.Range(0, allNodes.Length));

            List<HashSet<int>> islands = new List<HashSet<int>>();

            Queue<int> queue = new Queue<int>();

            while (nodesNotUsed.Count > 0)
            {
                var island = new HashSet<int>();

                int rootNodeIndex = nodesNotUsed.First();
                queue.Enqueue(rootNodeIndex);
                nodesNotUsed.Remove(rootNodeIndex);

                while (queue.Count > 0)
                {
                    var nodeIndex = queue.Dequeue();

                    var node = allNodes[nodeIndex];
                    island.Add(nodeIndex);

                    for (int i = node.linkListIndex.index; i < node.linkListIndex.index + node.linkListIndex.size; i++)
                    {
                        var link = links[i];
                        if (nodesNotUsed.Contains(link.nodeIndexB.nodeIndex))
                        {
                            queue.Enqueue(link.nodeIndexB.nodeIndex);
                            nodesNotUsed.Remove(link.nodeIndexB.nodeIndex);
                        }
                    }
                }

                islands.Add(island);
            }

            return islands;
        }

        private void SetMainIslandFlags(NodeGraph groundGraph, HashSet<int> mainIsland, float[,,] floorMap, float mapScale)
        {
            foreach (int nodeIndex in mainIsland)
            {
                ref NodeGraph.Node node = ref groundGraph.nodes[nodeIndex];

                NodeFlags flags = NodeFlags.NoCharacterSpawn | NodeFlags.NoChestSpawn | NodeFlags.NoShrineSpawn;

                float density = densityMap.GetDensity(floorMap, node.position / mapScale);

                if (node.position.y <= maxGroundheight)
                {
                    if (densityMap.minTeleporterDensity <= density && density <= densityMap.maxTeleporterDensity)
                    {
                        flags |= NodeFlags.TeleporterOK;
                    }

                    if (density < densityMap.maxChestDensity)
                    {
                        flags &= ~NodeFlags.NoChestSpawn;
                    }

                    if (density < densityMap.maxChestDensity)
                    {
                        flags &= ~NodeFlags.NoShrineSpawn;
                    }

                    if (densityMap.minNewtDensity <= density && density <= densityMap.maxNewtDensity)
                    {
                        flags |= NodeFlagsExt.Newt;
                    }

                    if (density < densityMap.maxSpawnDensity)
                    {
                        flags &= ~NodeFlags.NoCharacterSpawn;
                        flags |= NodeFlags.NoCeiling;
                    }
                }

                node.flags = flags;
            }
        }

        private NodeGraph CreateAirNodes(NodeGraph groundNodes, HashSet<int> mainIsland, float[,,] map3d, float mapScale)
        {
            Dictionary<int, int> newNodeIndex = new Dictionary<int, int>();
            List<NodeGraph.Node> airNodes = new List<NodeGraph.Node>(mainIsland.Count);

            int newIndex = 0;
            for (int i = 0; i < groundNodes.nodes.Length; i++)
            {
                if (!mainIsland.Contains(i))
                {
                    continue;
                }

                NodeGraph.Node airNode = groundNodes.nodes[i];
                airNode.position.y += airNodeheight;

                float density = densityMap.GetDensity(map3d, airNode.position / mapScale);

                HullMask forbiddenHulls = HullMask.None;

                if (density > densityMap.air.maxHumanDensity)
                {
                    forbiddenHulls |= HullMask.Human;
                }

                if (density > densityMap.air.maxGolemDensity)
                {
                    forbiddenHulls |= HullMask.Golem;
                }

                if (density > densityMap.air.maxBeetleQueenDensity)
                {
                    forbiddenHulls |= HullMask.BeetleQueen;
                }

                airNode.flags = NodeFlags.NoChestSpawn | NodeFlags.NoShrineSpawn | NodeFlags.NoCeiling;
                airNode.forbiddenHulls = forbiddenHulls;


                if (forbiddenHulls != (HullMask)7)
                {
                    newNodeIndex[i] = newIndex;
                    airNodes.Add(airNode);
                    newIndex++;
                }
            }

            NodeGraph.Node[] airNodesArray = airNodes.ToArray();

            List<NodeGraph.Link> airLinks = new List<NodeGraph.Link>();
            for (int i = 0; i < airNodesArray.Length; i++)
            {
                ref NodeGraph.Node node = ref airNodesArray[i];

                int position = airLinks.Count;
                uint count = 0;

                for (int j = node.linkListIndex.index; j < node.linkListIndex.index + node.linkListIndex.size; j++)
                {
                    NodeGraph.Link link = groundNodes.links[j];
                    if (newNodeIndex.TryGetValue(link.nodeIndexB.nodeIndex, out int index))
                    {
                        link.nodeIndexA = new NodeGraph.NodeIndex(i);
                        link.nodeIndexB = new NodeGraph.NodeIndex(index);
                        airLinks.Add(link);
                        count++;
                    }
                }

                node.linkListIndex.index = position;
                node.linkListIndex.size = count;
            }

            NodeGraph airGraph = ScriptableObject.CreateInstance<NodeGraph>();
            airGraph.nodes = airNodesArray;
            airGraph.links = airLinks.ToArray();
            airGraph.Awake();

            return airGraph;
        }
    }
}

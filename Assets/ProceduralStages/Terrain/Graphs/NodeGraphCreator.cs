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

            Graphs graphs = CreateGroundNodes(meshResult, floorMap, mapScale);
            LogStats("groundNodes");

            HashSet<int> mainIsland = GetMainIsland(graphs.ground);
            //HashSet<int> mainIsland = new HashSet<int>();
            LogStats("mainIsland");

            SetMainIslandFlags(graphs.ground, mainIsland, floorMap, mapScale);
            LogStats("SetMainIslandFlags");

            graphs.ground.Awake();

            graphs.air = CreateAirNodes(graphs.ground, mainIsland, map, mapScale);
            LogStats("CreateAirNodes");

            return graphs;

            void LogStats(string name)
            {
                Log.Debug($"{name}: {stopwatch.Elapsed}");
                stopwatch.Restart();
            }
        }

        private Graphs CreateGroundNodes(MeshResult meshResult, float[,,] floorMap, float mapScale)
        {
            var groundNodes = ScriptableObject.CreateInstance<NodeGraph>();

            var triangles = meshResult.triangles;
            var vertices = meshResult.vertices;
            var normals = meshResult.normals;

            Dictionary<Vector3, PropsNode> nodeInfoByPosition = new Dictionary<Vector3, PropsNode>();
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

                var propsNode = new PropsNode
                {
                    normal = normal,
                    position = vertex
                };

                nodeInfoByPosition[vertex] = propsNode;

                if (isFloor)
                {
                    floorProps.Add(propsNode);

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
                    ceilingProps.Add(propsNode);
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

            var nodeByPosition = allNodes
                .ToDictionary(
                    x => x.position,
                    x => x.linkListIndex.index);

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

            return new Graphs
            {
                ground = groundNodes,
                floorProps = floorProps,
                ceilingProps = ceilingProps,
                groundNodeIndexByPosition = nodeByPosition,
                nodeInfoByPosition = nodeInfoByPosition
            };
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

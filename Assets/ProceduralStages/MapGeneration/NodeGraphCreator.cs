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

namespace ProceduralStages
{
    [Serializable]
    public class NodeGraphCreator
    {
        public float minFloorAngle = 0.4f;
        public float flatMaxSlope = 1f;
        public float airNodeheight = 20f;

        public int maxGroundheight = 30;
        public DensityMap densityMap = new DensityMap();
        public FlatMap flatMap = new FlatMap();

        public (NodeGraph ground, NodeGraph air) CreateGraphs(MeshResult meshResult, bool[,,] map, float mapScale)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            MapDensity mapDensity = densityMap.Create(map);
            LogStats("mapDensity");

            NodeGraph groundNodes = CreateGroundNodes(meshResult, mapDensity, mapScale);
            LogStats("groundNodes");

            HashSet<int> mainIsland = GetMainIsland(groundNodes);
            LogStats("mainIsland");

            SetMainIslandFlags(groundNodes, mainIsland, map, mapDensity, mapScale);
            LogStats("SetMainIslandFlags");

            groundNodes.Awake();

            NodeGraph airNodes = CreateAirNodes(groundNodes, mainIsland, map, mapDensity, mapScale);
            LogStats("CreateAirNodes");

            return (groundNodes, airNodes);

            void LogStats(string name)
            {
                Log.Debug($"{name}: {stopwatch.Elapsed}");
                stopwatch.Restart();
            }
        }

        private NodeGraph CreateGroundNodes(MeshResult meshResult, MapDensity mapDensity, float mapScale)
        {
            var groundNodes = ScriptableObject.CreateInstance<NodeGraph>();

            var triangles = meshResult.triangles; ;
            var vertices = meshResult.vertices;
            var normals = meshResult.normals;

            var nodes = new NodeGraph.Node[vertices.Length];

            int index = 0;
            for (int i = 0; i < vertices.Length; i++)
            {
                var vertex = vertices[i];
                var normal = normals[i];

                bool valid = Vector3.Dot(Vector3.up, normal) > minFloorAngle;

                var node = new NodeGraph.Node
                {
                    position = vertex,
                    linkListIndex = new NodeGraph.LinkListIndex()
                    {
                        index = valid ? index : -1,
                        size = 0
                    },
                    forbiddenHulls = HullMask.None,
                    flags = NodeFlags.NoCharacterSpawn | NodeFlags.NoChestSpawn | NodeFlags.NoShrineSpawn
                };

                if (valid)
                {
                    index++;

                    float density = mapDensity.GetDensity(vertex / mapScale, isGround: true);

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

            return groundNodes;
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

        private void SetMainIslandFlags(NodeGraph groundGraph, HashSet<int> mainIsland, bool[,,] map, MapDensity mapDensity, float mapScale)
        {
            var mapFlatness = flatMap.Create(map);

            foreach (int nodeIndex in mainIsland)
            {
                ref NodeGraph.Node node = ref groundGraph.nodes[nodeIndex];

                NodeFlags flags = NodeFlags.NoCeiling | NodeFlags.NoCharacterSpawn | NodeFlags.NoChestSpawn | NodeFlags.NoShrineSpawn;

                Vector3 scaledPosition = node.position / mapScale;
                int x = Mathf.RoundToInt(scaledPosition.x);
                int y = Mathf.RoundToInt(scaledPosition.y);
                int z = Mathf.RoundToInt(scaledPosition.z);

                float density = mapDensity.GetDensity(scaledPosition, isGround: true);
                float flatness = mapFlatness[x, y + 1, z];

                if (node.position.y <= maxGroundheight)
                {
                    if (densityMap.minTeleporterDensity <= density && density <= densityMap.maxTeleporterDensity)
                    {
                        if (flatMap.minTeleporterFlatness <= flatness)
                        {
                          flags |= NodeFlags.TeleporterOK;
                        }
                    }

                    if (flatMap.minInteractableFlatness <= flatness)
                    {
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
                    }

                    if (flatMap.minCharacterFlatness <= flatness)
                    {
                        flags &= ~NodeFlags.NoCharacterSpawn;
                    }
                }

                node.flags = flags;
            }
        }

        private NodeGraph CreateAirNodes(NodeGraph groundNodes, HashSet<int> mainIsland, bool[,,] map3d, MapDensity mapDensity, float mapScale)
        {
            Dictionary<int, int> newNodeIndex = new Dictionary<int, int>();

            int newIndex = 0;
            for (int i = 0; i < groundNodes.nodes.Length; i++)
            {
                if (!mainIsland.Contains(i))
                {
                    continue;
                }

                ref NodeGraph.Node groundNode = ref groundNodes.nodes[i];
                Vector3 newPosition = groundNode.position;
                newPosition.y = newPosition.y + airNodeheight;
                Vector3 scaledPosition = newPosition / mapScale;
                Vector3Int position = new Vector3Int(Mathf.RoundToInt(scaledPosition.x), Mathf.RoundToInt(scaledPosition.y), Mathf.RoundToInt(scaledPosition.z));

                bool inWall = false;

                for (int dx = -1; dx <= 1 && !inWall; dx++)
                {
                    int x = position.x + dx;

                    if (x < 0 || x >= map3d.GetLength(0))
                    {
                        inWall = true;
                        break;
                    }

                    for (int dy = -1; dy <= 1 && !inWall; dy++)
                    {
                        int y = position.y + dy;

                        if (y < 0 || y >= map3d.GetLength(1))
                        {
                            inWall = true;
                            break;
                        }

                        for (int dz = -1; dz <= 1 && !inWall; dz++)
                        {
                            int z = position.z + dz;

                            if (z < 0 || z >= map3d.GetLength(2))
                            {
                                inWall = true;
                                break;
                            }

                            if (map3d[x, y, z])
                            {
                                inWall = true;
                                break;
                            }
                        }
                    }
                }

                if (!inWall)
                {
                    newNodeIndex[i] = newIndex;
                    newIndex++;
                }
            }

            NodeGraph.Node[] airNodes = new NodeGraph.Node[newNodeIndex.Count];
            for (int i = 0; i < groundNodes.nodes.Length; i++)
            {
                if (!newNodeIndex.TryGetValue(i, out int index))
                {
                    continue;
                }

                NodeGraph.Node airNode = groundNodes.nodes[i];

                airNode.flags = NodeFlags.NoChestSpawn | NodeFlags.NoShrineSpawn | NodeFlags.NoCeiling;
                airNode.position.y += airNodeheight;

                float density = mapDensity.GetDensity(airNode.position / mapScale, isGround: false);

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

                airNode.forbiddenHulls = forbiddenHulls;

                airNodes[index] = airNode;
            }

            List<NodeGraph.Link> airLinks = new List<NodeGraph.Link>();
            for (int i = 0; i < airNodes.Length; i++)
            {
                ref NodeGraph.Node node = ref airNodes[i];

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
            airGraph.nodes = airNodes;
            airGraph.links = airLinks.ToArray();
            airGraph.Awake();

            return airGraph;
        }
    }
}

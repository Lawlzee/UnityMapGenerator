using RoR2.Navigation;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts
{
    [Serializable]
    public class NodeGraphCreator
    {
        public float minFloorAngle = 0.4f;
        public float flatMaxSlope = 1f;
        public float airNodeheight = 20f;

        public (NodeGraph NodeGraph, HashSet<int> MainIsland) CreateGroundNodes(MeshResult meshResult)
        {
            var groundNodes = ScriptableObject.CreateInstance<NodeGraph>();

            var triangles = meshResult.triangles; ;
            var vertices = meshResult.vertices;
            var normals = meshResult.normals;

            var nodes = new NodeGraph.Node[vertices.Count];

            int index = 0;
            for (int i = 0; i < vertices.Count; i++)
            {
                //Log.Info("2");
                var vertex = vertices[i];
                var normal = normals[i];
                //Log.Info("3");

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
                }

                nodes[i] = node;

            }

            List<NodeGraph.Link>[] links = new List<NodeGraph.Link>[index];

            for (int i = 0; i < index; i++)
            {
                links[i] = new List<NodeGraph.Link>();
            }

            for (int i = 0; i < triangles.Count; i += 3)
            {
                ref var node1 = ref nodes[triangles[i]];
                ref var node2 = ref nodes[triangles[i + 1]];
                ref var node3 = ref nodes[triangles[i + 2]];

                if (node1.linkListIndex.index != -1)
                {
                    if (node2.linkListIndex.index != -1)
                    {
                        //node1.linkListIndex.size++;

                        links[node1.linkListIndex.index].Add(new NodeGraph.Link
                        {
                            nodeIndexA = new NodeGraph.NodeIndex(node1.linkListIndex.index),
                            nodeIndexB = new NodeGraph.NodeIndex(node2.linkListIndex.index),
                            distanceScore = 1,
                            minJumpHeight = 0,
                            hullMask = 31,
                            jumpHullMask = 31,
                            gateIndex = 0
                        });

                        links[node2.linkListIndex.index].Add(new NodeGraph.Link
                        {
                            nodeIndexA = new NodeGraph.NodeIndex(node2.linkListIndex.index),
                            nodeIndexB = new NodeGraph.NodeIndex(node1.linkListIndex.index),
                            distanceScore = 1,
                            minJumpHeight = 0,
                            hullMask = 31,
                            jumpHullMask = 31,
                            gateIndex = 0
                        });
                    }

                    if (node3.linkListIndex.index != -1)
                    {
                        links[node1.linkListIndex.index].Add(new NodeGraph.Link
                        {
                            nodeIndexA = new NodeGraph.NodeIndex(node1.linkListIndex.index),
                            nodeIndexB = new NodeGraph.NodeIndex(node3.linkListIndex.index),
                            distanceScore = 1,
                            minJumpHeight = 0,
                            hullMask = 31,
                            jumpHullMask = 31,
                            gateIndex = 0
                        });

                        links[node3.linkListIndex.index].Add(new NodeGraph.Link
                        {
                            nodeIndexA = new NodeGraph.NodeIndex(node3.linkListIndex.index),
                            nodeIndexB = new NodeGraph.NodeIndex(node1.linkListIndex.index),
                            distanceScore = 1,
                            minJumpHeight = 0,
                            hullMask = 31,
                            jumpHullMask = 31,
                            gateIndex = 0
                        });
                    }
                }

                if (node2.linkListIndex.index != -1 && node3.linkListIndex.index != -1)
                {
                    links[node2.linkListIndex.index].Add(new NodeGraph.Link
                    {
                        nodeIndexA = new NodeGraph.NodeIndex(node2.linkListIndex.index),
                        nodeIndexB = new NodeGraph.NodeIndex(node3.linkListIndex.index),
                        distanceScore = 1,
                        minJumpHeight = 0,
                        hullMask = 31,
                        jumpHullMask = 31,
                        gateIndex = 0
                    });

                    links[node3.linkListIndex.index].Add(new NodeGraph.Link
                    {
                        nodeIndexA = new NodeGraph.NodeIndex(node3.linkListIndex.index),
                        nodeIndexB = new NodeGraph.NodeIndex(node2.linkListIndex.index),
                        distanceScore = 1,
                        minJumpHeight = 0,
                        hullMask = 31,
                        jumpHullMask = 31,
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

            //Log.Info("A");

            var islands = GetNodeIslands(allNodes, linkList)
                .OrderByDescending(x => x.Count)
                .ToList();

            var mainIsland = islands[0];

            foreach (int nodeIndex in mainIsland)
            {
                ref NodeGraph.Node node = ref allNodes[nodeIndex];

                bool isFlat = true;
                for (int i = node.linkListIndex.index; i < node.linkListIndex.index + node.linkListIndex.size; i++)
                {
                    var link = linkList[i];
                    var otherNode = allNodes[link.nodeIndexB.nodeIndex];
                    if (Math.Abs(node.position.y - otherNode.position.y) > flatMaxSlope)
                    {
                        isFlat = false;
                        break;
                    }
                }

                if (isFlat)
                {
                    node.flags = NodeFlags.TeleporterOK;
                }
            }

            groundNodes.nodes = allNodes;
            groundNodes.links = linkList.ToArray();
            groundNodes.Awake();

            return (groundNodes, mainIsland);
        }

        private List<HashSet<int>> GetNodeIslands(NodeGraph.Node[] allNodes, List<NodeGraph.Link> links)
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

        public NodeGraph CreateAirNodes(NodeGraph groundNodes, HashSet<int> mainIsland, bool[,,] map3d, float mapScale)
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

                NodeGraph.Node groundNode = groundNodes.nodes[i];

                airNodes[index] = groundNode;
                airNodes[index].position.y += airNodeheight;
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

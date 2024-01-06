using Generator.Assets.Scripts;
using RoR2;
using RoR2.Navigation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Scripts
{
    public class Overlay : MonoBehaviour
    {
        public HullMask hullMask;
        public NodeFlags nodeFlags;
        public float minFloorAngle = 0.4f;
        public float flatMaxSlope = 1f;
        private MeshResult _meshResult;

        public void Awake()
        {
            GameObject.Find("Map Generator").GetComponent<MapGenerator>().onGenerated += Overlay_onGenerated;
        }

        public void OnValidate()
        {
            if (_meshResult != null)
            {
                Overlay_onGenerated(_meshResult);
            }
        }

        private void Overlay_onGenerated(MeshResult meshResult)
        {
            _meshResult = meshResult;

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



            //for (int i = 0; i < allNodes.Length; i++)
            //{
            //    var node = allNodes[i];
            //    SerializableBitArray bitArray = new SerializableBitArray(node.links.Count);
            //    for (int j = 0; j < node.links.Count; j++)
            //    {
            //        bitArray[j] = true;
            //    }
            //
            //    lineOfSightMasks[i] = bitArray;
            //}
            //Log.Info("A");

            var islands = GetNodeIslands(allNodes, linkList)
                .OrderByDescending(x => x.Count)
                .ToList();

            var mainIsland = islands[0];

            Debug.Log($"allNodes.Count = {allNodes.Length}");
            Debug.Log($"linkList.Count = {linkList.Count}");
            Debug.Log($"islands.Count = {islands.Count}");
            Debug.Log($"mainIsland.Count = {mainIsland.Count}");

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

            var mesh = groundNodes.GenerateLinkDebugMesh(hullMask, nodeFlags);
            GetComponent<MeshFilter>().mesh = mesh;

            //groundNodes.SetNodes(allNodes, lineOfSightMasks.AsReadOnly());
        }

        private List<List<int>> GetNodeIslands(NodeGraph.Node[] allNodes, List<NodeGraph.Link> links)
        {
            HashSet<int> nodesNotUsed = new HashSet<int>(Enumerable.Range(0, allNodes.Length));

            List<List<int>> islands = new List<List<int>>();

            Queue<int> queue = new Queue<int>();

            while (nodesNotUsed.Count > 0)
            {
                var island = new List<int>();

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
    }
}

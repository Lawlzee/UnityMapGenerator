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
using TMPro;

namespace ProceduralStages
{
    [Serializable]
    public class NodeGraphCreator
    {
        public float minFloorAngle = 0.4f;
        public float airNodeMinDistance = 4f;
        public int airNodeSample = 20;
        //public float airNodeheight = 20f;

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

            //graphs.air = CreateAirNodes(graphs.ground, mainIsland, map, mapScale);
            graphs.air = CreateAirGraph(map, mapScale);
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

        private NodeGraph CreateAirGraph(float[,,] map3d, float mapScale)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            Vector3 mapSize = new Vector3(
                mapScale * map3d.GetLength(0),
                mapScale * map3d.GetLength(1),
                mapScale * map3d.GetLength(2));

            Octree<int> kdTree = new Octree<int>(new Bounds(mapSize / 2, mapSize), 4);
            List<NodeGraph.Node> airNodes = new List<NodeGraph.Node>();

            Queue<Vector3> positionsToProcess = new Queue<Vector3>();

            int seedCount = 0;
            for (int i = 0; i < 5000 && seedCount < 25; i++)
            {
                float initialX = MapGenerator.rng.nextNormalizedFloat * map3d.GetLength(0) * mapScale;
                float initialY = MapGenerator.rng.nextNormalizedFloat * map3d.GetLength(1) * mapScale;
                float initialZ = MapGenerator.rng.nextNormalizedFloat * map3d.GetLength(2) * mapScale;

                Vector3 initialPosition = new Vector3(initialX, initialY, initialZ);

                float density = densityMap.GetDensity(map3d, initialPosition / mapScale);

                if (density < 0.25f)
                {
                    positionsToProcess.Enqueue(initialPosition);

                    NodeGraph.Node airNode = new NodeGraph.Node();
                    airNode.position = initialPosition;
                    airNode.forbiddenHulls = HullMask.None;
                    airNode.flags = NodeFlags.NoChestSpawn | NodeFlags.NoShrineSpawn | NodeFlags.NoCeiling;

                    kdTree.Add(new Octree<int>.Point
                    {
                        Position = initialPosition,
                        Value = airNodes.Count
                    });
                    airNodes.Add(airNode);

                    seedCount++;
                }
            }

            LogStats("initialPosition");

            while (positionsToProcess.Count > 0)
            {
                Vector3 seedPosition = positionsToProcess.Dequeue();

                for (int i = 0; i < airNodeSample; i++)
                {
                    float directionX = MapGenerator.rng.RangeFloat(-1, 1);
                    float directionY = MapGenerator.rng.RangeFloat(-1, 1);
                    float directionZ = MapGenerator.rng.RangeFloat(-1, 1);

                    Vector3 direction = new Vector3(directionX, directionY, directionZ).normalized;

                    float distance = MapGenerator.rng.RangeFloat(airNodeMinDistance, airNodeMinDistance * 2);

                    Vector3 targetPosition = seedPosition + direction * distance;

                    if (IsValidAirNodePosition(map3d, mapScale, kdTree, airNodes, targetPosition))
                    {
                        positionsToProcess.Enqueue(targetPosition);
                    }
                }
            }
            LogStats("nodes");

            //kdTree.Save();
            LogStats("kdTree.Save()");


            NodeGraph.Node[] airNodesArray = airNodes.ToArray();
            List<NodeGraph.Link> airLinks = new List<NodeGraph.Link>();

            Log.Debug("links");

            //for (int i = 0; i < airNodes.Count - 1; i++)
            //{
            //    ref NodeGraph.Node airNode = ref airNodesArray[i];
            //    //var neighbours = kdTree.RadialSearch(airNode.position, airNodeMinDistance * 2);
            //
            //    airLinks.Add(new NodeGraph.Link
            //    {
            //        nodeIndexA = new NodeGraph.NodeIndex(i),
            //        nodeIndexB = new NodeGraph.NodeIndex(i + 1),
            //        distanceScore = 1,
            //        minJumpHeight = 0,
            //        hullMask = 0xFFFFFFF,
            //        jumpHullMask = 0xFFFFFFF,
            //        gateIndex = 0
            //    });
            //
            //    airNode.linkListIndex = new NodeGraph.LinkListIndex
            //    {
            //        index = airLinks.Count - 1,
            //        size = 1
            //    };
            //}

            for (int i = 0; i < airNodes.Count; i++)
            {
                ref NodeGraph.Node airNode = ref airNodesArray[i];
                var neighbours = kdTree.RadialSearch(airNode.position, airNodeMinDistance * airNodeMinDistance, airNodeMinDistance * airNodeMinDistance * 4);
            
                uint linkCount = 0;
                for (int j = 0; j < neighbours.Count; j++)
                {
                    var neighbor = neighbours[j];
            
                    int neighbourIndex = neighbor.Value;
                    if (neighbourIndex == i)
                    {
                        continue;
                    }
            
                    NodeGraph.Node neighbourAirNode = airNodes[neighbourIndex];
            
                    float distance = (neighbourAirNode.position - airNode.position).magnitude;
            
                    if (distance < airNodeMinDistance)
                    {
                        Log.Debug(distance);
                    }
            
                    if (distance > airNodeMinDistance * 2)
                    {
                        Log.Debug(distance);
                    }
            
                    airLinks.Add(new NodeGraph.Link
                    {
                        nodeIndexA = new NodeGraph.NodeIndex(i),
                        nodeIndexB = new NodeGraph.NodeIndex(neighbourIndex),
                        distanceScore = distance,
                        minJumpHeight = 0,
                        hullMask = 0xFFFFFFF,
                        jumpHullMask = 0xFFFFFFF,
                        gateIndex = 0
                    });
            
                    linkCount++;
                }
            
                airNode.linkListIndex = new NodeGraph.LinkListIndex
                {
                    index = airLinks.Count - (int)linkCount,
                    size = linkCount
                };
            }

            LogStats("links");

            NodeGraph airGraph = ScriptableObject.CreateInstance<NodeGraph>();
            airGraph.nodes = airNodesArray;
            airGraph.links = airLinks.ToArray();
            airGraph.Awake();

            Log.Debug($"nodes: {airNodesArray.Length}");
            Log.Debug($"links: {airLinks.Count}");

            return airGraph;

            void LogStats(string name)
            {
                Log.Debug($"{name}: {stopwatch.Elapsed}");
                stopwatch.Restart();
            }
        }

        private bool IsValidAirNodePosition(float[,,] map3d, float mapScale, Octree<int> kdTree, List<NodeGraph.Node> airNodes, Vector3 targetPosition)
        {
            float density = densityMap.GetDensity(map3d, targetPosition / mapScale);

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

            if (forbiddenHulls == (HullMask)7)
            {
                return false;
            }

            //var min = kdTree.GetNearestNeighbours(targetPosition, kdTree.Count)
            //    .Min(x => (x.Point - targetPosition).sqrMagnitude);
            //
            //var max = kdTree.GetNearestNeighbours(targetPosition, kdTree.Count)
            //    .Max(x => (x.Point - targetPosition).sqrMagnitude);

            float nearestDistanceSqr = kdTree.GetNearestNeighboursDistanceSqr(targetPosition);

            //if (Math.Abs(min - nearestDistanceSqr) > 0.5f)
            //{
            //    Log.Debug("Bugged");
            //    Log.Debug(nearestDistanceSqr);
            //    Log.Debug(min);
            //    Log.Debug(max);
            //    Log.Debug("----");
            //    Log.Debug(Mathf.Sqrt(nearestDistanceSqr));
            //    Log.Debug(Mathf.Sqrt(min));
            //    Log.Debug(Mathf.Sqrt(max));
            //}
            //

            if (nearestDistanceSqr != float.MaxValue)
            {
                if (nearestDistanceSqr < (airNodeMinDistance * airNodeMinDistance))
                {
                    return false;
                }
            
                if (nearestDistanceSqr > (airNodeMinDistance * airNodeMinDistance * 4))
                {
                    Log.Debug(data: "More than max!");
                    Log.Debug(Mathf.Sqrt(nearestDistanceSqr));
                    Log.Debug(airNodeMinDistance * 2);
                    return false;
                }
            }
            
            NodeGraph.Node airNode = new NodeGraph.Node();
            airNode.position = targetPosition;
            airNode.forbiddenHulls = forbiddenHulls;
            airNode.flags = NodeFlags.NoChestSpawn | NodeFlags.NoShrineSpawn | NodeFlags.NoCeiling;

            kdTree.Add(new Octree<int>.Point
            {
                Position = targetPosition,
                Value = airNodes.Count
            });
            airNodes.Add(airNode);
            return true;
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

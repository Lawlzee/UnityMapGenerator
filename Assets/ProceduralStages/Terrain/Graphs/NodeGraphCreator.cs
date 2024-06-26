﻿using RoR2.Navigation;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SocialPlatforms;

namespace ProceduralStages
{
    [Serializable]
    public class NodeGraphCreator
    {
        public float minFloorAngle = 0.4f;
        [Range(0, 90)]
        public float maxFloorLinkAngle = 45f;

        public float groundNodeHorizontalCellSize = 4f;
        public float groundNodeVerticalCellSize = 4f;
        [Range(0, 1)]
        public float groundNodeDisplacementStrength = 0.5f;
        public float groundNodeMinDistance = 4f;
        public float groundNodeMaxDistance = 4f;
        public float groundNodeRayLength = 4f;

        public float airNodeCellSize = 4f;
        public float airNodeMinDistance = 4f;
        public float airNodeMaxDistance = 4f;

        public DensityMap densityMap = new DensityMap();

        public Graphs CreateGraphs(Terrain terrain)
        {
            using (ProfilerLog.CreateScope("CreateGraphs"))
            {
                //MapDensity mapDensity = densityMap.Create(map);
                ///LogStats("mapDensity");

                Graphs graphs = CreateGroundNodes2(terrain);
                ProfilerLog.Debug("groundNodes");

                HashSet<int> mainIsland = GetMainIsland(graphs.ground);
                //HashSet<int> mainIsland = new HashSet<int>();
                ProfilerLog.Debug("mainIsland");

                SetMainIslandFlags(graphs.ground, mainIsland, terrain);
                ProfilerLog.Debug("SetMainIslandFlags");

                graphs.ground.Awake();

                //graphs.air = CreateAirNodes(graphs.ground, mainIsland, map, mapScale);
                graphs.air = CreateAirGraph(terrain);
                ProfilerLog.Debug("CreateAirNodes");

                return graphs;
            }
        }

        private struct FloorNode
        {
            public int index;
            public Vector3Int cellPos;
            public Vector3 normal;
            public NodeGraph.Node node;

        }

        private Graphs CreateGroundNodes2(Terrain terrain)
        {
            using (ProfilerLog.CreateScope("CreateGroundNodes2"))
            {
                float mapScale = MapGenerator.instance.mapScale;

                Vector3Int size = new Vector3Int(terrain.densityMap.GetLength(0), terrain.densityMap.GetLength(1), terrain.densityMap.GetLength(2));
                Vector3Int cellSize = new Vector3Int(
                    Mathf.FloorToInt(mapScale * size.x / groundNodeHorizontalCellSize),
                    Mathf.FloorToInt(mapScale * size.y / groundNodeVerticalCellSize),
                    Mathf.FloorToInt(mapScale * size.z / groundNodeHorizontalCellSize));

                FloorNode?[,,] floorNodes = new FloorNode?[cellSize.x, cellSize.y, cellSize.z];
                Vector3?[,,] ceilNodes = new Vector3?[cellSize.x, cellSize.y, cellSize.z];

                int[] floorNodeCounts = new int[cellSize.x];
                int[] ceilNodeCounts = new int[cellSize.x];

                Parallel.For(0, cellSize.x, x => 
                {
                    int floorNodeCount = 0;
                    int ceilNodeCount = 0;

                    float posX = x * groundNodeHorizontalCellSize;
                    for (int y = 0; y < cellSize.y; y++)
                    {
                        float posY = y * groundNodeVerticalCellSize;
                        for (int z = 0; z < cellSize.z; z++)
                        {
                            float posZ = z * groundNodeHorizontalCellSize;

                            Vector3 pointIntegral = new Vector3Int(
                                Mathf.FloorToInt(posX),
                                Mathf.FloorToInt(posY),
                                Mathf.FloorToInt(posZ));

                            Vector3 displacement = groundNodeDisplacementStrength * RandomPG.Random3(pointIntegral) * groundNodeHorizontalCellSize;
                            displacement.y = 0;

                            Vector3 nodePos = pointIntegral + displacement;

                            Vector3 samplePos = nodePos / mapScale;
                            float density = densityMap.GetDensity(terrain.densityMap, samplePos);

                            if (density >= 0.5f)
                            {
                                continue;
                            }

                            float floorDensity = densityMap.GetDensity(terrain.densityMap, samplePos + Vector3.down * groundNodeRayLength);
                            if (floorDensity >= 0.5f)
                            {
                                NodeGraph.Node node = new NodeGraph.Node();
                                node.position = nodePos;
                                node.flags = NodeFlags.NoCharacterSpawn | NodeFlags.NoChestSpawn | NodeFlags.NoShrineSpawn;

                                floorNodes[x, y, z] = new FloorNode
                                {
                                    cellPos = new Vector3Int(x, y, z),
                                    node = node
                                };

                                floorNodeCount++;
                            }

                            float ceilDensity = densityMap.GetDensity(terrain.densityMap, samplePos + Vector3.up * groundNodeRayLength);
                            if (ceilDensity >= 0.5f)
                            {
                                ceilNodes[x, y, z] = nodePos;
                                ceilNodeCount++;
                            }
                        }
                    }

                    floorNodeCounts[x] = floorNodeCount;
                    ceilNodeCounts[x] = ceilNodeCount;
                });

                int worldLayerMask = 1 << LayerIndex.world.intVal;

                int floorNodesCount = floorNodeCounts.Sum();
                ProfilerLog.Debug($"floorNodesCount: {floorNodesCount}");

                int ceilsNodeCount = ceilNodeCounts.Sum();
                ProfilerLog.Debug($"ceilsNodeCount: {ceilsNodeCount}");

                Dictionary<Vector3, PropsNode> nodeInfoByPosition = new Dictionary<Vector3, PropsNode>();
                PropsNode[] flattenCeilNodes = new PropsNode[ceilsNodeCount];
                PropsNode[] flattenFloorNodes = new PropsNode[floorNodesCount];
                FloorNode[] filteredFloorNodes = new FloorNode[floorNodesCount];

                int currentCeilNodeIndex = 0;
                int currentFloorNodeIndex = 0;
                int currentValidFloorNodeIndex = 0;
                for (int x = 0; x < cellSize.x; x++)
                {
                    for (int y = 0; y < cellSize.y; y++)
                    {
                        for (int z = 0; z < cellSize.z; z++)
                        {
                            Vector3? maybeCeilPos = ceilNodes[x, y, z];
                            if (maybeCeilPos != null)
                            {
                                Vector3 ceilPos = maybeCeilPos.Value;

                                if (Physics.Raycast(ceilPos, Vector3.up, out RaycastHit ceilHit, groundNodeRayLength, worldLayerMask))
                                {
                                    float angle = Vector3.Dot(Vector3.up, ceilHit.normal);
                                    if (angle < -minFloorAngle)
                                    {
                                        flattenCeilNodes[currentCeilNodeIndex] = new PropsNode
                                        {
                                            position = ceilHit.point,
                                            normal = ceilHit.normal
                                        };
                                        currentCeilNodeIndex++;
                                    }
                                }
                            }

                            FloorNode? maybeNode = floorNodes[x, y, z];
                            if (maybeNode == null)
                            {
                                floorNodes[x, y, z] = null;
                                continue;
                            }


                            FloorNode node = maybeNode.Value;

                            if (Physics.Raycast(node.node.position, Vector3.down, out RaycastHit floorHit, groundNodeRayLength, worldLayerMask))
                            {
                                float angle = Vector3.Dot(Vector3.up, floorHit.normal);
                                if (angle > minFloorAngle)
                                {
                                    float actualDensity = densityMap.GetDensity(terrain.floorlessDensityMap, floorHit.point / mapScale);

                                    HullMask forbiddenHulls = HullMask.None;
                                    if (actualDensity > densityMap.ground.maxHumanDensity)
                                    {
                                        forbiddenHulls |= HullMask.Human;
                                    }

                                    if (actualDensity > densityMap.ground.maxGolemDensity)
                                    {
                                        forbiddenHulls |= HullMask.Golem;
                                    }

                                    if (actualDensity > densityMap.ground.maxBeetleQueenDensity)
                                    {
                                        forbiddenHulls |= HullMask.BeetleQueen;
                                    }

                                    bool valid = forbiddenHulls != (HullMask)7;

                                    nodeInfoByPosition[floorHit.point] = new PropsNode
                                    {
                                        position = floorHit.point,
                                        normal = floorHit.normal
                                    };

                                    flattenFloorNodes[currentFloorNodeIndex] = new PropsNode
                                    {
                                        position = floorHit.point,
                                        normal = floorHit.normal
                                    };

                                    currentFloorNodeIndex++;

                                    if (forbiddenHulls != (HullMask)7)
                                    {
                                        node.index = currentValidFloorNodeIndex;
                                        node.node.position = floorHit.point;
                                        node.node.forbiddenHulls = forbiddenHulls;
                                        node.normal = floorHit.normal;
                                        floorNodes[x, y, z] = node;
                                        filteredFloorNodes[currentValidFloorNodeIndex] = node;
                                        currentValidFloorNodeIndex++;
                                    }
                                    else
                                    {
                                        floorNodes[x, y, z] = null;
                                    }
                                }
                                else
                                {
                                    floorNodes[x, y, z] = null;
                                }
                            }
                            else
                            {
                                floorNodes[x, y, z] = null;
                            }
                        }
                    }
                }

                ProfilerLog.Debug($"currentCeilNodeIndex: {currentCeilNodeIndex} / {ceilsNodeCount}");
                ProfilerLog.Debug($"currentFloorNodeIndex: {currentFloorNodeIndex} / {floorNodesCount}");
                ProfilerLog.Debug($"currentValidFloorNodeIndex: {currentValidFloorNodeIndex} / {floorNodesCount}");

                Array.Resize(ref flattenCeilNodes, currentCeilNodeIndex);
                ProfilerLog.Debug($"Array.Resize(flattenCeilNodes)");

                Array.Resize(ref flattenFloorNodes, currentFloorNodeIndex);
                ProfilerLog.Debug($"Array.Resize(flattenFloorNodes)");

                NodeGraph.Link?[] links = new NodeGraph.Link?[currentValidFloorNodeIndex * 26];

                int bandCount = 8;
                int[] linkCounts = new int[bandCount];

                NodeGraph.Node[] finalNodes = new NodeGraph.Node[currentValidFloorNodeIndex];

                float max = float.MinValue;
                float min = float.MaxValue;
                ParallelPG.For(0, currentValidFloorNodeIndex, bandCount, (bandIndex, minIndex, maxIndex) =>
                {
                    uint totalLinkCount = 0;

                    for (int nodeIndex = minIndex; nodeIndex < maxIndex; nodeIndex++)
                    {
                        FloorNode floorNode = filteredFloorNodes[nodeIndex];
                        NodeGraph.Node node = floorNode.node;
                        Vector3Int cellPos = floorNode.cellPos;

                        uint linkCount = 0;
                        for (int i = -1; i <= 1; i++)
                        {
                            if (cellPos.x + i < 0 || cellPos.x + i >= cellSize.x)
                            {
                                continue;
                            }

                            for (int j = -1; j <= 1; j++)
                            {
                                if (cellPos.y + j < 0 || cellPos.y + j >= cellSize.y)
                                {
                                    continue;
                                }

                                for (int k = -1; k <= 1; k++)
                                {
                                    if (cellPos.z + k < 0 || cellPos.z + k >= cellSize.z)
                                    {
                                        continue;
                                    }

                                    if (i == 0 && j == 0 && k == 0)
                                    {
                                        continue;
                                    }

                                    FloorNode? neighbord = floorNodes[cellPos.x + i, cellPos.y + j, cellPos.z + k];

                                    if (neighbord == null)
                                    {
                                        continue;
                                    }

                                    Vector3 delta = floorNode.node.position - neighbord.Value.node.position;
                                    float angle = Mathf.Rad2Deg * Mathf.Atan2(delta.y, Mathf.Sqrt(delta.x * delta.x + delta.z * delta.z));

                                    min = Mathf.Min(angle, min);
                                    max = Mathf.Max(angle, max);

                                    if (Mathf.Abs(angle) > maxFloorLinkAngle)
                                    {
                                        continue;
                                    }

                                    float distance = delta.magnitude;
                                    if (groundNodeMinDistance <= distance && distance < groundNodeMaxDistance)
                                    {
                                        links[nodeIndex * 26 + linkCount] = new NodeGraph.Link
                                        {
                                            nodeIndexA = new NodeGraph.NodeIndex(nodeIndex),
                                            nodeIndexB = new NodeGraph.NodeIndex(neighbord.Value.index),
                                            distanceScore = distance,
                                            minJumpHeight = 0,
                                            hullMask = 0xFFFFFFF,
                                            jumpHullMask = 0xFFFFFFF,
                                            gateIndex = 0
                                        };

                                        linkCount++;
                                    }
                                }
                            }
                        }

                        node.linkListIndex = new NodeGraph.LinkListIndex
                        {
                            size = linkCount
                        };
                        finalNodes[nodeIndex] = node;

                        totalLinkCount += linkCount;
                    }

                    linkCounts[bandIndex] = (int)totalLinkCount;
                });

                ProfilerLog.Debug($"min: {min}");
                ProfilerLog.Debug($"max: {max}");
                ProfilerLog.Debug($"linkCounts.Sum(): {linkCounts.Sum()}");
                NodeGraph.Link[] filteredLinks = new NodeGraph.Link[linkCounts.Sum()];

                int linkIndex = 0;
                for (int i = 0; i < finalNodes.Length; i++)
                {
                    ref NodeGraph.Node node = ref finalNodes[i];

                    node.linkListIndex.index = linkIndex;

                    uint linkCount = node.linkListIndex.size;

                    for (int j = 0; j < linkCount; j++)
                    {
                        filteredLinks[linkIndex] = links[i * 26 + j].Value;
                        linkIndex++;
                    }
                }

                ProfilerLog.Debug("filteredLinks");

                var nodeByPosition = finalNodes
                    .Select((x, i) => (
                        Index: i,
                        Position: x.position
                    ))
                    .ToDictionary(
                        x => x.Position,
                        x => x.Index);

                ProfilerLog.Debug("nodeByPosition");

                NodeGraph groundGraph = ScriptableObject.CreateInstance<NodeGraph>();
                groundGraph.nodes = finalNodes;
                groundGraph.links = filteredLinks;
                groundGraph.Awake();

                ProfilerLog.Debug($"nodes: {finalNodes.Length}");
                ProfilerLog.Debug($"links: {filteredLinks.Length}");

                return new Graphs
                {
                    ground = groundGraph,
                    floorProps = flattenFloorNodes,
                    ceilingProps = flattenCeilNodes,
                    groundNodeIndexByPosition = nodeByPosition,
                    nodeInfoByPosition = nodeInfoByPosition
                };
            }

        }

        private Graphs CreateGroundNodes(Terrain terrain)
        {
            var groundNodes = ScriptableObject.CreateInstance<NodeGraph>();

            var triangles = terrain.meshResult.triangles;
            var vertices = terrain.meshResult.vertices;
            var normals = terrain.meshResult.normals;

            Dictionary<Vector3, PropsNode> nodeInfoByPosition = new Dictionary<Vector3, PropsNode>();
            List<PropsNode> floorProps = new List<PropsNode>();
            List<PropsNode> ceilingProps = new List<PropsNode>();

            var nodes = new NodeGraph.Node[terrain.meshResult.verticesLength];

            int index = 0;
            for (int i = 0; i < terrain.meshResult.verticesLength; i++)
            {
                var vertex = vertices[i];
                var normal = normals[i];

                float angle = Vector3.Dot(Vector3.up, normal);

                float density = densityMap.GetDensity(terrain.floorlessDensityMap, vertex / MapGenerator.instance.mapScale);

                bool isFloor = angle > minFloorAngle
                    && density < densityMap.ground.maxHumanDensity;
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

                    HullMask forbiddenHulls = HullMask.None;

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
                .GroupBy(x => x.position)
                .ToDictionary(
                    x => x.Key,
                    x => x.First().linkListIndex.index);

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
                //floorProps = floorProps,
                //ceilingProps = ceilingProps,
                groundNodeIndexByPosition = nodeByPosition,
                nodeInfoByPosition = nodeInfoByPosition
            };
        }

        private HashSet<int> GetMainIsland(NodeGraph groundGraph)
        {
            var islands = GetNodeIslands(groundGraph.nodes, groundGraph.links)
                .OrderByDescending(x => x.Count)
                .ToList();

            Log.Debug("islands.Count: " + islands.Count);
            Log.Debug("islands[0].Count: " + islands[0].Count);

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

        private void SetMainIslandFlags(NodeGraph groundGraph, HashSet<int> mainIsland, Terrain terrain)
        {
            foreach (int nodeIndex in mainIsland)
            {
                ref NodeGraph.Node node = ref groundGraph.nodes[nodeIndex];

                NodeFlags flags = NodeFlags.NoCharacterSpawn | NodeFlags.NoChestSpawn | NodeFlags.NoShrineSpawn;

                float density = densityMap.GetDensity(terrain.floorlessDensityMap, node.position / MapGenerator.instance.mapScale);

                if (node.position.y <= terrain.maxGroundHeight)
                {
                    if (terrain.minInteractableHeight <= node.position.y)
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

        private struct AirNode
        {
            public int index;
            public Vector3Int cellPos;
            public NodeGraph.Node node;

        }

        private NodeGraph CreateAirGraph(Terrain terrain)
        {
            float mapScale = MapGenerator.instance.mapScale;

            Vector3Int size = new Vector3Int(terrain.densityMap.GetLength(0), terrain.densityMap.GetLength(1), terrain.densityMap.GetLength(2));
            Vector3Int cellSize = new Vector3Int(
                Mathf.FloorToInt(mapScale * size.x / airNodeCellSize),
                Mathf.FloorToInt(mapScale * size.y / airNodeCellSize),
                Mathf.FloorToInt(mapScale * size.z / airNodeCellSize));

            AirNode?[,,] nodes = new AirNode?[cellSize.x, cellSize.y, cellSize.z];

            int[] nodeCounts = new int[cellSize.x];

            Parallel.For(0, cellSize.x, x =>
            {
                int nodeCount = 0;

                float posX = x * airNodeCellSize;
                for (int y = 0; y < cellSize.y; y++)
                {
                    float posY = y * airNodeCellSize;
                    for (int z = 0; z < cellSize.z; z++)
                    {
                        float posZ = z * airNodeCellSize;

                        Vector3 pointIntegral = new Vector3Int(
                            Mathf.FloorToInt(posX),
                            Mathf.FloorToInt(posY),
                            Mathf.FloorToInt(posZ));
                        //Vector3 pointFractional = new Vector3(posX, posY, posZ) - pointIntegral;

                        Vector3 displacement = RandomPG.Random3(pointIntegral) * airNodeCellSize;

                        Vector3 nodePos = pointIntegral + displacement;

                        float density = densityMap.GetDensity(terrain.densityMap, nodePos / mapScale);

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
                            continue;
                        }

                        NodeGraph.Node airNode = new NodeGraph.Node();
                        airNode.position = nodePos;
                        airNode.forbiddenHulls = forbiddenHulls;
                        airNode.flags = NodeFlags.NoChestSpawn | NodeFlags.NoShrineSpawn | NodeFlags.NoCeiling;

                        nodes[x, y, z] = new AirNode
                        {
                            cellPos = new Vector3Int(x, y, z),
                            node = airNode
                        };
                        nodeCount++;
                    }
                }

                nodeCounts[x] = nodeCount;
            });

            AirNode[] filteredNodes = new AirNode[nodeCounts.Sum()];

            int currentNodeIndex = 0;
            for (int x = 0; x < cellSize.x; x++)
            {
                for (int y = 0; y < cellSize.y; y++)
                {
                    for (int z = 0; z < cellSize.z; z++)
                    {
                        AirNode? maybeNode = nodes[x, y, z];
                        if (maybeNode == null)
                        {
                            continue;
                        }

                        AirNode node = maybeNode.Value;
                        node.index = currentNodeIndex;

                        nodes[x, y, z] = node;
                        filteredNodes[currentNodeIndex] = node;
                        currentNodeIndex++;
                    }
                }
            }

            NodeGraph.Link?[] links = new NodeGraph.Link?[filteredNodes.Length * 26];

            int bandCount = 8;
            int[] linkCounts = new int[bandCount];

            NodeGraph.Node[] finalNodes = new NodeGraph.Node[filteredNodes.Length];

            ParallelPG.For(0, filteredNodes.Length, bandCount, (bandIndex, minIndex, maxIndex) =>
            {
                uint totalLinkCount = 0;

                for (int nodeIndex = minIndex; nodeIndex < maxIndex; nodeIndex++)
                {
                    AirNode airNode = filteredNodes[nodeIndex];
                    NodeGraph.Node node = airNode.node;
                    Vector3Int cellPos = airNode.cellPos;

                    uint linkCount = 0;
                    for (int i = -1; i <= 1; i++)
                    {
                        if (cellPos.x + i < 0 || cellPos.x + i >= cellSize.x)
                        {
                            continue;
                        }

                        for (int j = -1; j <= 1; j++)
                        {
                            if (cellPos.y + j < 0 || cellPos.y + j >= cellSize.y)
                            {
                                continue;
                            }

                            for (int k = -1; k <= 1; k++)
                            {
                                if (cellPos.z + k < 0 || cellPos.z + k >= cellSize.z)
                                {
                                    continue;
                                }

                                if (i == 0 && j == 0 && k == 0)
                                {
                                    continue;
                                }

                                AirNode? neighbord = nodes[cellPos.x + i, cellPos.y + j, cellPos.z + k];

                                if (neighbord == null)
                                {
                                    continue;
                                }

                                float distance = (airNode.node.position - neighbord.Value.node.position).magnitude;
                                if (airNodeMinDistance <= distance && distance < airNodeMaxDistance)
                                {
                                    links[nodeIndex * 26 + linkCount] = new NodeGraph.Link
                                    {
                                        nodeIndexA = new NodeGraph.NodeIndex(nodeIndex),
                                        nodeIndexB = new NodeGraph.NodeIndex(neighbord.Value.index),
                                        distanceScore = distance,
                                        minJumpHeight = 0,
                                        hullMask = 0xFFFFFFF,
                                        jumpHullMask = 0xFFFFFFF,
                                        gateIndex = 0
                                    };

                                    linkCount++;
                                }
                            }
                        }
                    }

                    node.linkListIndex = new NodeGraph.LinkListIndex
                    {
                        size = linkCount
                    };
                    finalNodes[nodeIndex] = node;

                    totalLinkCount += linkCount;
                }

                linkCounts[bandIndex] = (int)totalLinkCount;
            });

            NodeGraph.Link[] filteredLinks = new NodeGraph.Link[linkCounts.Sum()];

            int linkIndex = 0;
            for (int i = 0; i < finalNodes.Length; i++)
            {
                ref NodeGraph.Node node = ref finalNodes[i];

                node.linkListIndex.index = linkIndex;

                uint linkCount = node.linkListIndex.size;

                for (int j = 0; j < linkCount; j++)
                {
                    filteredLinks[linkIndex] = links[i * 26 + j].Value;
                    linkIndex++;
                }
            }

            NodeGraph airGraph = ScriptableObject.CreateInstance<NodeGraph>();
            airGraph.nodes = finalNodes;
            airGraph.links = filteredLinks;
            airGraph.Awake();

            Log.Debug($"nodes: {finalNodes.Length}");
            Log.Debug($"links: {filteredLinks.Length}");

            return airGraph;
        }
    }
}

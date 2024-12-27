using RoR2.Navigation;
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
                Graphs graphs = CreateGroundNodes(terrain);
                ProfilerLog.Debug("groundNodes");

                graphs.air = CreateAirGraph(terrain);
                ProfilerLog.Debug("CreateAirNodes");

                if (terrain.moonTerrain == null)
                {
                    HashSet<int> mainIsland = GetMainIsland(graphs.ground);
                    //HashSet<int> mainIsland = new HashSet<int>();
                    ProfilerLog.Debug("mainIsland");

                    SetMainIslandFlags(graphs.ground, mainIsland, terrain);
                    ProfilerLog.Debug("SetMainIslandFlags");
                }
                else
                {
                    SetGroundNodeGraphFlags(graphs.ground, terrain);
                    graphs.ground = MergeGraphs(graphs.ground, terrain.moonTerrain.arenaGroundGraph);
                    graphs.air = MergeGraphs(graphs.air, terrain.moonTerrain.arenaAirGraph);
                    //graphs.ground = terrain.moonTerrain.arenaGroundGraph;
                    //graphs.air = terrain.moonTerrain.arenaAirGraph;
                }

                graphs.ground.Awake();
                graphs.air.Awake();

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

        public Graphs CreateBackdropGraphs(MeshBackdropTerrain backdropTerrain)
        {
            var vertices = backdropTerrain.meshResult.vertices;
            var normals = backdropTerrain.meshResult.normals;
            int verticesLength = backdropTerrain.meshResult.verticesLength;

            PropsNode[] floorProps = new PropsNode[verticesLength];

            int index = 0;
            for (int i = 0; i < verticesLength; i++)
            {
                Vector3 normal = normals[i];
                float angle = Vector3.Dot(Vector3.up, normal);
                if (angle > minFloorAngle)
                {
                    floorProps[index] = new PropsNode
                    {
                        normal = normal,
                        position = vertices[i]
                    };
                    index++;
                }
            }

            Array.Resize(ref floorProps, index);

            return new Graphs
            {
                floorProps = floorProps,
                ceilingProps = new PropsNode[0],
                groundNodeIndexByPosition = new Dictionary<Vector3, int>()
            };
        }

        private Graphs CreateGroundNodes(Terrain terrain)
        {
            using (ProfilerLog.CreateScope("CreateGroundNodes"))
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


                                //Add NoCeiling to half of the nodes, so that lunar golems can spawn
                                if ((x + y + z) % 2 == 0)
                                {
                                    node.flags |= NodeFlags.NoCeiling;
                                }

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

        private HashSet<int> GetMainIsland(NodeGraph groundGraph)
        {
            var islands = GetNodeIslands(groundGraph.nodes, groundGraph.links)
                .OrderByDescending(x => x.Count)
                .ToList();

            if (islands.Count == 0)
            {
                return new HashSet<int>();
            }

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
                SetGroundNodeFlags(terrain, ref node);
            }
        }

        private void SetGroundNodeGraphFlags(NodeGraph groundGraph, Terrain terrain)
        {
            for (int i = 0; i < groundGraph.nodes.Length; i++)
            {
                ref NodeGraph.Node node = ref groundGraph.nodes[i];
                SetGroundNodeFlags(terrain, ref node);
            }
        }

        private void SetGroundNodeFlags(Terrain terrain, ref NodeGraph.Node node)
        {
            float density = densityMap.GetDensity(terrain.floorlessDensityMap, node.position / MapGenerator.instance.mapScale);

            if (node.position.y <= terrain.maxGroundHeight)
            {
                if (terrain.minInteractableHeight <= node.position.y)
                {
                    if (densityMap.minTeleporterDensity <= density && density <= densityMap.maxTeleporterDensity)
                    {
                        node.flags |= NodeFlags.TeleporterOK;
                    }

                    if (density < densityMap.maxChestDensity)
                    {
                        node.flags &= ~NodeFlags.NoChestSpawn;
                    }

                    if (density < densityMap.maxChestDensity)
                    {
                        node.flags &= ~NodeFlags.NoShrineSpawn;
                    }

                    if (densityMap.minNewtDensity <= density && density <= densityMap.maxNewtDensity)
                    {
                        node.flags |= NodeFlagsExt.Newt;
                    }
                }

                if (density < densityMap.maxSpawnDensity)
                {
                    node.flags &= ~NodeFlags.NoCharacterSpawn;                       
                }
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
            float airScale = terrain.generator.airNodesScale * airNodeCellSize;
            float mapScale = MapGenerator.instance.mapScale;

            Vector3Int size = new Vector3Int(terrain.densityMap.GetLength(0), terrain.densityMap.GetLength(1), terrain.densityMap.GetLength(2));
            Vector3Int cellSize = new Vector3Int(
                Mathf.FloorToInt(mapScale * size.x / airScale),
                Mathf.FloorToInt(mapScale * size.y / airScale),
                Mathf.FloorToInt(mapScale * size.z / airScale));

            AirNode?[,,] nodes = new AirNode?[cellSize.x, cellSize.y, cellSize.z];

            int[] nodeCounts = new int[cellSize.x];

            Parallel.For(0, cellSize.x, x =>
            {
                int nodeCount = 0;

                float posX = x * airScale;
                for (int y = 0; y < cellSize.y; y++)
                {
                    float posY = y * airScale;
                    for (int z = 0; z < cellSize.z; z++)
                    {
                        float posZ = z * airScale;

                        Vector3 pointIntegral = new Vector3Int(
                            Mathf.FloorToInt(posX),
                            Mathf.FloorToInt(posY),
                            Mathf.FloorToInt(posZ));
                        //Vector3 pointFractional = new Vector3(posX, posY, posZ) - pointIntegral;

                        Vector3 displacement = RandomPG.Random3(pointIntegral) * airScale;

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
                                if (airNodeMinDistance * terrain.generator.airNodesScale <= distance && distance < airNodeMaxDistance * terrain.generator.airNodesScale)
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

            Log.Debug($"nodes: {finalNodes.Length}");
            Log.Debug($"links: {filteredLinks.Length}");

            return airGraph;
        }

        private NodeGraph MergeGraphs(NodeGraph graph1, NodeGraph graph2)
        {
            NodeGraph.Node[] nodes = new NodeGraph.Node[graph1.nodes.Length + graph2.nodes.Length];
            NodeGraph.Link[] links = new NodeGraph.Link[graph1.links.Length + graph2.links.Length];

            Array.Copy(graph1.nodes, nodes, graph1.nodes.Length);
            Array.Copy(graph1.links, links, graph1.links.Length);

            for (int i = 0; i < graph2.nodes.Length; i++)
            {
                NodeGraph.Node node = graph2.nodes[i];
                node.linkListIndex.index += graph1.links.Length;

                nodes[i + graph1.nodes.Length] = node;
            }

            for (int i = 0; i < graph2.links.Length; i++)
            {
                NodeGraph.Link link = graph2.links[i];
                link.nodeIndexA = new NodeGraph.NodeIndex(link.nodeIndexA.nodeIndex + graph1.nodes.Length);
                link.nodeIndexB = new NodeGraph.NodeIndex(link.nodeIndexB.nodeIndex + graph1.nodes.Length);

                links[i + graph1.links.Length] = link;
            }

            NodeGraph resultNodeGraph = ScriptableObject.CreateInstance<NodeGraph>();
            resultNodeGraph.nodes = nodes;
            resultNodeGraph.links = links;
            //todo: merge gate names
            resultNodeGraph.gateNames = graph2.gateNames;

            return resultNodeGraph;
        }
    }
}

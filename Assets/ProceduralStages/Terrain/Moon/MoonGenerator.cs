using HG;
using RoR2;
using RoR2.Navigation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using UnityMeshSimplifier;

namespace ProceduralStages
{
    [CreateAssetMenu(fileName = "MoonGenerator", menuName = "ProceduralStages/MoonGenerator", order = 2)]
    public class MoonGenerator : TerrainGenerator
    {
        public Spheres spheres;

        public Vector3 arenaZoneScale;
        public Vector3 arenaZoneOffset;

        public float arenaDistance;

        public Voronoi3D voronoi3D;
        public ThreadSafeCurve voronoiRemapCurve;
        public ThreadSafeCurve sphereDistanceCurve;


        public GameObject gravitySpherePrefab;
        public GameObject gravityCylinderPrefab;
        public float antiGravitySphereScale;
        public GameObject antiGravitySpherePrefab;

        [Serializable]
        public struct Spheres
        {
            public int maxAttempt;
            public IntervalInt count;
            public Interval radius;
            public float minDistance;
            public float buffer;
            public float landScale;

            public int bubbleRenderQueue;
            public string bubbleMaterialKey;
            public Texture2D antigravityColorRemapRamp;
            public Texture2D gravityColorRemapRamp;
        }

        public struct Sphere
        {
            public GameObject gravitySphere;
            public Vector3 position;
            public float radius;
        }

        public override Terrain Generate()
        {
            var stageSize = MapGenerator.instance.stageSize;
            var rng = MapGenerator.rng;

            float arenaAngle = rng.nextNormalizedFloat * 2 * Mathf.PI;

            Vector3 arenaPosition = new Vector3(
                arenaDistance * Mathf.Cos(arenaAngle) * stageSize.x + stageSize.x / 2f,
                0,
                arenaDistance * Mathf.Sin(arenaAngle) * stageSize.z + stageSize.z / 2f);

            GameObject arenaGravityZone = Instantiate(gravityCylinderPrefab);
            arenaGravityZone.transform.position = MapGenerator.instance.mapScale * arenaPosition + arenaZoneOffset;
            arenaGravityZone.transform.localScale = arenaZoneScale;

            Material antiGravitySphereMaterial = new Material(Addressables.LoadAssetAsync<Material>(spheres.bubbleMaterialKey).WaitForCompletion());
            antiGravitySphereMaterial.renderQueue = spheres.bubbleRenderQueue;
            antiGravitySphereMaterial.SetTexture("_RemapTex", spheres.antigravityColorRemapRamp);

            Material gravitySphereMaterial = new Material(antiGravitySphereMaterial);
            gravitySphereMaterial.SetTexture("_RemapTex", spheres.gravityColorRemapRamp);

            Vector3 stageCenter = (Vector3)stageSize / 2f;

            var antiGravitySphere = Instantiate(antiGravitySpherePrefab);
            antiGravitySphere.GetComponent<MeshRenderer>().material = antiGravitySphereMaterial;

            float antiGravitySphereScale = this.antiGravitySphereScale * Mathf.Max(stageSize.x, stageCenter.y, stageCenter.z);
            antiGravitySphere.transform.localScale = MapGenerator.instance.mapScale * new Vector3(antiGravitySphereScale, antiGravitySphereScale, antiGravitySphereScale);
            antiGravitySphere.transform.position = new Vector3(stageCenter.x, 0, stageCenter.z) * MapGenerator.instance.mapScale;


            var moonObject = SceneManager.GetActiveScene().GetRootGameObjects().Single(x => x.name == "Moon");

            MoonPillars moonPillars = moonObject.GetComponentInChildren<MoonPillars>();
            moonPillars.pillarPositions = new List<Vector3>();
            moonPillars.rng = new Xoroshiro128Plus(rng.nextUlong);
            moonPillars.globalSphereScaleCurve = antiGravitySphere.GetComponent<ObjectScaleCurve>();
            moonPillars.globalSphereScaleCurve.baseScale = antiGravitySphere.transform.localScale;

            Transform brotherMissionControllerTransform = moonObject.transform.Find("BrotherMissionController");
            Transform gameplaySpaceTransform = moonObject.transform.Find("HOLDER: Gameplay Space");

            Vector3 arenaDeltaPos = MapGenerator.instance.mapScale * arenaPosition - gameplaySpaceTransform.position;
            brotherMissionControllerTransform.position += arenaDeltaPos;

            gameplaySpaceTransform.position = MapGenerator.instance.mapScale * arenaPosition;

            GameObject arena = MoonArena.AddArena(MapGenerator.instance.mapScale * arenaPosition);
            //GameObject arenaZone = Instantiate(gravityCylinderPrefab);
            //arenaZone.transform.localScale = MapGenerator.instance.mapScale * new Vector3(2 * arenaZoneRadius, stageSize.y, 2 * arenaZoneRadius);
            //arenaZone.transform.position = MapGenerator.instance.mapScale * arenaPosition;
            
            List<Sphere> sphereZones = new List<Sphere>();

            int sphereCount = rng.RangeInt(spheres.count.min, spheres.count.max);

            for (int i = 0; i < spheres.maxAttempt && sphereZones.Count < sphereCount; i++)
            {
                float radius = rng.RangeFloat(spheres.radius.min, spheres.radius.max);
                Vector3 position = new Vector3(
                    rng.RangeFloat(radius + spheres.buffer, stageSize.x - radius - spheres.buffer),
                    rng.RangeFloat(radius + spheres.buffer, stageSize.y - radius - spheres.buffer),
                    rng.RangeFloat(radius + spheres.buffer, stageSize.z - radius - spheres.buffer));

                bool validPosition = true;
                for (int j = 0; j < sphereZones.Count; j++)
                {
                    Vector3 delta = sphereZones[j].position - position;

                    float distance = Mathf.Max(Mathf.Abs(delta.x), Mathf.Abs(delta.y), Mathf.Abs(delta.z)) - radius - sphereZones[j].radius;

                    if (distance < spheres.minDistance)
                    {
                        validPosition = false;
                        break;
                    }
                }

                if (!validPosition)
                {
                    continue;
                }

                GameObject gravitySphere = Instantiate(gravitySpherePrefab);
                gravitySphere.transform.position = position * MapGenerator.instance.mapScale;
                gravitySphere.transform.localScale = 2 * new Vector3(radius, radius, radius) * MapGenerator.instance.mapScale;
                gravitySphere.GetComponent<MeshRenderer>().material = gravitySphereMaterial;

                sphereZones.Add(new Sphere
                {
                    position = position,
                    radius = radius,
                    gravitySphere = gravitySphere
                });
            }

            int pillarCount = rng.RangeInt(4, 8);
            for (int i = 1; i <= pillarCount && i < sphereZones.Count; i++)
            {
                moonPillars.pillarPositions.Add(sphereZones[i].position * MapGenerator.instance.mapScale);
            }

            GameObject dropship = null;

            if (NetworkServer.active)
            {
                dropship = MoonDropship.Place(sphereZones[0].position * MapGenerator.instance.mapScale, moonObject);

                moonObject.transform.Find("MoonEscapeSequence").GetComponent<MoonEscapeSequence>().dropshipZone = dropship;
            }

            float[,,] densityMap = new float[stageSize.x, stageSize.y, stageSize.z];

            Parallel.ForEach(sphereZones, sphereZone =>
            {
                int minX = Mathf.Clamp(Mathf.FloorToInt(sphereZone.position.x - sphereZone.radius - spheres.buffer), 0, stageSize.x - 1);
                int maxX = Mathf.Clamp(Mathf.CeilToInt(sphereZone.position.x + sphereZone.radius + spheres.buffer), 0, stageSize.x - 1);

                int minY = Mathf.Clamp(Mathf.FloorToInt(sphereZone.position.y - sphereZone.radius - spheres.buffer), 0, stageSize.y - 1);
                int maxY = Mathf.Clamp(Mathf.CeilToInt(sphereZone.position.y + sphereZone.radius + spheres.buffer), 0, stageSize.y - 1);

                int minZ = Mathf.Clamp(Mathf.FloorToInt(sphereZone.position.z - sphereZone.radius - spheres.buffer), 0, stageSize.z - 1);
                int maxZ = Mathf.Clamp(Mathf.CeilToInt(sphereZone.position.z + sphereZone.radius + spheres.buffer), 0, stageSize.z - 1);


                for (int y = minY; y <= maxY; y++)
                {
                    bool isBottomHalf = y < sphereZone.position.y;
                    float verticalDistance = Mathf.Clamp01(0.5f + (sphereZone.position.y - y) / sphereZone.radius);

                    for (int x = minX; x <= maxX; x++)
                    {
                        for (int z = minZ; z <= maxZ; z++)
                        {
                            Vector3 position = new Vector3(x, y, z);


                            float distance = (position - sphereZone.position).magnitude;

                            float density = verticalDistance;

                            if (isBottomHalf)
                            {
                                density = Mathf.Min(density, Mathf.Clamp01(1 - spheres.landScale * distance / sphereZone.radius));
                            }

                            densityMap[x, y, z] = density;
                        }
                    }
                }
            });

            List<GameObject> customObjects = new List<GameObject>();
            customObjects.AddRange(sphereZones.Select(x => x.gravitySphere));
            customObjects.Add(antiGravitySphere);
            customObjects.Add(dropship);
            customObjects.Add(arena);
            customObjects.Add(arenaGravityZone);

            MoonExitOrbSpawner[] exitOrbs = moonObject.GetComponentsInChildren<MoonExitOrbSpawner>(includeInactive: true);
            exitOrbs[0].transform.parent.position += arenaDeltaPos;

            for (int i = 0; i < exitOrbs.Length; i++)
            {
                Vector3Int destination;
                while (true)
                {
                    destination = new Vector3Int(
                        rng.RangeInt(0, stageSize.x),
                        rng.RangeInt(0, stageSize.y),
                        rng.RangeInt(0, stageSize.z));

                    if (densityMap[destination.x, destination.y, destination.z] < 0.5f)
                    {
                        break;
                    }
                }

                GameObject orbDestination = new GameObject("OrbDestination");
                orbDestination.transform.position = (Vector3)destination * MapGenerator.instance.mapScale;

                exitOrbs[i].explicitDestination = orbDestination.transform;
                customObjects.Add(orbDestination);
            }

            var meshResult = MarchingCubes.CreateMesh(densityMap, MapGenerator.instance.mapScale);
            ProfilerLog.Debug("marchingCubes");


            //customObjects.Add(arenaZone);

            moonObject.SetActive(true);

            MoonTerrain moonTerrain = new MoonTerrain();
            SetArenaNodeGraph(moonTerrain, arenaDeltaPos);

            return new Terrain
            {
                meshResult = meshResult,
                floorlessDensityMap = new float[stageSize.x, stageSize.y, stageSize.z],
                densityMap = densityMap,
                maxGroundHeight = float.MaxValue,
                oobScale = new Vector3(4, 6, 4),
                customObjects = customObjects.ToArray(),
                moonTerrain = moonTerrain
            };
        }

        private void SetArenaNodeGraph(MoonTerrain moonTerrain, Vector3 arenaOffset)
        {
            moonTerrain.arenaGroundGraph = CreateSubGraph("RoR2/Base/moon2/moon2_GroundNodeGraph.asset", arenaOffset);
            Log.Debug("moonTerrain.arenaGroundGraph.nodes.Length " + moonTerrain.arenaGroundGraph.nodes.Length);
            Log.Debug("moonTerrain.arenaGroundGraph.links.Length " + moonTerrain.arenaGroundGraph.links.Length);
            moonTerrain.arenaAirGraph = CreateSubGraph("RoR2/Base/moon2/moon2_AirNodeGraph.asset", arenaOffset);
            Log.Debug("moonTerrain.arenaAirGraph.nodes.Length " + moonTerrain.arenaAirGraph.nodes.Length);
            Log.Debug("moonTerrain.arenaAirGraph.links.Length " + moonTerrain.arenaAirGraph.links.Length);
        }

        private NodeGraph CreateSubGraph(string graphKey, Vector3 arenaOffset)
        {
            NodeGraph nodeGraph = Addressables.LoadAssetAsync<NodeGraph>(graphKey).WaitForCompletion();

            List<NodeGraph.Node> arenaNodes = new List<NodeGraph.Node>(nodeGraph.nodes.Length);
            int[] nodeReIndex = new int[nodeGraph.nodes.Length];

            int linkCount = 0;
            int nodeIndex = 0;
            for (int i = 0; i < nodeGraph.nodes.Length; i++)
            {
                NodeGraph.Node node = nodeGraph.nodes[i];

                if (node.position.y < 400)
                {
                    nodeReIndex[i] = -1;
                    continue;
                }

                node.position += arenaOffset;
                arenaNodes.Add(node);
                nodeReIndex[i] = nodeIndex;

                linkCount += (int)node.linkListIndex.size;
                nodeIndex++;
            }

            NodeGraph.Link[] nodeLinks = new NodeGraph.Link[linkCount];

            int linkIndex = 0;
            for (int i = 0; i < arenaNodes.Count; i++)
            {
                NodeGraph.Node node = arenaNodes[i];
                int newLinkIndex = linkIndex;

                for (int j = 0; j < node.linkListIndex.size; j++)
                {
                    var link = nodeGraph.links[node.linkListIndex.index + j];
                    link.nodeIndexA = new NodeGraph.NodeIndex(nodeReIndex[link.nodeIndexA.nodeIndex]);
                    link.nodeIndexB = new NodeGraph.NodeIndex(nodeReIndex[link.nodeIndexB.nodeIndex]);

                    nodeLinks[linkIndex] = link;
                    linkIndex++;
                }

                node.linkListIndex.index = newLinkIndex;
                arenaNodes[i] = node;
            }

            NodeGraph resultNodeGraph = CreateInstance<NodeGraph>();
            resultNodeGraph.nodes = arenaNodes.ToArray();
            resultNodeGraph.links = nodeLinks;
            resultNodeGraph.gateNames = nodeGraph.gateNames;

            return resultNodeGraph;
        }
    }
}

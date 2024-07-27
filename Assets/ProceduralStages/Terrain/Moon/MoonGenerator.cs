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
using UnityEngine.SceneManagement;

namespace ProceduralStages
{
    [CreateAssetMenu(fileName = "MoonGenerator", menuName = "ProceduralStages/MoonGenerator", order = 2)]
    public class MoonGenerator : TerrainGenerator
    {
        public Spheres spheres;

        public Vector3 arenaZoneScale;
        public Vector3 arenaZoneOffset;

        public float arenaDistance;

        public GameObject gravitySpherePrefab;
        public GameObject gravityCylinderPrefab;
        public float antiGravitySphereScale;
        public GameObject antiGravitySpherePrefab;

        public string redCauldronKey;
        public string greenCauldronKey;
        public string whiteCauldronKey;
        public string lunarPodKey;

        private Vector3 arenaPosition;
        private List<Sphere> sphereZones;

        [Serializable]
        public struct Spheres
        {
            public int maxAttempt;
            public IntervalInt count;
            public Interval radius;
            public float minDistance;
            public float buffer;
            public float landScale;
            [Range(0f, 1f)]
            public float maxObjectifDistance;

            public FBM floorFBM;
            public ThreadSafeCurve floorCurve;
            public ThreadSafeCurve distanceFloorMultiplierCurve;

            public int bubbleRenderQueue;
            public string bubbleMaterialKey;
            public Texture2D antigravityColorRemapRamp;
            public Texture2D gravityColorRemapRamp;
            public Texture2D shipColorRemapRamp;
            public Texture2D lootColorRemapRamp;
            public Texture2D pillarColorRemapRamp;
            public Texture2D spawnColorRemapRamp;
        }

        public struct Sphere
        {
            public Vector3 position;
            public float radius;

            public int seedX;
            public int seedZ;
        }

        public override Terrain Generate()
        {
            var stageSize = MapGenerator.instance.stageSize;
            var rng = MapGenerator.rng;

            float arenaAngle = rng.nextNormalizedFloat * 2 * Mathf.PI;

            arenaPosition = new Vector3(
                arenaDistance * Mathf.Cos(arenaAngle) * stageSize.x + stageSize.x / 2f,
                0,
                arenaDistance * Mathf.Sin(arenaAngle) * stageSize.z + stageSize.z / 2f);

            Vector3 stageCenter = (Vector3)stageSize / 2f;

            sphereZones = new List<Sphere>();

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

                sphereZones.Add(new Sphere
                {
                    position = position,
                    radius = radius,
                    seedX = rng.RangeInt(0, short.MaxValue),
                    seedZ = rng.RangeInt(0, short.MaxValue)
                });
            }

            sphereZones.Sort((a, b) => a.radius > b.radius ? -1 : 1);

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
                    float verticalDistance = 0.5f + (sphereZone.position.y - y) / sphereZone.radius;

                    for (int x = minX; x <= maxX; x++)
                    {
                        for (int z = minZ; z <= maxZ; z++)
                        {
                            Vector3 position = new Vector3(x, y, z);
                            float distance = (position - sphereZone.position).magnitude / sphereZone.radius;

                            float floorNoise = spheres.floorCurve.Evaluate(0.5f * (spheres.floorFBM.Evaluate(x + sphereZone.seedX, z + sphereZone.seedZ) + 1));
                            float scaledFloorNoise = spheres.distanceFloorMultiplierCurve.Evaluate(distance) * floorNoise;
                            float verticalNoise = verticalDistance + scaledFloorNoise;

                            float density = verticalNoise;

                            if (isBottomHalf)
                            {
                                density = Mathf.Min(density, Mathf.Clamp01(1 - spheres.landScale * distance));
                            }

                            densityMap[x, y, z] = density;
                        }
                    }
                }
            });

            var moonObject = SceneManager.GetActiveScene().GetRootGameObjects().Single(x => x.name == "Moon").gameObject;
            Transform gameplaySpaceTransform = moonObject.transform.Find("HOLDER: Gameplay Space");
            gameplaySpaceTransform.Find("HOLDER: Final Arena").Find("ArenaTrigger").GetComponent<AllPlayersTrigger>().enabled = true;

            MoonTerrain moonTerrain = new MoonTerrain();
            Vector3 arenaDeltaPos = MapGenerator.instance.mapScale * arenaPosition - gameplaySpaceTransform.position;
            SetArenaNodeGraph(moonTerrain, arenaDeltaPos);

            var meshResult = MarchingCubes.CreateMesh(densityMap, MapGenerator.instance.mapScale);
            ProfilerLog.Debug("marchingCubes");

            return new Terrain
            {
                generator = this,
                meshResult = meshResult,
                floorlessDensityMap = new float[stageSize.x, stageSize.y, stageSize.z],
                densityMap = densityMap,
                maxGroundHeight = float.MaxValue,
                oobScale = new Vector3(4, 6, 4),
                customObjects = new List<GameObject>(),
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

        public override void AddProps(Terrain terrain, Graphs graphs)
        {
            var rng = MapGenerator.rng;

            var stageSize = MapGenerator.instance.stageSize;
            Vector3 stageCenter = (Vector3)stageSize / 2f;

            GameObject arenaGravityZone = Instantiate(gravityCylinderPrefab);
            arenaGravityZone.transform.position = MapGenerator.instance.mapScale * arenaPosition + arenaZoneOffset;
            arenaGravityZone.transform.localScale = arenaZoneScale;
            terrain.customObjects.Add(arenaGravityZone);

            Material antiGravitySphereMaterial = new Material(Addressables.LoadAssetAsync<Material>(spheres.bubbleMaterialKey).WaitForCompletion());
            antiGravitySphereMaterial.renderQueue = spheres.bubbleRenderQueue;
            antiGravitySphereMaterial.SetTexture("_RemapTex", spheres.antigravityColorRemapRamp);

            var antiGravitySphere = Instantiate(antiGravitySpherePrefab);
            antiGravitySphere.GetComponent<MeshRenderer>().material = antiGravitySphereMaterial;

            Material gravitySphereMaterial = new Material(antiGravitySphereMaterial);
            gravitySphereMaterial.SetTexture("_RemapTex", spheres.gravityColorRemapRamp);

            Material shipSphereMaterial = new Material(antiGravitySphereMaterial);
            shipSphereMaterial.SetTexture("_RemapTex", spheres.shipColorRemapRamp);

            Material lootSphereMaterial = new Material(antiGravitySphereMaterial);
            lootSphereMaterial.SetTexture("_RemapTex", spheres.lootColorRemapRamp);

            Material pillarSphereMaterial = new Material(antiGravitySphereMaterial);
            pillarSphereMaterial.SetTexture("_RemapTex", spheres.pillarColorRemapRamp);

            Material spawnSphereMaterial = new Material(antiGravitySphereMaterial);
            spawnSphereMaterial.SetTexture("_RemapTex", spheres.spawnColorRemapRamp);

            float antiGravitySphereScale = this.antiGravitySphereScale * Mathf.Max(stageSize.x, stageCenter.y, stageCenter.z);
            antiGravitySphere.transform.localScale = MapGenerator.instance.mapScale * new Vector3(antiGravitySphereScale, antiGravitySphereScale, antiGravitySphereScale);
            antiGravitySphere.transform.position = new Vector3(stageCenter.x, 0, stageCenter.z) * MapGenerator.instance.mapScale;

            terrain.customObjects.Add(antiGravitySphere);
            var moonObject = SceneManager.GetActiveScene().GetRootGameObjects().Single(x => x.name == "Moon");

            MoonEscapeSequence moonEscapeSequence = moonObject.transform.Find("MoonEscapeSequence").GetComponent<MoonEscapeSequence>();

            MoonPillars moonPillars = moonObject.transform.Find("MoonBatteryMissionController").GetComponent<MoonPillars>();
            moonPillars.pillarPositions = new List<Vector3>();
            moonPillars.rng = new Xoroshiro128Plus(rng.nextUlong);
            moonPillars.globalSphereScaleCurve = antiGravitySphere.GetComponent<ObjectScaleCurve>();
            moonPillars.globalSphereScaleCurve.baseScale = antiGravitySphere.transform.localScale;

            int minPillarsCount = Application.isEditor
                ? 4
                : Math.Max(4, ModConfig.MoonRequiredPillarsCount.Value);

            int pillarCount = rng.RangeInt(minPillarsCount, 8);
            for (int i = 1; i <= pillarCount && i < sphereZones.Count; i++)
            {
                var sphere = sphereZones[i];
                PropsNode? pillarPosition = graphs.FindNodeApproximate(rng, sphere.position * MapGenerator.instance.mapScale, sphere.radius * MapGenerator.instance.mapScale * spheres.maxObjectifDistance);
                pillarPosition = pillarPosition ?? graphs.FindNodeApproximate(rng, sphere.position * MapGenerator.instance.mapScale, sphere.radius * MapGenerator.instance.mapScale);

                moonPillars.pillarPositions.Add(pillarPosition?.position ?? (sphere.position * MapGenerator.instance.mapScale));
                if (pillarPosition != null)
                {
                    graphs.OccupySpace(pillarPosition.Value.position, solid: true);
                }
            }

            var shipSphere = sphereZones[0];
            PropsNode? shipPosition = graphs.FindNodeApproximate(rng, shipSphere.position * MapGenerator.instance.mapScale, shipSphere.radius * MapGenerator.instance.mapScale * spheres.maxObjectifDistance);
            shipPosition = shipPosition ?? graphs.FindNodeApproximate(rng, shipSphere.position * MapGenerator.instance.mapScale, shipSphere.radius * MapGenerator.instance.mapScale);

            if (NetworkServer.active)
            {
                GameObject dropship = MoonDropship.Place(shipPosition?.position ?? (shipSphere.position * MapGenerator.instance.mapScale));
                moonEscapeSequence.dropshipZone = dropship;
                terrain.customObjects.Add(dropship);
            }

            if (shipPosition != null)
            {
                graphs.OccupySpace(shipPosition.Value.position, solid: true);
            }

            var lunarSphere = sphereZones[sphereZones.Count - 3];
            GameObject lunarPodPrefab = Addressables.LoadAssetAsync<GameObject>(lunarPodKey).WaitForCompletion();

            for (int i = 0; i < 7; i++)
            {
                float lunarSpawnRate = 17f / 49f;

                if (rng.nextNormalizedFloat < lunarSpawnRate)
                {
                    PropsNode? podLocation = graphs.FindNodeApproximate(rng, lunarSphere.position * MapGenerator.instance.mapScale, lunarSphere.radius * MapGenerator.instance.mapScale * spheres.maxObjectifDistance);
                    podLocation = podLocation ?? graphs.FindNodeApproximate(rng, lunarSphere.position * MapGenerator.instance.mapScale, lunarSphere.radius * MapGenerator.instance.mapScale);
                    if (podLocation == null)
                    {
                        break;
                    }

                    graphs.OccupySpace(podLocation.Value.position, solid: false);

                    if (NetworkServer.active)
                    {
                        GameObject lunarPod = Instantiate(lunarPodPrefab);
                        lunarPod.transform.position = podLocation.Value.position;

                        NetworkServer.Spawn(lunarPod);
                        terrain.customObjects.Add(lunarPod);
                    }
                }
            }

            var cauldronSphere = sphereZones[sphereZones.Count - 2];
            float redCauldronRate = 10f / 22f;

            GameObject redCauldronPrefab = Addressables.LoadAssetAsync<GameObject>(redCauldronKey).WaitForCompletion();
            GameObject greenCauldronPrefab = Addressables.LoadAssetAsync<GameObject>(greenCauldronKey).WaitForCompletion();

            for (int i = 0; i < 5; i++)
            {
                GameObject cauldronPrefab = rng.nextNormalizedFloat < redCauldronRate
                    ? redCauldronPrefab
                    : greenCauldronPrefab;

                PropsNode? cauldronLocation = graphs.FindNodeApproximate(rng, cauldronSphere.position * MapGenerator.instance.mapScale, cauldronSphere.radius * MapGenerator.instance.mapScale * spheres.maxObjectifDistance);
                cauldronLocation = cauldronLocation ?? graphs.FindNodeApproximate(rng, cauldronSphere.position * MapGenerator.instance.mapScale, cauldronSphere.radius * MapGenerator.instance.mapScale);
                if (cauldronLocation == null)
                {
                    break;
                }

                graphs.OccupySpace(cauldronLocation.Value.position, solid: true);

                if (NetworkServer.active)
                {
                    GameObject cauldron = Instantiate(cauldronPrefab);
                    cauldron.transform.position = cauldronLocation.Value.position;

                    NetworkServer.Spawn(cauldron);
                    terrain.customObjects.Add(cauldron);
                }
            }

            GameObject whiteCauldronPrefab = Addressables.LoadAssetAsync<GameObject>(whiteCauldronKey).WaitForCompletion();
            int whiteCauldronCount = MapGenerator.rng.RangeInt(0, 3);

            for (int i = 0; i < whiteCauldronCount; i++)
            {
                PropsNode? cauldronLocation = graphs.FindNodeApproximate(rng, cauldronSphere.position * MapGenerator.instance.mapScale, cauldronSphere.radius * MapGenerator.instance.mapScale * spheres.maxObjectifDistance);
                cauldronLocation = cauldronLocation ?? graphs.FindNodeApproximate(rng, cauldronSphere.position * MapGenerator.instance.mapScale, cauldronSphere.radius * MapGenerator.instance.mapScale);
                if (cauldronLocation == null)
                {
                    break;
                }

                graphs.OccupySpace(cauldronLocation.Value.position, solid: true);

                if (NetworkServer.active)
                {
                    GameObject cauldron = Instantiate(whiteCauldronPrefab);
                    cauldron.transform.position = cauldronLocation.Value.position;

                    NetworkServer.Spawn(cauldron);
                    terrain.customObjects.Add(cauldron);
                }
            }

            Transform brotherMissionControllerTransform = moonObject.transform.Find("BrotherMissionController");

            ChildLocator childLocator = MapGenerator.instance.sceneInfoObject.GetComponent<ChildLocator>();
            Transform centerOfArena = brotherMissionControllerTransform.Find("CenterOfArena");
            childLocator.transformPairs[0].transform = centerOfArena;

            centerOfArena.Find("CenterOrbEffect").gameObject.SetActive(true);

            var spawnSphere = sphereZones[sphereZones.Count - 1];

            GameObject playerSpawnOrigin = new GameObject("PlayerSpawnOrigin");
            playerSpawnOrigin.transform.position = spawnSphere.position * MapGenerator.instance.mapScale;
            childLocator.transformPairs[1].transform = playerSpawnOrigin.transform;
            terrain.customObjects.Add(playerSpawnOrigin);

            PropsNode? frogPosition = graphs.FindNodeApproximate(rng, spawnSphere.position * MapGenerator.instance.mapScale, spawnSphere.radius * MapGenerator.instance.mapScale * spheres.maxObjectifDistance);
            frogPosition = frogPosition ?? graphs.FindNodeApproximate(rng, spawnSphere.position * MapGenerator.instance.mapScale, spawnSphere.radius * MapGenerator.instance.mapScale);
            moonEscapeSequence.frogPosition = frogPosition?.position ?? playerSpawnOrigin.transform.position;

            Transform gameplaySpaceTransform = moonObject.transform.Find("HOLDER: Gameplay Space");

            Vector3 arenaDeltaPos = MapGenerator.instance.mapScale * arenaPosition - gameplaySpaceTransform.position;
            brotherMissionControllerTransform.position += arenaDeltaPos;

            gameplaySpaceTransform.position = MapGenerator.instance.mapScale * arenaPosition;

            GameObject arena = MoonArena.AddArena(MapGenerator.instance.mapScale * arenaPosition);
            terrain.customObjects.Add(arena);

            for (int i = 0; i < sphereZones.Count; i++)
            {
                Sphere sphere = sphereZones[i];
                GameObject gravitySphere = Instantiate(gravitySpherePrefab);
                gravitySphere.transform.position = sphere.position * MapGenerator.instance.mapScale;
                gravitySphere.transform.localScale = 2 * new Vector3(sphere.radius, sphere.radius, sphere.radius) * MapGenerator.instance.mapScale;
                gravitySphere.GetComponent<MeshRenderer>().material = i == 0
                    ? shipSphereMaterial
                    : i <= pillarCount
                        ? pillarSphereMaterial
                        : i == sphereZones.Count - 1
                            ? spawnSphereMaterial
                            : i >= sphereZones.Count - 3
                                ? lootSphereMaterial
                                : gravitySphereMaterial;

                terrain.customObjects.Add(gravitySphere);
            }

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

                    if (terrain.densityMap[destination.x, destination.y, destination.z] < 0.5f)
                    {
                        break;
                    }
                }

                GameObject orbDestination = new GameObject("OrbDestination");
                orbDestination.transform.position = (Vector3)destination * MapGenerator.instance.mapScale;

                exitOrbs[i].explicitDestination = orbDestination.transform;
                terrain.customObjects.Add(orbDestination);
            }

            moonObject.GetComponent<MoonMissionController>().enabled = true;

            //moonObject.SetActive(true);
            if (NetworkServer.active)
            {
                //NetworkServer.Spawn(moonObject);
            }
        }
    }
}

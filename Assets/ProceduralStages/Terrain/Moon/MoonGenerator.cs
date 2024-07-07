using HG;
using RoR2;
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
        public Pathway pathway;
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
            public string bubbleMaterialKey;// = "RoR2/Base/Teleporters/matTeleporterRangeIndicator.mat";
            public Texture2D antigravityColorRemapRamp;
            public Texture2D gravityColorRemapRamp;
        }

        [Serializable]
        public struct Pathway
        {
            public SquareHoneycomb honeycomb;
            public Bounds bounds;
            public Torus rings;
        }

        [Serializable]
        public struct Torus
        {
            //public Interval count;
            public Interval distance;
            public Interval radius;
            public Interval thickness;
            public CubicHoneycomb honeycomb;
        }

        private struct Ring
        {
            public Vector3 position;
            public float radius;
            public float thickness;
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

                //float arenaDistance = new Vector3(position.x - arenaPosition.x, position.z - arenaPosition.z).magnitude - arenaZoneRadius - radius;
                //
                //if (arenaDistance < spheres.minDistance)
                //{
                //    continue;
                //}

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

            //List<Ring> rings = new List<Ring>();
            //
            //float ringX = 0;
            //
            //while (true)
            //{
            //    float distance = rng.RangeFloat(pathway.rings.distance.min, pathway.rings.distance.max);
            //    ringX += distance;
            //
            //    if (ringX > pathway.bounds.max.x)
            //    {
            //        break;
            //    }
            //
            //    rings.Add(new Ring
            //    {
            //        position = new Vector3(ringX, pathway.bounds.center.y, pathway.bounds.center.z),
            //        radius = rng.RangeFloat(pathway.rings.radius.min, pathway.rings.radius.max),
            //        thickness = rng.RangeFloat(pathway.rings.thickness.min, pathway.rings.thickness.max),
            //    });
            //}
            //
            //
            //bool[,,] ringsBlockMap = new bool[stageSize.x, stageSize.y, stageSize.z];
            //
            //
            //Parallel.ForEach(rings, ring =>
            //{
            //    int minX = Mathf.Clamp(Mathf.FloorToInt(ring.position.x - ring.thickness), 0, stageSize.x - 1);
            //    int maxX = Mathf.Clamp(Mathf.CeilToInt(ring.position.x + ring.thickness), 0, stageSize.x - 1);
            //
            //    Vector2 center = new Vector2(ring.position.y, ring.position.z);
            //
            //    int minY = Mathf.Clamp(Mathf.FloorToInt(ring.position.y - ring.radius - ring.thickness), 0, stageSize.y - 1);
            //    int maxY = Mathf.Clamp(Mathf.CeilToInt(ring.position.y + ring.radius + ring.thickness), 0, stageSize.y - 1);
            //
            //    int minZ = Mathf.Clamp(Mathf.FloorToInt(ring.position.z - ring.radius - ring.thickness), 0, stageSize.z - 1);
            //    int maxZ = Mathf.Clamp(Mathf.CeilToInt(ring.position.z + ring.radius + ring.thickness), 0, stageSize.z - 1);
            //
            //    for (int y = minY; y <= maxY; y++)
            //    {
            //        for (int z = minZ; z <= maxZ; z++)
            //        {
            //            float distance = (new Vector2(y, z) - center).magnitude;
            //            bool inRing = Mathf.Abs(distance - ring.radius) < ring.thickness;
            //
            //            if (!inRing)
            //            {
            //                continue;
            //            }
            //
            //            for (int x = minX; x <= maxX; x++)
            //            {
            //                ringsBlockMap[x, y, z] = true;
            //            }
            //        }
            //    }
            //});
            //
            //
            //int floorSeedX = rng.RangeInt(0, short.MaxValue);
            //int floorSeedZ = rng.RangeInt(0, short.MaxValue);
            //
            //int ringSeedX = rng.RangeInt(0, short.MaxValue);
            //int ringSeedY = rng.RangeInt(0, short.MaxValue);
            //int ringSeedZ = rng.RangeInt(0, short.MaxValue);

            //float[,,] densityMap = new float[stageSize.x, stageSize.y, stageSize.z];
            //
            //Parallel.For(0, stageSize.x, x =>
            //{
            //    for (int z = 0; z < stageSize.z; z++)
            //    {
            //        Vector2 pos2D = new Vector2(x, z);
            //
            //        Voronoi2DResult floorHoneycombResult = pathway.honeycomb[x + floorSeedX, z + floorSeedZ];
            //
            //        Vector2 floorPos1 = pos2D + floorHoneycombResult.displacement1;
            //        Vector2 floorPos2 = pos2D + floorHoneycombResult.displacement2;
            //
            //        bool isFloor1 = pathway.bounds.min.x <= floorPos1.x
            //            && pathway.bounds.min.z <= floorPos1.y
            //            && floorPos1.x < pathway.bounds.max.x
            //            && floorPos1.y < pathway.bounds.max.z;
            //
            //        bool isFloor2 = pathway.bounds.min.x <= floorPos2.x
            //            && pathway.bounds.min.z <= floorPos2.y
            //            && floorPos2.x < pathway.bounds.max.x
            //            && floorPos2.y < pathway.bounds.max.z;
            //
            //        for (int y = 0; y < stageSize.y; y++)
            //        {
            //            Vector3 pos = new Vector3(x, y, z);
            //
            //            var ringHoneyCombResult = pathway.rings.honeycomb[x + ringSeedX, y + ringSeedY, z + ringSeedZ];
            //            bool inRing1 = ringsBlockMap[
            //                Mathf.Clamp(Mathf.RoundToInt(x + ringHoneyCombResult.displacement1.x), 0, stageSize.x - 1),
            //                Mathf.Clamp(Mathf.RoundToInt(y + ringHoneyCombResult.displacement1.y), 0, stageSize.y - 1),
            //                Mathf.Clamp(Mathf.RoundToInt(z + ringHoneyCombResult.displacement1.z), 0, stageSize.z - 1)];
            //
            //            bool inRing2 = ringsBlockMap[
            //                Mathf.Clamp(Mathf.RoundToInt(x + ringHoneyCombResult.displacement2.x), 0, stageSize.x - 1),
            //                Mathf.Clamp(Mathf.RoundToInt(y + ringHoneyCombResult.displacement2.y), 0, stageSize.y - 1),
            //                Mathf.Clamp(Mathf.RoundToInt(z + ringHoneyCombResult.displacement2.z), 0, stageSize.z - 1)];
            //
            //            bool isFloorHeight = pathway.bounds.min.y <= y
            //                && y <= pathway.bounds.max.y;
            //
            //            float density = 0;
            //
            //            if (isFloorHeight)
            //            {
            //                if (isFloor1 && isFloor2)
            //                {
            //                    density = 1;
            //                }
            //                else if (isFloor1)
            //                {
            //                    density = 1 - floorHoneycombResult.weight;
            //                }
            //                else if (isFloor2)
            //                {
            //                    density = floorHoneycombResult.weight;
            //                }
            //            }
            //
            //            if (ringsBlockMap[x, y, z])
            //            {
            //                density = 1;
            //            }
            //
            //            //if (inRing1 || inRing2)
            //            //{
            //            //    density = 1;
            //            //}
            //            //else if (inRing1)
            //            //{
            //            //    density = Mathf.Max(density, 1 - ringHoneyCombResult.weight);
            //            //}
            //            //else if (inRing2)
            //            //{
            //            //    density = Mathf.Max(density, ringHoneyCombResult.weight);
            //            //}
            //
            //            densityMap[x, y, z] = density;
            //
            //            //Vector3 delta = pos - center;
            //            //float distance = delta.magnitude / radius;
            //            //float distanceNoise = sphereDistanceCurve.Evaluate(distance);
            //            //
            //            //float voronoiNoise = voronoiRemapCurve.Evaluate(voronoi3D[x, y, z].weight);
            //            //
            //            //densityMap[x, y, z] = Mathf.Min(distanceNoise, voronoiNoise);
            //        }
            //    }
            //});

            //float radius = Mathf.Min(center.x, center.y, center.z);
            //
            //Parallel.For(0, stageSize.x, x =>
            //{
            //    for (int y = 0; y < stageSize.y; y++)
            //    {
            //        for (int z = 0; z < stageSize.z; z++)
            //        {
            //            Vector3 pos = new Vector3(x, y, z);
            //            Vector3 delta = pos - center;
            //            float distance = delta.magnitude / radius;
            //            float distanceNoise = sphereDistanceCurve.Evaluate(distance);
            //
            //            float voronoiNoise = voronoiRemapCurve.Evaluate(voronoi3D[x, y, z].weight);
            //
            //            densityMap[x, y, z] = Mathf.Min(distanceNoise, voronoiNoise);
            //        }
            //    }
            //});
            //
            //ProfilerLog.Debug("sphere");


            var meshResult = MarchingCubes.CreateMesh(densityMap, MapGenerator.instance.mapScale);
            ProfilerLog.Debug("marchingCubes");

            List<GameObject> customObjects = new List<GameObject>();
            customObjects.AddRange(sphereZones.Select(x => x.gravitySphere));
            customObjects.Add(antiGravitySphere);
            customObjects.Add(dropship);
            customObjects.Add(arena);
            customObjects.Add(arenaGravityZone);
            //customObjects.Add(arenaZone);

            moonObject.SetActive(true);

            return new Terrain
            {
                meshResult = meshResult,
                floorlessDensityMap = new float[stageSize.x, stageSize.y, stageSize.z],
                densityMap = densityMap,
                maxGroundHeight = float.MaxValue,
                oobScale = new Vector3(4, 6, 4),
                customObjects = customObjects.ToArray()
            };
        }
    }
}

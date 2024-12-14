using RoR2.UI;
using RiskOfOptions.Resources;
using RoR2;
using RoR2.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace ProceduralStages
{
    [CreateAssetMenu(fileName = "PotRollingGenerator", menuName = "ProceduralStages/PotRollingGenerator", order = 3)]
    public class PotRollingGenerator : TerrainGenerator
    {
        public ThreadSafeCurve minHeightCurve;
        public ThreadSafeCurve maxHeightCurve;
        public FBM floorFBM;
        public ThreadSafeCurve floorNoiseRemap;

        public FBM pathCurveFBM;
        public ThreadSafeCurve pathCurveRemap;

        public float blendFactor = 0.1f;
        public float plateDistanceFromEdge;
        public Vector3 platePositionMaxOffset;
        public float platePositionYOffset;

        public GameObject plateIndicatorPrefab;

        public override Terrain Generate()
        {
            var stageSize = MapGenerator.instance.stageSize;
            var rng = MapGenerator.rng;

            float[,,] densityMap = new float[stageSize.x, stageSize.y, stageSize.z];

            int curvatureSeed = rng.RangeInt(0, short.MaxValue);
            int seedX = rng.RangeInt(0, short.MaxValue);
            int seedZ = rng.RangeInt(0, short.MaxValue);

            Parallel.For(0, stageSize.x, x =>
            {
                for (int z = 0; z < stageSize.z; z++)
                {
                    float pathCurvature = pathCurveRemap.Evaluate(pathCurveFBM.Evaluate(z, curvatureSeed));

                    float minHeight = minHeightCurve.Evaluate((x + stageSize.x * pathCurvature) / (stageSize.x - 1f));
                    float maxHeight = maxHeightCurve.Evaluate((x + stageSize.x * pathCurvature) / (stageSize.x - 1f));

                    if (z < stageSize.x / 2)
                    {
                        float t = Mathf.InverseLerp(0, stageSize.x, z);
                        minHeight = Mathf.Min(minHeight, minHeightCurve.Evaluate(t));
                        maxHeight = Mathf.Min(maxHeight, maxHeightCurve.Evaluate(t));
                    }

                    if (stageSize.z - 1 - z < stageSize.x / 2)
                    {
                        float t = Mathf.InverseLerp(stageSize.x, 0, stageSize.z - 1 - z);
                        minHeight = Mathf.Min(minHeight, minHeightCurve.Evaluate(t));
                        maxHeight = Mathf.Min(maxHeight, maxHeightCurve.Evaluate(t));
                    }

                    float height = Mathf.Lerp(minHeight, maxHeight, floorNoiseRemap.Evaluate(floorFBM.Evaluate(x + seedX, z + seedZ)));

                    for (int y = 0; y < stageSize.y - 1; y++)
                    {
                        float noise = Mathf.Clamp01((height * stageSize.y - y) * blendFactor + 0.5f);
                        if (noise == 0f)
                        {
                            break;
                        }

                        densityMap[x, y, z] = noise;
                    }
                }
            });


            var moonObject = SceneManager.GetActiveScene().GetRootGameObjects().Single(x => x.name == "Moon").gameObject;
            GameObject playerSpawnOrigin = new GameObject("PlayerSpawnOrigin");
            playerSpawnOrigin.transform.position = new Vector3(stageSize.x / 2, stageSize.y / 2, plateDistanceFromEdge) * MapGenerator.instance.mapScale;
            ChildLocator childLocator = MapGenerator.instance.sceneInfoObject.GetComponent<ChildLocator>();
            childLocator.transformPairs[1].transform = playerSpawnOrigin.transform;
            moonObject.GetComponent<MoonMissionController>().enabled = true;



            var meshResult = MarchingCubes.CreateMesh(densityMap, MapGenerator.instance.mapScale);
            ProfilerLog.Debug("marchingCubes");

            return new Terrain
            {
                generator = this,
                meshResult = meshResult,
                floorlessDensityMap = new float[stageSize.x, stageSize.y, stageSize.z],
                densityMap = densityMap,
                maxGroundHeight = float.MaxValue,
                customObjects = new List<GameObject>()
                {
                    playerSpawnOrigin
                },
                oobScale = new Vector3(1, 8, 1)
            };
        }

        public override void AddProps(Terrain terrain, Graphs graphs)
        {
            var stageSize = MapGenerator.instance.stageSize;
            var rng = MapGenerator.rng;

            Vector3 basePlatePos = MapGenerator.instance.mapScale * new Vector3(stageSize.x / 2, stageSize.y, stageSize.z - plateDistanceFromEdge);
            
            float maxDot = 0;
            PropsNode bestNodeInfo = default;

            for (int i = 0; i < 1000; i++)
            {
                Vector3 estimatePos = basePlatePos + new Vector3(
                    rng.RangeFloat(-1, 1) * platePositionMaxOffset.x,
                    rng.RangeFloat(-1, 1) * platePositionMaxOffset.y,
                    rng.RangeFloat(-1, 1) * platePositionMaxOffset.z);

                NodeGraph.NodeIndex nodeIndex = graphs.ground.FindClosestNodeWithFlagConditions(
                    estimatePos,
                    HullClassification.Human,
                    NodeFlags.None,
                    NodeFlags.NoCharacterSpawn,
                    preventOverhead: false);

                Vector3 platePos = graphs.ground.nodes[nodeIndex.nodeIndex].position;
                PropsNode nodeInfo = graphs.nodeInfoByPosition[platePos];

                float dot = Vector3.Dot(nodeInfo.normal, Vector3.up);
                if (dot > maxDot)
                {
                    dot = maxDot;
                    bestNodeInfo = nodeInfo;
                }
            }

            graphs.OccupySpace(bestNodeInfo.position, solid: true);

            var platetPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/goolake/GLPressurePlate.prefab").WaitForCompletion();
            var plate = Instantiate(platetPrefab, bestNodeInfo.position + new Vector3(0, platePositionYOffset, 0), Quaternion.FromToRotation(Vector3.up, bestNodeInfo.normal));

            PressurePlateController pressurePlateController = plate.GetComponent<PressurePlateController>();
            PlateStageChanger plateStageChanger = plate.AddComponent<PlateStageChanger>();
            plateStageChanger.onDelayFinished = () =>
            {
                RoR2.Console.instance.SubmitCmd(null, "next_stage");
            };

            pressurePlateController.OnSwitchDown.AddListener(() =>
            {
                plateStageChanger.enabled = true;
            });

            pressurePlateController.OnSwitchUp.AddListener(() =>
            {
                plateStageChanger.enabled = false;
            });

            GameObject plateIndicator = Instantiate(plateIndicatorPrefab);
            terrain.customObjects.Add(plateIndicator);

            plateIndicator.GetComponentInChildren<SpriteRenderer>(includeInactive: true).sprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/MiscIcons/texLootIconOutlined.png").WaitForCompletion();
            plateIndicator.GetComponent<PositionIndicator>().targetTransform = plate.transform;

            GameObject batteryPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/moon2/MoonBatterySoul.prefab").WaitForCompletion();

            GameObject beamPreafb = batteryPrefab.transform
                .Find("Model")
                .Find("mdlMoonBattery")
                .Find("InactiveFX")
                .Find("Beam, Strong")
                .gameObject;

            GameObject beam = Instantiate(beamPreafb, bestNodeInfo.position, Quaternion.FromToRotation(Vector3.up, Vector3.up));

            terrain.customObjects.Add(plate);
            terrain.customObjects.Add(beam);
        }
    }
}

using RoR2;
using RoR2.Navigation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.Rendering.PostProcessing;
using UnityMeshSimplifier;

namespace ProceduralStages
{
    //[DefaultExecutionOrder(-1)]
    public class MapGenerator : MonoBehaviour
    {
        public int width;
        public int height;
        public int depth;

        public int seed;

        [Range(0, 10)]
        public float mapScale = 1f;

        [Range(0, 1)]
        public float meshQuality = 0.1f;

        public CellularAutomata2d cave2d = new CellularAutomata2d();

        public Map2dToMap3d map2dToMap3d = new Map2dToMap3d();
        public Carver carver = new Carver();
        public Waller waller = new Waller();

        public CellularAutomata3d cave3d = new CellularAutomata3d();

        public Map3dNoiser map3dNoiser = new Map3dNoiser();

        public MeshColorer meshColorer = new MeshColorer();

        public ColorPatelette colorPatelette = new ColorPatelette();

        public NodeGraphCreator nodeGraphCreator = new NodeGraphCreator();

        public GameObject sceneInfoObject;
        public GameObject postProcessingObject;
        public GameObject directorObject;
        //private float[,] _map;
        //private bool[,,] _map;

        private void Awake()
        {
            if (Application.IsPlaying(this))
            {
                GenerateMap();
            }
        }

        private void Start()
        {
            //UnityEngine.Debug.Log("Start");
            //GenerateMap();
        }

        private void Update()
        {
            //if (Input.GetKeyDown(KeyCode.F2))
            //{
            //    GenerateMap();
            //}
        }

        private void OnValidate()
        {
            if (Application.isEditor && Application.IsPlaying(this))
            {
                GenerateMap();
                sceneInfoObject.GetComponent<SceneInfo>().OnValidate();
            }
        }

        private void GenerateMap()
        {
            if (!Application.isEditor && !NetworkServer.active)
            {
                return;
            }

            int stageInLoop = ((Run.instance?.stageClearCount ?? 0) % 5) + 1;

            Log.Debug("GenerateMap");
            int currentSeed = seed == 0
                ? Time.time.GetHashCode()
                : seed;

            System.Random rng = new System.Random(currentSeed);

            Stopwatch totalStopwatch = Stopwatch.StartNew();

            Stopwatch stopwatch = Stopwatch.StartNew();

            var map2D = cave2d.Create(width, depth, rng);
            LogStats("cave2d");

            bool[,,] map3d = map2dToMap3d.Convert(map2D, height);
            LogStats("map2dToMap3d");

            carver.CarveWalls(map3d, rng);
            LogStats("carver");

            waller.AddFloorAndCeilling(map3d, rng);
            LogStats("waller.AddFloorAndCeilling");

            waller.AddWalls(map3d, rng);
            LogStats("waller.AddWalls");

            bool[,,] smoothMap3d = cave3d.SmoothMap(map3d);
            LogStats("cave3d");

            float[,,] noiseMap3d = map3dNoiser.ToNoiseMap(smoothMap3d, rng);
            LogStats("map3dNoiser");

            var unOptimisedMesh = MarchingCubes.CreateMesh(noiseMap3d, mapScale, meshColorer, rng);
            LogStats("marchingCubes");

            MeshSimplifier simplifier = new MeshSimplifier(unOptimisedMesh);
            simplifier.SimplifyMesh(meshQuality);
            var optimisedMesh = simplifier.ToMesh();
            LogStats("MeshSimplifier");

            var meshResult = new MeshResult
            {
                mesh = optimisedMesh,
                normals = optimisedMesh.normals,
                triangles = optimisedMesh.triangles,
                vertices = optimisedMesh.vertices
            };

            meshColorer.ColorMesh(meshResult, rng);
            LogStats("meshColorer");

            GetComponent<MeshFilter>().mesh = meshResult.mesh;
            LogStats("MeshFilter");

            Texture2D heightMap = colorPatelette.CreateHeightMap(rng);
            Texture2D texture = colorPatelette.CreateTexture(rng, heightMap);
            LogStats("colorPatelette");

            var material = GetComponent<MeshRenderer>().material;
            material.mainTexture = texture;
            material.SetTexture("_ParallaxMap", heightMap);
            //material.SetTexture("_DetailAlbedoMap", texture);
            material.color = new Color(1.3f, 1.3f, 1.3f);
            LogStats("MeshRenderer");

            float sunHue = (float)rng.NextDouble();
            RenderSettings.sun.color = Color.HSVToRGB(sunHue, colorPatelette.light.saturation, colorPatelette.light.value);
            LogStats("RenderSettings");

            RampFog fog = postProcessingObject.GetComponent<PostProcessVolume>().profile.GetSetting<RampFog>();
            var fogColor = Color.HSVToRGB(sunHue, colorPatelette.fog.saturation, colorPatelette.fog.value);

            fog.fogColorStart.value = fogColor;
            fog.fogColorStart.value.a = colorPatelette.fog.colorStartAlpha;
            fog.fogColorMid.value = fogColor;
            fog.fogColorMid.value.a = colorPatelette.fog.colorMidAlpha;
            fog.fogColorEnd.value = fogColor;
            fog.fogColorEnd.value.a = colorPatelette.fog.colorEndAlpha;
            fog.fogZero.value = colorPatelette.fog.zero;
            fog.fogOne.value = colorPatelette.fog.one;

            fog.fogIntensity.value = colorPatelette.fog.intensity;
            fog.fogPower.value = colorPatelette.fog.power;
            LogStats("Fog/Light");

            var surface = ScriptableObject.CreateInstance<SurfaceDef>();
            surface.approximateColor = colorPatelette.AverageColor(texture);
            surface.materialSwitchString = "stone";

            GetComponent<SurfaceDefProvider>().surfaceDef = surface;
            LogStats("surfaceDef");

            GetComponent<MeshCollider>().sharedMesh = meshResult.mesh;
            LogStats("MeshCollider");

            (NodeGraph groundNodes, NodeGraph airNodes) = nodeGraphCreator.CreateGraphs(meshResult, map3d, mapScale);
            LogStats("nodeGraphs");

            SceneInfo sceneInfo = sceneInfoObject.GetComponent<SceneInfo>();
            sceneInfo.groundNodes = groundNodes;
            sceneInfo.airNodes = airNodes;

            ClassicStageInfo stageInfo = sceneInfoObject.GetComponent<ClassicStageInfo>();

            List<string> dpMonsters = new List<string>()
            {
                "RoR2/DLC1/ancientloft/dpAncientLoftMonsters.asset",
                "RoR2/Base/blackbeach/dpBlackBeachMonsters.asset",
                "RoR2/Base/dampcave/dpDampCaveMonsters.asset",
                "RoR2/Base/foggyswamp/dpFoggySwampMonsters.asset",
                "RoR2/Base/frozenwall/dpFrozenWallMonsters.asset",
                "RoR2/Base/golemplains/dpGolemplainsMonsters.asset",
                "RoR2/Base/goolake/dpGooLakeMonsters.asset",
                "RoR2/Base/rootjungle/dpRootJungleMonsters.asset",
                "RoR2/Base/shipgraveyard/dpShipgraveyardMonsters.asset",
                "RoR2/Base/skymeadow/dpSkyMeadowMonsters.asset",
                "RoR2/DLC1/snowyforest/dpSnowyForestMonsters.asset",
                "RoR2/DLC1/sulfurpools/dpSulfurPoolsMonsters.asset",
                "RoR2/Base/wispgraveyard/dpWispGraveyardMonsters.asset"
            };

            List<string> dpInteratables = new List<string>()
            {
                "RoR2/DLC1/ancientloft/dpAncientLoftInteractables.asset",
                "RoR2/Base/blackbeach/dpBlackBeachInteractables.asset",
                "RoR2/Base/dampcave/dpDampCaveInteractables.asset",
                "RoR2/Base/foggyswamp/dpFoggySwampInteractables.asset",
                "RoR2/Base/frozenwall/dpFrozenWallInteractables.asset",
                "RoR2/Base/golemplains/dpGolemplainsInteractables.asset",
                "RoR2/Base/goolake/dpGooLakeInteractables.asset",
                "RoR2/Base/rootjungle/dpRootJungleInteractables.asset",
                "RoR2/Base/shipgraveyard/dpShipgraveyardInteractables.asset",
                "RoR2/Base/skymeadow/dpSkyMeadowInteractables.asset",
                "RoR2/DLC1/snowyforest/dpSnowyForestInteractables.asset",
                "RoR2/DLC1/sulfurpools/dpSulfurPoolsInteractables.asset",
                "RoR2/Base/wispgraveyard/dpWispGraveyardInteractables.asset"
            };

            string dpMonster = dpMonsters[rng.Next(0, dpMonsters.Count)];
            string dpInteratable = dpInteratables[rng.Next(0, dpInteratables.Count)];

            Log.Debug(dpMonster);
            Log.Debug(dpInteratable);

            stageInfo.monsterDccsPool = Addressables.LoadAssetAsync<DccsPool>(dpMonster).WaitForCompletion();
            stageInfo.interactableDccsPool = Addressables.LoadAssetAsync<DccsPool>(dpInteratable).WaitForCompletion();
            stageInfo.sceneDirectorInteractibleCredits = 75 * (stageInLoop + 2);
            stageInfo.sceneDirectorMonsterCredits = 30 * (stageInLoop + 4);

            SceneDirector sceneDirector = directorObject.GetComponent<SceneDirector>();

            bool useLunarPortal = stageInLoop == Run.stagesPerLoop;
            string portalPath = useLunarPortal
                ? "RoR2/Base/Teleporters/iscLunarTeleporter.asset"
                : "RoR2/Base/Teleporters/iscTeleporter.asset";

            sceneDirector.teleporterSpawnCard = Addressables.LoadAssetAsync<InteractableSpawnCard>(portalPath).WaitForCompletion();

            Action<SceneDirector> placeInteractables = null;
            placeInteractables = _ =>
            {
                SceneDirector.onPostPopulateSceneServer -= placeInteractables;
                Xoroshiro128Plus r = new Xoroshiro128Plus((ulong)rng.Next());

                InteractablePlacer.Place(r, "RoR2/Base/NewtStatue/NewtStatue.prefab", NodeFlagsExt.Newt, Vector3.up);

                if (stageInLoop == 4)
                {
                    InteractablePlacer.Place(r, "RoR2/Base/GoldChest/GoldChest.prefab", NodeFlagsExt.Newt);
                }
            };

            SceneDirector.onPostPopulateSceneServer += placeInteractables;

            Log.Debug($"total: " + totalStopwatch.Elapsed.ToString());

            void LogStats(string name)
            {
                Log.Debug($"{name}: {stopwatch.Elapsed}");
                stopwatch.Restart();
            }
        }

        private void SceneDirector_onPostPopulateSceneServer(SceneDirector obj)
        {
            throw new NotImplementedException();
        }
    }
}
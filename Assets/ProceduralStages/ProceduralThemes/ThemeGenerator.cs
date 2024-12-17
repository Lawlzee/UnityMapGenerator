
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProceduralStages
{
    public class ThemeGenerator : MonoBehaviour
    {
        public ulong editorSeed;
        public Theme editorTheme;
        public MapThemeCollection themes;
        public VanillaStageDef[] stages;

        public PropsPlacer propsPlacer;
        public MeshColorer meshColorer;

        public Material terrainMaterial;
        public SurfaceDefProvider surfaceDefProvider;
        public OcclusionCulling occlusionCulling;

        public bool showDebugMeshes;

        public MeshFilter debugFloorMeshFilter;
        public MeshFilter debugCeilMeshFilter;
        public GameObject debugMapBounds;

        private ulong lastSeed;
        public static ThemeGenerator instance;
        public static Xoroshiro128Plus rng;

        private void Awake()
        {
            instance = this;

            if (Application.IsPlaying(this) && RunConfig.instance != null)
            {
                if (Application.isEditor)
                {
                    themes.WarmUp();
                }

                lastSeed = SetSeed();
                ApplyTheme();
            }
        }

        private void OnDestroy()
        {
            instance = null;
            rng = null;
        }

        private bool generateNextFrame;

        private void Update()
        {
            if (!Application.isEditor)
            {
                return;
            }

            if (generateNextFrame)
            {
                generateNextFrame = false;
                ApplyTheme();
            }

            if (Input.GetKeyDown(KeyCode.F2) || Input.GetKeyDown(KeyCode.F3))
            {
                for (int i = 0; i < propsPlacer.instances.Count; i++)
                {
                    Destroy(propsPlacer.instances[i]);
                }
                propsPlacer.instances.Clear();

                if (Input.GetKeyDown(KeyCode.F2))
                {
                    lastSeed = SetSeed();
                }
                else
                {
                    rng = new Xoroshiro128Plus(lastSeed);
                }

                generateNextFrame = true;
            }

            if (Input.GetKeyDown(KeyCode.F5))
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }

        private void OnValidate()
        {
            debugFloorMeshFilter.gameObject.SetActive(Application.isEditor && showDebugMeshes);
            debugCeilMeshFilter.gameObject.SetActive(Application.isEditor && showDebugMeshes);
            debugMapBounds.SetActive(Application.isEditor && showDebugMeshes);
        }

        private void ApplyTheme()
        {
            ProfilerLog.Reset();
            using (ProfilerLog.CreateScope("total"))
            {
                string currentSceneName = SceneManager.GetActiveScene().name;
                var stageDef = stages.FirstOrDefault(x => x.sceneName == currentSceneName);

                if (stageDef == null)
                {
                    ProfilerLog.Debug($"No stageDef found for '{currentSceneName}'");
                    return;
                }

                stageDef.DisableProps();
                ProfilerLog.Debug("Built in props disabled");

                (Mesh floorMesh, Mesh ceilMesh) = stageDef.CreateMeshes();
                ProfilerLog.Debug("CreateMeshes");

                if (floorMesh == null)
                {
                    ProfilerLog.Error("floorMesh is null");
                    return;
                }

                debugFloorMeshFilter.sharedMesh = floorMesh;
                debugCeilMeshFilter.sharedMesh = ceilMesh;
                debugMapBounds.transform.localPosition = stageDef.mapBounds.center;
                debugMapBounds.transform.localScale = stageDef.mapBounds.size;

                Graphs graphs = new Graphs
                {
                    floorProps = MeshToPropsNode(floorMesh),
                    ceilingProps = MeshToPropsNode(ceilMesh),
                    groundNodeIndexByPosition = new Dictionary<Vector3, int>()
                };
                ProfilerLog.Debug("Mesh to graph");

                MapTheme theme = GetTheme();

                PropsDefinitionCollection propsCollection = theme.propCollections[rng.RangeInt(0, theme.propCollections.Length)];
                MaterialInfo materialInfo = theme.GenerateMaterialInfo(rng);

                stageDef.ApplyTerrainMaterial(
                    terrainMaterial, 
                    materialInfo,
                    meshColorer, 
                    rng);
                ProfilerLog.Debug("Terrain material applied");

                propsPlacer.PlaceAll(
                    rng,
                    Vector3.zero,
                    graphs,
                    propsCollection,
                    meshColorer,
                    materialInfo.grassColorGradiant,
                    materialInfo.ApplyTo(new Material(terrainMaterial)),
                    ceillingWeight: 1,
                    propCountWeight: 1,
                    bigObjectOnly: false);

                ProfilerLog.Debug("propsPlacer");

                using (ProfilerLog.CreateScope("OcclusionCulling.SetTargets"))
                {
                    occlusionCulling.SetTargets(propsPlacer.instances, stageDef.mapBounds);
                }
            }

            PropsNode[] MeshToPropsNode(Mesh mesh)
            {
                var vertices = mesh.vertices;
                var normals = mesh.normals;

                PropsNode[] propsNodes = new PropsNode[vertices.Length];

                for (int i = 0; i < vertices.Length; i++)
                {
                    propsNodes[i].position = vertices[i];
                    propsNodes[i].normal = normals[i];
                }

                return propsNodes;
            }
        }

        private MapTheme GetTheme()
        {
            Theme themeType = Theme.LegacyRandom;
            if (Application.isEditor)
            {
                if (editorTheme == Theme.Random)
                {
                    themeType = themes.themes[rng.RangeInt(1, themes.themes.Length)].Theme;
                }
                else
                {
                    themeType = editorTheme;
                }
            }
            else if (RunConfig.instance.selectedTheme != Theme.Random)
            {
                themeType = RunConfig.instance.selectedTheme;
                RunConfig.instance.selectedTheme = Theme.Random;
            }
            else
            {
                WeightedSelection<Theme> selection = new WeightedSelection<Theme>(RunConfig.instance.themePercents.Length);

                for (int i = 0; i < RunConfig.instance.themePercents.Length; i++)
                {
                    var config = RunConfig.instance.themePercents[i];
                    selection.AddChoice(config.Theme, config.Percent);
                }

                themeType = selection.totalWeight > 0
                    ? selection.Evaluate(rng.nextNormalizedFloat)
                    : Theme.Plains;
            }

            Log.Debug(themeType);
            MapTheme theme = themes.themes.First(x => x.Theme == themeType);

            if (Application.isEditor)
            {
                foreach (var t in themes.themes)
                {
                    t.CheckAssets();
                }
            }

            return theme;
        }

        private ulong SetSeed()
        {
            ulong currentSeed;
            if (Application.isEditor)
            {
                if (editorSeed != 0)
                {
                    currentSeed = editorSeed;
                }
                else if (RunConfig.instance.stageRng != null)
                {
                    currentSeed = RunConfig.instance.stageRng.nextUlong;
                }
                else
                {
                    currentSeed = (ulong)DateTime.Now.Ticks;
                }
            }
            else
            {
                if (RunConfig.instance.stageSeed != "")
                {
                    if (ulong.TryParse(RunConfig.instance.stageSeed, out ulong seed))
                    {
                        currentSeed = seed;
                    }
                    else
                    {
                        currentSeed = (ulong)RunConfig.instance.stageSeed.GetHashCode();
                    }
                }
                else
                {
                    currentSeed = RunConfig.instance.stageRng.nextUlong;
                }
            }

            Log.Debug("Stage Seed: " + currentSeed);

            rng = new Xoroshiro128Plus(currentSeed);
            return currentSeed;
        }
    }
}
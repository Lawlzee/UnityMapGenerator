
using RoR2;
using System;
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

        public bool showDebugMeshes;

        public MeshFilter debugFloorMeshFilter;
        public MeshFilter debugCeilMeshFilter;

        private ulong lastSeed;
        public static ThemeGenerator instance;
        public static Xoroshiro128Plus rng;

        private void Awake()
        {
            instance = this;
            if (Application.IsPlaying(this))
            {
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

                debugFloorMeshFilter.sharedMesh = stageDef.floorMesh;
                debugCeilMeshFilter.sharedMesh = stageDef.ceilMesh;

                MapTheme theme = GetTheme();
                MaterialInfo materialInfo = theme.GenerateMaterialInfo(rng);

                stageDef.ApplyTerrainMaterial(terrainMaterial, materialInfo);
                ProfilerLog.Debug("Terrain material applied");
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
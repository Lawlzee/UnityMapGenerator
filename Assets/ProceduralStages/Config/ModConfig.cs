using BepInEx.Configuration;
using RiskOfOptions.Options;
using RiskOfOptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RoR2;
using RiskOfOptions.OptionConfigs;

namespace ProceduralStages
{
    public struct TerrainTypePercentConfig
    {
        public int StageIndex;
        public TerrainType TerrainType;
        public ConfigEntry<float> Config;
    }

    public struct ThemeConfig
    {
        public Theme Theme;
        public ConfigEntry<float> Config;
    }

    public static class ModConfig
    {
        private static Dictionary<(TerrainType, int), float> _defaultTerrainTypesPercents = new Dictionary<(TerrainType, int), float>
        {
            [(TerrainType.OpenCaves, 0)] = 0.05f,
            [(TerrainType.TunnelCaves, 0)] = 0.05f,
            [(TerrainType.Islands, 0)] = 0.25f,
            [(TerrainType.Mines, 0)] = 0.10f,
            [(TerrainType.Basalt, 0)] = 0.25f,
            [(TerrainType.Towers, 0)] = 0.25f,
            [(TerrainType.Temple, 0)] = 0.05f,

            [(TerrainType.OpenCaves, 1)] = 0.25f,
            [(TerrainType.TunnelCaves, 1)] = 0.25f,
            [(TerrainType.Islands, 1)] = 0.05f,
            [(TerrainType.Mines, 1)] = 0.10f,
            [(TerrainType.Basalt, 1)] = 0.05f,
            [(TerrainType.Towers, 1)] = 0.05f,
            [(TerrainType.Temple, 1)] = 0.25f,

            [(TerrainType.OpenCaves, 2)] = 0.10f,
            [(TerrainType.TunnelCaves, 2)] = 0.10f,
            [(TerrainType.Islands, 2)] = 0.15f,
            [(TerrainType.Mines, 2)] = 0.25f,
            [(TerrainType.Basalt, 2)] = 0.10f,
            [(TerrainType.Towers, 2)] = 0.15f,
            [(TerrainType.Temple, 2)] = 0.15f,

            [(TerrainType.OpenCaves, 3)] = 0.15f,
            [(TerrainType.TunnelCaves, 3)] = 0.05f,
            [(TerrainType.Islands, 3)] = 0.20f,
            [(TerrainType.Mines, 3)] = 0.10f,
            [(TerrainType.Basalt, 3)] = 0.10f,
            [(TerrainType.Towers, 3)] = 0.20f,
            [(TerrainType.Temple, 3)] = 0.20f,

            [(TerrainType.OpenCaves, 4)] = 0.15f,
            [(TerrainType.TunnelCaves, 4)] = 0.25f,
            [(TerrainType.Islands, 4)] = 0.05f,
            [(TerrainType.Mines, 4)] = 0.20f,
            [(TerrainType.Basalt, 4)] = 0.25f,
            [(TerrainType.Towers, 4)] = 0.05f,
            [(TerrainType.Temple, 4)] = 0.05f,
        };

        private static Dictionary<Theme, float> _defaulThemePercents = new Dictionary<Theme, float>
        {
            [Theme.Desert] = 0.018f,
            [Theme.Snow] = 0.018f,
            [Theme.Void] = 0.018f,
            [Theme.Plains] = 0.018f,
            [Theme.Mushroom] = 0.018f,
            [Theme.LegacyRandom] = 0.01f,
        };

        public static ConfigEntry<string> ConfigVersion;

        public static ConfigEntry<string> StageSeed;
        public static ConfigEntry<bool> InfiniteMapScaling;
        public static ConfigEntry<int> OcclusionCullingDelay;

        public static List<ThemeConfig> ThemeConfigs;
        public static List<TerrainTypePercentConfig> TerrainTypesPercents;

        public static void Init(ConfigFile config)
        {
            ConfigVersion = config.Bind("Configuration", "Last version played", Main.PluginVersion, "Do not touch");
            var lastVersion = SemanticVersion.Parse(ConfigVersion.Value);
            //Upgrade config here
            ConfigVersion.Value = Main.PluginVersion;

            InfiniteMapScaling = config.Bind("Configuration", "Infinite map scaling", false, "If enabled, the stage size scaling will not be reset every loop. Exercise caution when utilizing this feature, as it may lead to increased map generation time and a decrease in framerate.");
            ModSettingsManager.AddOption(new CheckBoxOption(InfiniteMapScaling));

            OcclusionCullingDelay = config.Bind("Performance", "Occlusion culling frame delay", 6, "The number of frames between each occlusion culling check impacts performance. A shorter delay decreases FPS, while a longer delay causes decorations to flicker more when moving quickly. The game operates at 60 frames per second. Any changes to this configuration will take effect at the start of the next stage.");
            ModSettingsManager.AddOption(new IntSliderOption(OcclusionCullingDelay, new IntSliderConfig { min = 0, max = 60 }));

            float totalThemes = 0;
            ThemeConfigs = new List<ThemeConfig>();
            foreach (var kvp in _defaulThemePercents)
            {
                Theme theme = kvp.Key;

                string description = $"Specifies the percentage of stages that will be generated with the \"{theme.GetName()}\" theme.";
                var themeConfig = config.Bind("Themes", $"{theme.GetName()} spawn rate", kvp.Value, description);

                ModSettingsManager.AddOption(new StepSliderOption(themeConfig, new StepSliderConfig() { min = 0, max = 1, increment = 0.01f, formatString = "{0:P0}" }));

                ThemeConfigs.Add(new ThemeConfig
                {
                    Theme = kvp.Key,
                    Config = themeConfig
                });

                totalThemes += themeConfig.Value;
            }

            if (totalThemes != 1)
            {
                if (totalThemes <= 0)
                {
                    for (int i = 0; i < ThemeConfigs.Count; i++)
                    {
                        ThemeConfigs[i].Config.Value = _defaulThemePercents[ThemeConfigs[i].Theme];
                    }
                }
                else
                {
                    for (int i = 0; i < ThemeConfigs.Count; i++)
                    {
                        ThemeConfigs[i].Config.Value /= totalThemes;
                    }
                }
            }

            float variedSpawnRate = -0.01f;
            bool isTerrainTypeChanging = false;
            bool isGlobalTerrainTypeChanging = false;
            TerrainTypesPercents = new List<TerrainTypePercentConfig>();
            Dictionary<TerrainType, ConfigEntry<float>> stageGlobalConfigByType = new Dictionary<TerrainType, ConfigEntry<float>>();

            float totalGlobalTerrain = 0;
            foreach (TerrainType terrainType in Enum.GetValues(typeof(TerrainType)))
            {
                if (terrainType != TerrainType.Random && terrainType != TerrainType.Moon)
                {
                    string description = $"Sets the overall percentage of stages that will feature the \"{terrainType.GetName()}\" terrain type. Adjusting this value will automatically update the spawn rates for this terrain type in each individual stage.";
                    ConfigEntry<float> terrainConfig = config.Bind($"All Stages", $"{terrainType.GetName()} map spawn rate", variedSpawnRate, description);

                    ModSettingsManager.AddOption(new StepSliderOption(terrainConfig, new StepSliderConfig() { min = variedSpawnRate, max = 1, increment = 0.01f, formatString = "{0:0%;'Varied';0%}" }));

                    TerrainType currentTerrainType = terrainType;
                    terrainConfig.SettingChanged += (o, e) =>
                    {
                        if (isTerrainTypeChanging || terrainConfig.Value == variedSpawnRate)
                        {
                            return;
                        }
                        try
                        {
                            isGlobalTerrainTypeChanging = true;

                            foreach (TerrainTypePercentConfig stageTerrainConfig in TerrainTypesPercents)
                            {
                                if (stageTerrainConfig.TerrainType == currentTerrainType)
                                {
                                    stageTerrainConfig.Config.Value = terrainConfig.Value;
                                }
                            }
                        }
                        finally
                        {
                            isGlobalTerrainTypeChanging = false;
                        }
                    };

                    stageGlobalConfigByType[terrainType] = terrainConfig;
                    totalGlobalTerrain += terrainConfig.Value;
                }
            }

            if (totalGlobalTerrain > 1)
            {
                foreach (var globalConfig in stageGlobalConfigByType.Values)
                {
                    globalConfig.Value /= totalGlobalTerrain;
                }
            }

            for (int stageIndex = 0; stageIndex < Run.stagesPerLoop; stageIndex++)
            {
                int startIndex = TerrainTypesPercents.Count;
                float totalPercent = 0;

                foreach (TerrainType terrainType in Enum.GetValues(typeof(TerrainType)))
                {
                    if (terrainType != TerrainType.Random && terrainType != TerrainType.Moon)
                    {
                        float defaultPercent = _defaultTerrainTypesPercents[(terrainType, stageIndex)];

                        string description = $"Specifies the percentage of maps that will be generated with the \"{terrainType.GetName()}\" terrain type for stage {stageIndex + 1}. If the total percentage for stage 1 is less than 100%, normal stages may also spawn. If the total percentage for stage {stageIndex + 1} is 0%, only normal stages will spawn.";
                        var terrainConfig = config.Bind($"Stage {stageIndex + 1}", $"{terrainType.GetName()} map spawn rate", defaultPercent, description);
                        if (lastVersion < SemanticVersion.Parse("1.15.0"))
                        {
                            terrainConfig.Value = defaultPercent;
                        }

                        TerrainType currentTerrainType = terrainType;
                        terrainConfig.SettingChanged += (o, e) =>
                        {
                            if (isGlobalTerrainTypeChanging)
                            {
                                return;
                            }

                            try
                            {
                                isTerrainTypeChanging = true;

                                foreach (TerrainTypePercentConfig terrainPercentConfig in TerrainTypesPercents)
                                {
                                    if (terrainPercentConfig.TerrainType != currentTerrainType)
                                    {
                                        continue;
                                    }

                                    if (terrainPercentConfig.Config.Value != terrainConfig.Value)
                                    {
                                        stageGlobalConfigByType[currentTerrainType].Value = variedSpawnRate;
                                        return;
                                    }
                                }

                                stageGlobalConfigByType[currentTerrainType].Value = terrainConfig.Value;
                            }
                            finally
                            {
                                isTerrainTypeChanging = false;
                            }
                        };

                        ModSettingsManager.AddOption(new StepSliderOption(terrainConfig, new StepSliderConfig() { min = 0, max = 1, increment = 0.01f, formatString = "{0:P0}" }));

                        TerrainTypesPercents.Add(new TerrainTypePercentConfig
                        {
                            TerrainType = terrainType,
                            StageIndex = stageIndex,
                            Config = terrainConfig
                        });

                        if (terrainConfig.Value < 0)
                        {
                            terrainConfig.Value = 0;
                        }

                        totalPercent += terrainConfig.Value;
                    }
                }

                if (totalPercent > 1)
                {
                    for (int i = startIndex; i < TerrainTypesPercents.Count; i++)
                    {
                        TerrainTypesPercents[i].Config.Value /= totalPercent;
                    }
                }
            }

            StageSeed = config.Bind("Debug", "Stage seed", "", "Specifies the stage seed. If left blank, a random seed will be used.");
            ModSettingsManager.AddOption(new StringInputFieldOption(StageSeed));
        }
    }
}

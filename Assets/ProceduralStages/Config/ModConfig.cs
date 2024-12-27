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
using System.Text.RegularExpressions;

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

    public struct VanillaStageConfig
    {
        public VanillaStageDef Stage;
        public ConfigEntry<float> Config;
    }

    public enum TerrainRepetition
    {
        Yes,
        NonePerLoop
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
        public static ConfigEntry<int> MinStageCount;
        public static ConfigEntry<TerrainRepetition> TerrainTypeRepetition;
        public static List<TerrainTypePercentConfig> TerrainTypesPercents;

        public static List<VanillaStageConfig> VanillaStageThemePercents;

        public static ConfigEntry<float> MoonSpawnRate;
        public static ConfigEntry<int> MoonRequiredPillarsCount;

        public static ConfigEntry<bool> PotRollingModeEnabled;
        public static ConfigEntry<int> PotRollingStageWidth;
        public static ConfigEntry<int> PotRollingStageHeight;
        public static ConfigEntry<int> PotRollingStageDepth;

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

                string description = $"Specifies the percentage of stages that will be generated with the <style=cIsHealing>{theme.GetName()}</style> theme.";
                var themeConfig = config.Bind("Themes", $"{theme.GetName()} spawn rate", kvp.Value, description);

                ModSettingsManager.AddOption(new StepSliderOption(themeConfig, new StepSliderConfig() { min = 0, max = 1, increment = 0.01f, FormatString = "{0:P0}" }));

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

            VanillaStageThemePercents = new List<VanillaStageConfig>();
            foreach (VanillaStageDef stageDef in ContentProvider.themeGeneratorPrefab.GetComponent<ThemeGenerator>().stages)
            {
                string stageToken = stageDef.nameToken != ""
                    ? stageDef.nameToken
                    : SceneCatalog.FindSceneDef(stageDef.sceneName).nameToken;

                string stageName = Language.GetString(stageToken);

                if (stageDef.variant)
                {
                    stageName += " (variant)";
                }

                ConfigEntry<float> themeConfig = config.Bind("Vanilla Stages Themes", $"{NormaliseName(stageName)}", 1f, "Specifies the probability that the vanilla theme for this stage is replaced with a custom theme.");
                ModSettingsManager.AddOption(new StepSliderOption(themeConfig, new StepSliderConfig() { min = 0, max = 1, increment = 0.01f, FormatString = "{0:P0}" }));

                VanillaStageThemePercents.Add(new VanillaStageConfig
                {
                    Stage = stageDef,
                    Config = themeConfig
                });
            }

            MinStageCount = config.Bind("All Stages", "Min stage count", 1, "Defines the minimum number of stages required to enable the spawning of procedural stages.");
            ModSettingsManager.AddOption(new IntSliderOption(MinStageCount, new IntSliderConfig { min = 1, max = 15 }));

            TerrainTypeRepetition = config.Bind("All Stages", "Stage repetition", TerrainRepetition.NonePerLoop, "Specifies whether a stage can be repeated.\r\n\r\n<style=cIsHealing>Yes</style>: The stage can be repeated multiple times.\r\n<style=cIsHealing>NonePerLoop</style>: The stage cannot be repeated within the same loop.");
            ModSettingsManager.AddOption(new ChoiceOption(TerrainTypeRepetition));

            float variedSpawnRate = -0.01f;
            bool isTerrainTypeChanging = false;
            bool isGlobalTerrainTypeChanging = false;
            TerrainTypesPercents = new List<TerrainTypePercentConfig>();
            Dictionary<TerrainType, ConfigEntry<float>> stageGlobalConfigByType = new Dictionary<TerrainType, ConfigEntry<float>>();

            float totalGlobalTerrain = 0;
            foreach (TerrainType terrainType in Enum.GetValues(typeof(TerrainType)))
            {
                if (terrainType.IsNormalStage())
                {
                    string description = $"Sets the overall percentage of stages that will feature the <style=cIsHealing>{terrainType.GetName()}</style> terrain type. Adjusting this value will automatically update the spawn rates for this terrain type in each individual stage.";
                    ConfigEntry<float> terrainConfig = config.Bind($"All Stages", $"{terrainType.GetName()} map spawn rate", variedSpawnRate, description);

                    ModSettingsManager.AddOption(new StepSliderOption(terrainConfig, new StepSliderConfig() { min = variedSpawnRate, max = 1, increment = 0.01f, FormatString = "{0:0%;'Varied';0%}" }));

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
                    if (terrainType.IsNormalStage())
                    {
                        float defaultPercent = _defaultTerrainTypesPercents[(terrainType, stageIndex)];

                        string description = $"Specifies the percentage of maps that will be generated with the <style=cIsHealing>{terrainType.GetName()}</style> terrain type for stage {stageIndex + 1}. If the total percentage for stage 1 is less than 100%, normal stages may also spawn. If the total percentage for stage {stageIndex + 1} is 0%, only normal stages will spawn.";
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

                        ModSettingsManager.AddOption(new StepSliderOption(terrainConfig, new StepSliderConfig() { min = 0, max = 1, increment = 0.01f, FormatString = "{0:P0}" }));

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

            MoonSpawnRate = config.Bind("Moon", "Lunar Fields map spawn rate", 1f, "Indicates the percentage of final stages featuring the custom <style=cIsHealing>Lunar Fields</style> terrain type instead of the vanilla moon stage. If this percentage is less than 100%, the normal moon stage will also appear. If the total percentage is 0%, only the normal moon stage will be generated.");
            ModSettingsManager.AddOption(new StepSliderOption(MoonSpawnRate, new StepSliderConfig() { min = 0, max = 1, increment = 0.01f, FormatString = "{0:P0}" }));

            MoonRequiredPillarsCount = config.Bind("Moon", "Required pillars", 4, "Number of pillars necessary to access the Mithrix arena");
            ModSettingsManager.AddOption(new IntSliderOption(MoonRequiredPillarsCount, new IntSliderConfig() { min = 0, max = 7 }));

            StageSeed = config.Bind("Debug", "Stage seed", "", "Specifies the stage seed. If left blank, a random seed will be used.");
            ModSettingsManager.AddOption(new StringInputFieldOption(StageSeed));

            PotRollingModeEnabled = config.Bind("Pot rolling", "Enabled", false, "This feature is not production ready. Do not touch");
            ModSettingsManager.AddOption(new CheckBoxOption(PotRollingModeEnabled));

            PotRollingStageWidth = config.Bind("Pot rolling", "Stage width", 200, "This feature is not production ready. Do not touch");
            ModSettingsManager.AddOption(new IntSliderOption(PotRollingStageWidth, new IntSliderConfig { min = 20, max = 500 }));

            PotRollingStageHeight = config.Bind("Pot rolling", "Stage height", 30, "This feature is not production ready. Do not touch");
            ModSettingsManager.AddOption(new IntSliderOption(PotRollingStageHeight, new IntSliderConfig { min = 10, max = 100 }));

            PotRollingStageDepth = config.Bind("Pot rolling", "Stage depth", 900, "This feature is not production ready. Do not touch");
            ModSettingsManager.AddOption(new IntSliderOption(PotRollingStageDepth, new IntSliderConfig { min = 50, max = 2000 }));

            string NormaliseName(string name)
            {
                //'=', '\n', '\t', '\\', '"', '\'', '[', ']'
                return Regex.Replace(name, @"[=\n\t\\""'\[\]]", "").Trim();
            }
        }
    }
}

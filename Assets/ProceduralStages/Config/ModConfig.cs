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

    public static class ModConfig
    {
        private static Dictionary<(TerrainType, int), float> _defaultTerrainTypesPercents = new Dictionary<(TerrainType, int), float>
        {
            [(TerrainType.OpenCaves, 0)] = 0.4f,
            [(TerrainType.TunnelCaves, 0)] = 0.4f,
            [(TerrainType.Islands, 0)] = 0.2f,

            [(TerrainType.OpenCaves, 1)] = 0.4f,
            [(TerrainType.TunnelCaves, 1)] = 0.2f,
            [(TerrainType.Islands, 1)] = 0.4f,

            [(TerrainType.OpenCaves, 2)] = 0.3f,
            [(TerrainType.TunnelCaves, 2)] = 0.6f,
            [(TerrainType.Islands, 2)] = 0.1f,

            [(TerrainType.OpenCaves, 3)] = 0.20f,
            [(TerrainType.TunnelCaves, 3)] = 0.1f,
            [(TerrainType.Islands, 3)] = 0.7f,

            [(TerrainType.OpenCaves, 4)] = 0.20f,
            [(TerrainType.TunnelCaves, 4)] = 0.40f,
            [(TerrainType.Islands, 4)] = 0.40f,
        };

        public static ConfigEntry<string> ConfigVersion;

        public static ConfigEntry<string> StageSeed;
        public static ConfigEntry<bool> InfiniteMapScaling;

        public static List<TerrainTypePercentConfig> TerrainTypesPercents;

        public static void Init(ConfigFile config)
        {
            ConfigVersion = config.Bind("Configuration", "Last version played", Main.PluginVersion, "Do not touch");
            //Upgrade config here
            ConfigVersion.Value = Main.PluginVersion;

            InfiniteMapScaling = config.Bind("Configuration", "Infinite map scaling", false, "If enabled, the stage size scaling will not be reset every loop. Exercise caution when utilizing this feature, as it may lead to increased map generation time and a decrease in framerate.");
            ModSettingsManager.AddOption(new CheckBoxOption(InfiniteMapScaling));

            TerrainTypesPercents = new List<TerrainTypePercentConfig>();

            for (int stageIndex = 0; stageIndex < Run.stagesPerLoop; stageIndex++)
            {
                int startIndex = TerrainTypesPercents.Count;
                float totalPercent = 0;

                foreach (TerrainType terrainType in Enum.GetValues(typeof(TerrainType)))
                {
                    if (terrainType != TerrainType.Random)
                    {
                        float defaultPercent = _defaultTerrainTypesPercents[(terrainType, stageIndex)];

                        string description = $"Specifies the percentage of maps that will be generated with the \"{terrainType.GetName()}\" terrain type for stage {stageIndex + 1}. If the total percentage for stage 1 is less than 100%, normal stages may also spawn. If the total percentage for stage {stageIndex + 1} is 0%, only normal stages will spawn.";
                        var terrainConfig = config.Bind($"Stage {stageIndex + 1}", $"{terrainType.GetName()} map spawn rate", defaultPercent, description);
                        //terrainConfig.Value = defaultPercent;
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

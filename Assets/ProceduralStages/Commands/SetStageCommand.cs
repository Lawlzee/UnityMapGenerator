using RoR2;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace ProceduralStages
{
    public static class SetStageCommand
    {
        [ConCommand(commandName = "ps_set_stage", flags = ConVarFlags.None, helpText = @"Set the procedural stage. 
syntax: 'ps_set_stage <terrain_type> <theme> <stage_count>'. 
Terrain types are: Random, OpenCaves, Islands, TunnelCaves, Mines, Basalt, Towers, Temple, Moon and PotRolling
Themes are: Random, LegacyRandom, Desert, Snow, Void, Plains and Mushroom")]
        public static void SetStage(ConCommandArgs args)
        {
            TerrainType terrainType = TerrainType.Random;
            if (args.Count >= 1 && !Enum.TryParse(args.GetArgString(0), ignoreCase: true, out terrainType))
            {
                Debug.Log($"Invalid terrain type");
                return;
            }

            Theme theme = Theme.Random;
            if (args.Count >= 2 && !Enum.TryParse(args.GetArgString(1), ignoreCase: true, out theme))
            {
                Debug.Log($"Invalid theme");
                return;
            }

            if (args.Count >= 3)
            {
                int? stageCount = args.TryGetArgInt(2);
                if (stageCount == null)
                {
                    Debug.Log($"Invalid stage count");
                    return;
                }
                RunConfig.instance.nextStageClearCount = stageCount.Value;
            }

            RunConfig.instance.selectedTerrainType = terrainType;
            RunConfig.instance.selectedTheme = theme;

            RoR2.Console.instance.SubmitCmd(args.sender, "set_scene random");
        }
    }
}

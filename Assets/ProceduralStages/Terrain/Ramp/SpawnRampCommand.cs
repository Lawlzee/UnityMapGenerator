using RoR2;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProceduralStages
{
    public static class SpawnRampCommand
    {
        [ConCommand(commandName = "spawn_ramp", flags = ConVarFlags.None, helpText = "Spawn a ramp. syntax: 'spawn_ramp <size_width=50> <size_height=40> <size_depth=100> <distance=0> <y_offset=-10> <noise_level=1> <props_weight=-0.1>'")]
        public static void SpawnRamp(ConCommandArgs args)
        {
            ProceduralRamp ramp = ProceduralRamp.instance ?? UnityEngine.Object.Instantiate(ContentProvider.rampPrefab).GetComponent<ProceduralRamp>();
            ramp.Generate(
                new Vector3(GetValue(0, 50), 
                    GetValue(1, 40), 
                    GetValue(2, 100)), 
                GetValue(3, 0), 
                GetValue(4, -10), 
                GetValue(5, 1),
                GetValue(6, 0.1f));

            float GetValue(int index, float defaultValue)
            {
                if (index < args.Count)
                {
                    return args.GetArgFloat(index);
                }

                return defaultValue;
            }
        }
    }
}

using RoR2.Navigation;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using RoR2.EntityLogic;
using RoR2.Networking;
using UnityEngine.Networking;
using RiskOfOptions.Resources;
using TMPro;

namespace ProceduralStages
{
    public class SpecialInteractablesPlacer
    {
        public static void Place(
            Graphs graphs,
            int stageInLoop,
            MoonTerrain moonTerrain)
        {
            if (MapGenerator.instance.stageType == StageType.Moon || MapGenerator.instance.stageType == StageType.PotRolling)
            {
                return;
            }

            if (MapGenerator.instance.stageType == StageType.Regular)
            {
                InteractablePlacer.Place(
                    graphs,
                    "RoR2/Base/NewtStatue/NewtStatue.prefab",
                    NodeFlagsExt.Newt,
                    normal: Vector3.up,
                    offset: Vector3.up,
                    lookAwayFromWall: true);

                if (stageInLoop == 3 && MapGenerator.serverRng.nextNormalizedFloat < (1 / 3f))
                {
                    InteractablePlacer.Place(
                        graphs,
                        "RoR2/Base/TimedChest/TimedChest.prefab",
                        NodeFlagsExt.Newt,
                        normal: Vector3.up,
                        offset: new Vector3(0, 0.6f, 0),
                        lookAwayFromWall: true);
                }

                if (stageInLoop == 2 && MapGenerator.serverRng.nextNormalizedFloat < (1 / 3f))
                {
                    RingEvent.Add(graphs);
                }
            }

            if (stageInLoop == 4)
            {
                if (MapGenerator.instance.stageType == StageType.Simulacrum || MapGenerator.serverRng.nextNormalizedFloat < (2 / 3f))
                {
                    InteractablePlacer.Place(graphs, "RoR2/Base/GoldChest/GoldChest.prefab", NodeFlagsExt.Newt, skipSpawnWhenSacrificeArtifactEnabled: true);
                }
                else
                {
                    MapGenerator.instance.awuEvent.seed = MapGenerator.serverRng.nextUlong;
                    MapGenerator.instance.awuEvent.enabled = true;
                }
            }
        }
    }
}

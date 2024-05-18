using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace ProceduralStages
{
    [DefaultExecutionOrder(-1)]
    public class MoonPillarsMission : NetworkBehaviour
    {
        public MoonPillars pillars;

        public void Awake()
        {
            Debug.Log("pillarIds.Count = " + pillars.pillarIds.Count);
            Debug.Log("elevatorIds.Count = " + pillars.elevatorIds.Count);

            MoonBatteryMissionController controller = GetComponent<MoonBatteryMissionController>();
            controller.moonBatteries = pillars.pillarIds
                .Select(NetworkServer.FindLocalObject)
                .ToArray();

            controller.elevators = pillars.elevatorIds
                .Select(NetworkServer.FindLocalObject)
                .ToArray();
        }
    }
}

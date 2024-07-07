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
        private MoonBatteryMissionController controller;

        public void Awake()
        {
            Debug.Log("pillarIds.Count = " + pillars.pillarIds.Count);
            Debug.Log("elevatorIds.Count = " + pillars.elevatorIds.Count);

            controller = GetComponent<MoonBatteryMissionController>();
            controller.moonBatteries = pillars.pillarIds
                .Select(NetworkServer.FindLocalObject)
                .ToArray();


            controller.elevators = pillars.elevatorIds
                .Select(NetworkServer.FindLocalObject)
                .ToArray();
        }

        public void Start()
        {
            for (int i = 0; i < controller.batteryHoldoutZones.Length; i++)
            {
                var pillar = controller.batteryHoldoutZones[i];
                pillar.onCharged.AddListener(OnBatteryCharged);
            }
        }

        public void OnBatteryCharged(HoldoutZoneController holdoutZone)
        {
            if (controller.numChargedBatteries < controller.numRequiredBatteries)
            {
                return;
            }

            for (int i = 0; i < controller.batteryHoldoutZones.Length; i++)
            {
                var pillar = controller.batteryHoldoutZones[i];
                pillar.onCharged.RemoveListener(OnBatteryCharged);
            }

            pillars.globalSphereScaleCurve.enabled = true;
        }
    }
}

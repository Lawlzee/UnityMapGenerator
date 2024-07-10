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

            controller = GetComponent<MoonBatteryMissionController>();
            controller.moonBatteries = pillars.pillarIds
                .Select(NetworkServer.FindLocalObject)
                .ToArray();

            controller.elevators = new GameObject[0];

            if (NetworkServer.active)
            {
                if (ModConfig.MoonRequiredPillarsCount.Value == 0)
                {
                    controller._numChargedBatteries = int.MaxValue;
                }
                controller._numRequiredBatteries = ModConfig.MoonRequiredPillarsCount.Value;
            }
        }

        public void Start()
        {
            if (controller.numChargedBatteries >= controller.numRequiredBatteries)
            {
                pillars.globalSphereScaleCurve.enabled = true;
                return;
            }

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

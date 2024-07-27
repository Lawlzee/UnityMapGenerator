using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine;
using RoR2;
using UnityEngine.Networking;

namespace ProceduralStages
{
    [Serializable]
    public struct PillarIds : IEquatable<PillarIds>
    {
        public int id0;
        public int id1;
        public int id2;
        public int id3;
        public int id4;
        public int id5;
        public int id6;

        public int this[int i]
        {
            get
            {
                switch (i)
                {
                    case 0: return id0;
                    case 1: return id1;
                    case 2: return id2;
                    case 3: return id3;
                    case 4: return id4;
                    case 5: return id5;
                    case 6: return id6;
                }

                return -1;
            }
            set
            {
                switch (i)
                {
                    case 0: id0 = value; break;
                    case 1: id1 = value; break;
                    case 2: id2 = value; break;
                    case 3: id3 = value; break;
                    case 4: id4 = value; break;
                    case 5: id5 = value; break;
                    case 6: id6 = value; break;
                }
            }
        }

        public bool Equals(PillarIds other)
        {
            return id0 == other.id0
                && id1 == other.id1
                && id2 == other.id2
                && id3 == other.id3
                && id4 == other.id4
                && id5 == other.id5
                && id6 == other.id6;
        }
    }

    public class MoonPillars : NetworkBehaviour
    {
        [SyncVar/*(hook = nameof(OnPillarsSent))*/]
        public PillarIds pillarIds;
        public List<Vector3> pillarPositions;
        private bool _pillarInitialised;
        private bool _pillarsCharged;

        public MoonBatteryMissionController controller;
        public ObjectScaleCurve globalSphereScaleCurve;
        public Xoroshiro128Plus rng;

        public void Start()
        {
            Log.Debug("Start");
        }

        public override void OnStartClient()
        {
            Log.Debug("OnStartClient");
        }

        public void Awake()
        {
            Log.Debug("Awake");
            Log.Debug("AA0");
            if (NetworkServer.active)
            {
                WeightedSelection<GameObject> prefabs = new WeightedSelection<GameObject>();
                prefabs.AddChoice(Addressables.LoadAssetAsync<GameObject>("RoR2/Base/moon2/MoonBatterySoul.prefab").WaitForCompletion(), 1);
                prefabs.AddChoice(Addressables.LoadAssetAsync<GameObject>("RoR2/Base/moon2/MoonBatteryMass.prefab").WaitForCompletion(), 1);
                prefabs.AddChoice(Addressables.LoadAssetAsync<GameObject>("RoR2/Base/moon2/MoonBatteryDesign.prefab").WaitForCompletion(), 1);
                prefabs.AddChoice(Addressables.LoadAssetAsync<GameObject>("RoR2/Base/moon2/MoonBatteryBlood.prefab").WaitForCompletion(), 1);

                PillarIds ids = pillarIds;

                for (int i = 0; i < pillarPositions.Count; i++)
                {
                    var prefab = prefabs.Evaluate(rng.nextNormalizedFloat);

                    var pillar = Instantiate(prefab, transform);

                    pillar.transform.position = pillarPositions[i];

                    NetworkServer.Spawn(pillar);
                    ids[i] = (int)pillar.GetComponent<NetworkIdentity>().netId.Value;
                }

                pillarIds = ids;
            }
        }
        /*
        private void OnPillarsSent(PillarIds newValue)
        {
            Debug.Log("OnPillarsSent");
            //if (pillarIds.Count == pillarPositions.Count)
            {

            }
        }
        */
        public void Update()
        {
            if (!_pillarsCharged)
            {
                if (controller.numChargedBatteries >= controller._numRequiredBatteries)
                {
                    if (globalSphereScaleCurve != null)
                    {
                        globalSphereScaleCurve.enabled = true;
                    }
                    _pillarsCharged = true;
                }
            }

            if (_pillarInitialised || pillarPositions.Count == 0)
            {
                return;
            }

            Debug.Log("Update");

            GameObject[] pillars = new GameObject[pillarPositions.Count];
            for (int i = 0; i < pillarPositions.Count; i++)
            {
                int netId = pillarIds[i];
                if (netId == -1)
                {

                    Debug.Log("Update -1");
                    return;
                }

                GameObject pillar = ClientScene.FindLocalObject(new NetworkInstanceId((uint)netId));
                if (pillar == null)
                {
                    Debug.Log("Update null");
                    return;
                }
                pillars[i] = pillar;
            }

            _pillarInitialised = true;

            controller.moonBatteries = pillars;
            controller.elevators = new GameObject[0];

            if (NetworkServer.active && ModConfig.MoonRequiredPillarsCount.Value == 0)
            {
                controller._numChargedBatteries = int.MaxValue;
            }

            controller._numRequiredBatteries = RunConfig.instance.moonRequiredPillarsCount;

            controller.Awake();
            controller.enabled = true;
        }
    }
}

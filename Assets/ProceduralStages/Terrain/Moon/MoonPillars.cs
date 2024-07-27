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
    public class SyncListNetworkInstanceId : SyncList<NetworkInstanceId>
    {
        protected override void SerializeItem(NetworkWriter writer, NetworkInstanceId item)
        {
            writer.Write(item);
        }

        protected override NetworkInstanceId DeserializeItem(NetworkReader reader)
        {
            return reader.ReadNetworkId();
        }
    }

    public class MoonPillars : NetworkBehaviour
    {
        public SyncListNetworkInstanceId pillarIds;
        public List<Vector3> pillarPositions;

        public MoonPillarsMission mission;
        public ObjectScaleCurve globalSphereScaleCurve;
        public Xoroshiro128Plus rng;

        public void Awake()
        {
            if (NetworkServer.active)
            {
                //NetworkServer.Spawn(gameObject);
            }
        }

        public void Start()
        {
            Log.Debug("AA0");
            if (NetworkServer.active)
            {
                WeightedSelection<GameObject> prefabs = new WeightedSelection<GameObject>();
                prefabs.AddChoice(Addressables.LoadAssetAsync<GameObject>("RoR2/Base/moon2/MoonBatterySoul.prefab").WaitForCompletion(), 1);
                prefabs.AddChoice(Addressables.LoadAssetAsync<GameObject>("RoR2/Base/moon2/MoonBatteryMass.prefab").WaitForCompletion(), 1);
                prefabs.AddChoice(Addressables.LoadAssetAsync<GameObject>("RoR2/Base/moon2/MoonBatteryDesign.prefab").WaitForCompletion(), 1);
                prefabs.AddChoice(Addressables.LoadAssetAsync<GameObject>("RoR2/Base/moon2/MoonBatteryBlood.prefab").WaitForCompletion(), 1);

                for (int i = 0; i < pillarPositions.Count; i++)
                {
                    var prefab = prefabs.Evaluate(rng.nextNormalizedFloat);

                    var pillar = Instantiate(prefab, transform);

                    pillar.transform.position = pillarPositions[i];

                    NetworkServer.Spawn(pillar);
                    pillarIds.Add(pillar.GetComponent<NetworkIdentity>().netId);
                }
                mission.gameObject.SetActive(true);
                //NetworkServer.Spawn(mission.gameObject);

            }
            else
            {
                mission.gameObject.SetActive(true);
            }
        }
    }
}

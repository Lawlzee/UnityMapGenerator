using RoR2.Navigation;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine;
using RoR2.EntityLogic;

namespace ProceduralStages
{
    public static class RingEvent
    {
        public static void Add(Graphs graphs)
        {
            GameObject ringEventController = new GameObject();
            ringEventController.name = "RingEventController";
            Counter counter = ringEventController.AddComponent<Counter>();
            counter.threshold = 2;
            counter.onTrigger = new UnityEvent();

            //same amount of pots than goolake
            int potCount = MapGenerator.serverRng.RangeInt(8, 31);

            for (int i = 0; i < potCount; i++)
            {
                GameObject plateObject = InteractablePlacer.Place(graphs, "RoR2/Base/ExplosivePotDestructible/ExplosivePotDestructibleBody.prefab", NodeFlagsExt.Newt, offset: Vector3.up, rng: MapGenerator.serverRng);
                if (plateObject)
                {
                    plateObject.GetComponentInChildren<MeshRenderer>().enabled = true;
                }
            }

            Vector3 targetPosition = Vector3.zero;

            List<GameObject> plates = new List<GameObject>();

            for (int i = 0; i < 2; i++)
            {
                GameObject plateObject = InteractablePlacer.Place(graphs, "RoR2/Base/goolake/GLPressurePlate.prefab", NodeFlagsExt.Newt);
                if (plateObject)
                {
                    PressurePlateController pressurePlateController = plateObject.GetComponent<PressurePlateController>();
                    pressurePlateController.OnSwitchDown.AddListener(() => targetPosition = plateObject.transform.position);
                    pressurePlateController.OnSwitchDown.AddListener(() => counter.Add(1));
                    pressurePlateController.OnSwitchUp.AddListener(() => counter.Add(-1));

                    counter.onTrigger.AddListener(() => pressurePlateController.EnableOverlapSphere(false));

                    plates.Add(plateObject);
                }
            }

            Xoroshiro128Plus lemurianRng = new Xoroshiro128Plus(MapGenerator.serverRng.nextUlong);

            counter.onTrigger.AddListener(() =>
            {
                Chat.SendBroadcastChat(new Chat.SimpleChatMessage()
                {
                    baseToken = "STONEGATE_OPEN"
                });

                SpawnLemurian("RoR2/Base/goolake/LemurianBruiserMasterFire.prefab");
                SpawnLemurian("RoR2/Base/goolake/LemurianBruiserMasterIce.prefab");
            });

            void SpawnLemurian(string assset)
            {
                CharacterSpawnCard lemurian = ScriptableObject.CreateInstance<CharacterSpawnCard>();
                lemurian.prefab = Addressables.LoadAssetAsync<GameObject>(assset).WaitForCompletion();
                lemurian.sendOverNetwork = true;
                lemurian.hullSize = HullClassification.Golem;
                lemurian.nodeGraphType = MapNodeGroup.GraphType.Ground;
                lemurian.forbiddenFlags = NodeFlags.NoCharacterSpawn;

                var request = new DirectorSpawnRequest(
                    lemurian,
                    new DirectorPlacementRule()
                    {
                        placementMode = DirectorPlacementRule.PlacementMode.Approximate,
                        position = targetPosition,
                        //MonsterSpawnDistance.Standard
                        minDistance = 25,
                        maxDistance = 40
                    },
                    lemurianRng)
                {
                    teamIndexOverride = TeamIndex.Monster,
                    ignoreTeamMemberLimit = true
                };

                DirectorCore.instance.TrySpawnObject(request);
            }
        }
    }
}

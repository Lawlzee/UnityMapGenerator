using RoR2;
using RoR2.Networking;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace ProceduralStages
{
    [RequireComponent(typeof(BossGroup))]
    public class AwuEventBehaviour : NetworkBehaviour
    {
        [SyncVar(hook = nameof(OnSeedSet))]
        public ulong seed;

        public Xoroshiro128Plus rng;

        private int deathCount;
        private float encounterCountdown = 2;
        private Vector3 targetPosition;

        private BossGroup bossGroup;
        private CombatSquad combatSquad;

        private void Awake()
        {
            bossGroup = GetComponent<BossGroup>();
            combatSquad = GetComponent<CombatSquad>();

            bossGroup.dropTable = Addressables.LoadAssetAsync<PickupDropTable>("RoR2/Base/Common/dtTier3Item.asset").WaitForCompletion();
        }

        private void OnSeedSet(ulong seed)
        {
            enabled = true;
        }

        private void Start()
        {
            rng = new Xoroshiro128Plus(seed);
            var graphs = MapGenerator.instance.graphs;

            for (int i = 0; i < 12; i++)
            {
                GameObject egg = InteractablePlacer.Place(graphs, "RoR2/Base/shipgraveyard/VultureNest.prefab", NodeFlagsExt.Newt, spawnServer: false, rng: rng);
                if (egg)
                {
                    GenericSceneSpawnPoint oldSpawnPoint = egg.GetComponentInChildren<GenericSceneSpawnPoint>();
                    oldSpawnPoint.enabled = false;

                    if (NetworkServer.active)
                    {
                        EggSpawnPoint eggSpawnPoint = oldSpawnPoint.gameObject.AddComponent<EggSpawnPoint>();
                        eggSpawnPoint.onDeath = OnEggDeath;
                    }
                    else
                    {
                        oldSpawnPoint.gameObject.SetActive(false);
                    }
                }
            }
        }

        private void OnEggDeath(GameObject egg)
        {
            deathCount++;
            if (deathCount == 5)
            {
                Chat.SendBroadcastChat(new Chat.SimpleChatMessage()
                {
                    baseToken = "VULTURE_EGG_WARNING"
                });
            }

            if (deathCount == 6)
            {
                targetPosition = egg.transform.position;

                Chat.SendBroadcastChat(new Chat.SimpleChatMessage()
                {
                    baseToken = "VULTURE_EGG_BEGIN"
                });
            }
        }

        private void FixedUpdate()
        {
            if (!NetworkServer.active || deathCount < 6)
            {
                return;
            }

            encounterCountdown -= Time.fixedDeltaTime;
            if (encounterCountdown < 0)
            {
                CharacterSpawnCard spawnCard = Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/Base/RoboBallBoss/cscSuperRoboBallBoss.asset").WaitForCompletion();

                var request = new DirectorSpawnRequest(
                    spawnCard,
                    new DirectorPlacementRule()
                    {
                        placementMode = DirectorPlacementRule.PlacementMode.Approximate,
                        position = targetPosition,
                        //MonsterSpawnDistance.Standard
                        minDistance = 25,
                        maxDistance = 40
                    },
                    rng)
                {
                    teamIndexOverride = TeamIndex.Monster,
                    ignoreTeamMemberLimit = true
                };

                GameObject awu = DirectorCore.instance.TrySpawnObject(request);
                
                if (!awu)
                {
                    request.placementRule.minDistance = 40;
                    request.placementRule.maxDistance = 75;
                    awu = DirectorCore.instance.TrySpawnObject(request);
                }

                if (!awu)
                {
                    request.placementRule.placementMode = DirectorPlacementRule.PlacementMode.Random;
                    awu = DirectorCore.instance.TrySpawnObject(request);
                }

                if (awu)
                {
                    bossGroup.dropPosition = awu.GetComponent<CharacterMaster>().bodyInstanceObject.transform;
                    combatSquad.AddMember(awu.GetComponent<CharacterMaster>());
                }

                enabled = false;
            }
        }
    }

    public class EggSpawnPoint : MonoBehaviour
    {
        public Action<GameObject> onDeath;

        private void Start()
        {
            GameObject egg = Instantiate(Addressables.LoadAssetAsync<GameObject>("RoR2/Base/shipgraveyard/VultureEggBody.prefab").WaitForCompletion(), transform.position, transform.rotation);
            NetworkServer.Spawn(egg);

            EggDeathBehavior deathBehavior = egg.AddComponent<EggDeathBehavior>();
            deathBehavior.onDeath = onDeath;

            gameObject.SetActive(false);
        }
    }

    public class EggDeathBehavior : MonoBehaviour, ILifeBehavior
    {
        public Action<GameObject> onDeath;

        public void OnDeathStart()
        {
            onDeath(gameObject);
        }
    }
}

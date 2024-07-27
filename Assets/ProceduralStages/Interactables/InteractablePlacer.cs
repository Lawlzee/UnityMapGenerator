using RoR2;
using RoR2.Navigation;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.UIElements;

namespace ProceduralStages
{
    public static class InteractablePlacer
    {
        public static GameObject Place(
            Graphs graphs,
            string prefab,
            NodeFlags requiredFlags,
            Vector3 offset = default,
            Vector3? normal = null,
            bool skipSpawnWhenSacrificeArtifactEnabled = false,
            bool lookAwayFromWall = false,
            bool spawnServer = true,
            Xoroshiro128Plus rng = null)
        {
            if (skipSpawnWhenSacrificeArtifactEnabled && RunArtifactManager.instance.IsArtifactEnabled(RoR2Content.Artifacts.sacrificeArtifactDef))
            {
                return null;
            }

            rng = rng ?? MapGenerator.serverRng;

            var card = ScriptableObject.CreateInstance<SpawnCard>();
            card.prefab = Addressables.LoadAssetAsync<GameObject>(prefab).WaitForCompletion();
            card.hullSize = HullClassification.Human;
            card.nodeGraphType = MapNodeGroup.GraphType.Ground;
            card.requiredFlags = requiredFlags;
            card.forbiddenFlags = NodeFlags.None;
            card.directorCreditCost = 0;
            card.occupyPosition = true;
            card.eliteRules = SpawnCard.EliteRules.Default;
            //card.sendOverNetwork = true;

            DirectorPlacementRule placementRule = new DirectorPlacementRule()
            {
                placementMode = DirectorPlacementRule.PlacementMode.Random
            };

            GameObject gameObject = null;

            for (int i = 0; gameObject == null && i < 10; i++)
            {
                gameObject = DirectorCore.instance.TrySpawnObject(new DirectorSpawnRequest(card, placementRule, rng));
            }

            if (gameObject)
            {
                var floorNormal = normal ?? graphs.nodeInfoByPosition[gameObject.transform.position].normal;

                gameObject.transform.rotation = Quaternion.FromToRotation(Vector3.up, floorNormal)
                    * card.prefab.transform.rotation;

                if (lookAwayFromWall)
                {
                    RaycastHit? bestRay = null;
                    int bestAngle = 0;

                    for (int i = 0; i < 12; i++)
                    {
                        var lookAngle = Quaternion.AngleAxis(i * 30, Vector3.up) * Vector3.forward;

                        if (Physics.Raycast(new Ray(gameObject.transform.position + Vector3.up * InteractableSpawnCard.floorOffset, lookAngle), out RaycastHit hitInfo, maxDistance: 12, (int)LayerIndex.world.mask))
                        {
                            if (bestRay == null || hitInfo.distance < bestRay.Value.distance)
                            {
                                bestRay = hitInfo;
                                bestAngle = i * 30;
                            }
                        }
                    }

                    gameObject.transform.Rotate(floorNormal, (rng.nextNormalizedFloat - 0.5f) * 30 + bestAngle + 180, Space.World);
                }
                else
                {
                    gameObject.transform.Rotate(floorNormal, rng.nextNormalizedFloat * 360f, Space.World);
                }

                gameObject.transform.position = gameObject.transform.position + offset;

                PurchaseInteraction purchaseInteraction = gameObject.GetComponent<PurchaseInteraction>();
                if (purchaseInteraction && purchaseInteraction.costType == CostTypeIndex.Money)
                {
                    purchaseInteraction.Networkcost = Run.instance.GetDifficultyScaledCost(purchaseInteraction.cost);
                }

                if (spawnServer)
                {
                    NetworkServer.Spawn(gameObject);
                }
            }

            return gameObject;
        }
    }
}

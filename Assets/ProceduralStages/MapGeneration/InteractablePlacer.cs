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
            string prefab,
            NodeFlags requiredFlags,
            Vector3 offset = default)
        {
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

            GameObject gameObject = DirectorCore.instance.TrySpawnObject(new DirectorSpawnRequest(card, placementRule, MapGenerator.rng));
            if (gameObject)
            {
                gameObject.transform.position = gameObject.transform.position + offset;

                PurchaseInteraction purchaseInteraction = gameObject.GetComponent<PurchaseInteraction>();
                if (purchaseInteraction && purchaseInteraction.costType == CostTypeIndex.Money)
                {
                    purchaseInteraction.Networkcost = Run.instance.GetDifficultyScaledCost(purchaseInteraction.cost);
                }

                NetworkServer.Spawn(gameObject);
            }

            return gameObject;
        }
    }
}

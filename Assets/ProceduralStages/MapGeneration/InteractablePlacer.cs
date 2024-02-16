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
            bool orientToFloor = true)
        {
            if (skipSpawnWhenSacrificeArtifactEnabled && RunArtifactManager.instance.IsArtifactEnabled(RoR2Content.Artifacts.sacrificeArtifactDef))
                return null;

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
                if (orientToFloor)
                {
                    var floorNormal = normal ?? graphs.nodeInfoByPosition[gameObject.transform.position].normal;

                    gameObject.transform.rotation = Quaternion.FromToRotation(Vector3.up, floorNormal)
                        * card.prefab.transform.rotation;

                    gameObject.transform.Rotate(floorNormal, MapGenerator.rng.nextNormalizedFloat * 360f, Space.World);
                }

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

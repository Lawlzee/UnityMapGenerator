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
            Xoroshiro128Plus rng,
            string prefab,
            NodeFlags requiredFlags,
            Vector3 offset = default)
        {
            //if (!NetworkServer.active)
            //{
            //    return null;
            //}

            Log.Debug("A");

            var card = ScriptableObject.CreateInstance<SpawnCard>();
            Log.Debug("A");
            card.prefab = Addressables.LoadAssetAsync<GameObject>(prefab).WaitForCompletion();
            Log.Debug("A'");
            card.hullSize = HullClassification.Human;
            Log.Debug("A");
            card.nodeGraphType = MapNodeGroup.GraphType.Ground;
            Log.Debug("A");
            card.requiredFlags = requiredFlags;
            Log.Debug("A''");
            card.forbiddenFlags = NodeFlags.None;
            Log.Debug("A");
            card.directorCreditCost = 0;
            Log.Debug("A");
            card.occupyPosition = true;
            Log.Debug("A'''");
            card.eliteRules = SpawnCard.EliteRules.Default;
            Log.Debug("A");

            DirectorPlacementRule placementRule = new DirectorPlacementRule()
            {
                placementMode = DirectorPlacementRule.PlacementMode.Random
            };
            Log.Debug("A''''");
            GameObject gameObject = DirectorCore.instance.TrySpawnObject(new DirectorSpawnRequest(card, placementRule, rng));
            if (gameObject)
            {
                Log.Debug("A");
                gameObject.transform.position = gameObject.transform.position + offset;
                Log.Debug("A");
                NetworkServer.Spawn(gameObject);
            }

            return gameObject;
        }
    }
}

using Assets.Scripts;
using RoR2;
using RoR2.Navigation;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UIElements;

namespace ProceduralStages
{
    public class NewtPlacer : MonoBehaviour
    {
        public Xoroshiro128Plus rng;

        public void Start()
        {
            var card = ScriptableObject.CreateInstance<SpawnCard>();
            card.prefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/NewtStatue/NewtStatue.prefab").WaitForCompletion();
            card.hullSize = HullClassification.Human;
            card.nodeGraphType = MapNodeGroup.GraphType.Ground;
            card.requiredFlags = NodeFlagsExt.Newt;
            card.forbiddenFlags = NodeFlags.None;
            card.directorCreditCost = 0;
            card.occupyPosition = true;
            card.eliteRules = SpawnCard.EliteRules.Default;

            DirectorPlacementRule placementRule = new DirectorPlacementRule()
            {
                placementMode = DirectorPlacementRule.PlacementMode.Random
            };
            GameObject gameObject = DirectorCore.instance.TrySpawnObject(new DirectorSpawnRequest(card, placementRule, rng));
            if (gameObject)
            {
                gameObject.transform.position = gameObject.transform.position + Vector3.up;
            }
        }
    }
}

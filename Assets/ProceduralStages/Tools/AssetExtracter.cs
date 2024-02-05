using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityObject = UnityEngine.Object;
using ProceduralStages;
using UnityEngine;
using System.IO;
using RoR2;

public class AssetExtracter : MonoBehaviour
{
    [MenuItem("Custom/Find All DirectorCardCategorySelection")]
    static void FindAllDirectorCardCategorySelection()
    {
        var keys = new List<string>
        {
            "RoR2/Base/MixEnemy/dccsMixEnemy.asset",
            "RoR2/Base/Common/dccsNullifiersOnly.asset",
            "RoR2/Base/arena/dccsArenaInteractables.asset",
            "RoR2/Base/arena/dccsArenaInteractablesDLC1.asset",
            "RoR2/Base/arena/dccsArenaMonsters.asset",
            "RoR2/Base/arena/dccsArenaMonstersDLC1.asset",
            "RoR2/Base/artifactworld/dccsArtifactWorldInteractables.asset",
            "RoR2/Base/artifactworld/dccsArtifactWorldInteractablesDLC1.asset",
            "RoR2/Base/artifactworld/dccsArtifactWorldMonsters.asset",
            "RoR2/Base/artifactworld/dccsArtifactWorldMonstersDLC1.asset",
            "RoR2/Base/blackbeach/dccsBlackBeachInteractables.asset",
            "RoR2/Base/blackbeach/dccsBlackBeachInteractablesDLC1.asset",
            "RoR2/Base/blackbeach/dccsBlackBeachMonsters.asset",
            "RoR2/Base/blackbeach/dccsBlackBeachMonstersDLC.asset",
            "RoR2/Base/dampcave/dccsDampCaveInteractables.asset",
            "RoR2/Base/dampcave/dccsDampCaveInteractablesDLC1.asset",
            "RoR2/Base/dampcave/dccsDampCaveMonsters.asset",
            "RoR2/Base/dampcave/dccsDampCaveMonstersDLC1.asset",
            "RoR2/Base/foggyswamp/dccsFoggySwampInteractables.asset",
            "RoR2/Base/foggyswamp/dccsFoggySwampInteractablesDLC1.asset",
            "RoR2/Base/foggyswamp/dccsFoggySwampMonsters.asset",
            "RoR2/Base/foggyswamp/dccsFoggySwampMonstersDLC.asset",
            "RoR2/Base/frozenwall/dccsFrozenWallInteractables.asset",
            "RoR2/Base/frozenwall/dccsFrozenWallInteractablesDLC1.asset",
            "RoR2/Base/frozenwall/dccsFrozenWallMonsters.asset",
            "RoR2/Base/frozenwall/dccsFrozenWallMonstersDLC1.asset",
            "RoR2/Base/goldshores/dccsGoldshoresInteractables.asset",
            "RoR2/Base/goldshores/dccsGoldshoresInteractablesDLC1.asset",
            "RoR2/Base/goldshores/dccsGoldshoresMonsters.asset",
            "RoR2/Base/goldshores/dccsGoldshoresMonstersDLC1.asset",
            "RoR2/Base/golemplains/dccsGolemplainsInteractables.asset",
            "RoR2/Base/golemplains/dccsGolemplainsInteractablesDLC1.asset",
            "RoR2/Base/golemplains/dccsGolemplainsMonsters.asset",
            "RoR2/Base/golemplains/dccsGolemplainsMonstersDLC1.asset",
            "RoR2/Base/goolake/dccsGooLakeInteractables.asset",
            "RoR2/Base/goolake/dccsGooLakeInteractablesDLC1.asset",
            "RoR2/Base/goolake/dccsGooLakeMonsters.asset",
            "RoR2/Base/goolake/dccsGooLakeMonstersDLC1.asset",
            "RoR2/Base/moon/dccsMoonInteractables.asset",
            "RoR2/Base/moon/dccsMoonInteractablesDLC1.asset",
            "RoR2/Base/moon/dccsMoonMonsters.asset",
            "RoR2/Base/moon/dccsMoonMonstersDLC1.asset",
            "RoR2/Base/rootjungle/dccsRootJungleInteractables.asset",
            "RoR2/Base/rootjungle/dccsRootJungleInteractablesDLC1.asset",
            "RoR2/Base/rootjungle/dccsRootJungleMonsters.asset",
            "RoR2/Base/rootjungle/dccsRootJungleMonstersDLC1.asset",
            "RoR2/Base/shipgraveyard/dccsShipgraveyardInteractables.asset",
            "RoR2/Base/shipgraveyard/dccsShipgraveyardInteractablesDLC1.asset",
            "RoR2/Base/shipgraveyard/dccsShipgraveyardMonsters.asset",
            "RoR2/Base/shipgraveyard/dccsShipgraveyardMonstersDLC1.asset",
            "RoR2/Base/skymeadow/dccsSkyMeadowInteractables.asset",
            "RoR2/Base/skymeadow/dccsSkyMeadowInteractablesDLC1.asset",
            "RoR2/Base/skymeadow/dccsSkyMeadowMonsters.asset",
            "RoR2/Base/skymeadow/dccsSkyMeadowMonstersDLC1.asset",
            "RoR2/Base/wispgraveyard/dccsWispGraveyardInteractables.asset",
            "RoR2/Base/wispgraveyard/dccsWispGraveyardInteractablesDLC1.asset",
            "RoR2/Base/wispgraveyard/dccsWispGraveyardMonsters.asset",
            "RoR2/Base/wispgraveyard/dccsWispGraveyardMonstersDLC1.asset",
            "RoR2/DLC1/GameModes/InfiniteTowerRun/InfiniteTowerAssets/dccsInfiniteTowerInteractables.asset",
            "RoR2/DLC1/GameModes/InfiniteTowerRun/InfiniteTowerAssets/dccsITScav.asset",
            "RoR2/DLC1/GameModes/InfiniteTowerRun/InfiniteTowerAssets/dccsITVoidMonsters.asset",
            "RoR2/DLC1/VoidCamp/dccsVoidCampFlavorProps.asset",
            "RoR2/DLC1/VoidCamp/dccsVoidCampInteractables.asset",
            "RoR2/DLC1/VoidCamp/dccsVoidCampMonsters.asset",
            "RoR2/DLC1/ancientloft/dccsAncientLoftInteractablesDLC1.asset",
            "RoR2/DLC1/ancientloft/dccsAncientLoftMonstersDLC1.asset",
            "RoR2/DLC1/itancientloft/dccsITAncientLoftMonsters.asset",
            "RoR2/DLC1/itdampcave/dccsITDampCaveMonsters.asset",
            "RoR2/DLC1/itfrozenwall/dccsITFrozenWallMonsters.asset",
            "RoR2/DLC1/itgolemplains/dccsITGolemplainsMonsters.asset",
            "RoR2/DLC1/itgoolake/dccsITGooLakeMonsters.asset",
            "RoR2/DLC1/itmoon/dccsITMoonMonsters.asset",
            "RoR2/DLC1/itskymeadow/dccsITSkyMeadowMonsters.asset",
            "RoR2/DLC1/snowyforest/dccsSnowyForestInteractablesDLC1.asset",
            "RoR2/DLC1/snowyforest/dccsSnowyForestMonstersDLC1.asset",
            "RoR2/DLC1/sulfurpools/dccsSulfurPoolsInteractablesDLC1.asset",
            "RoR2/DLC1/sulfurpools/dccsSulfurPoolsMonstersDLC1.asset",
            "RoR2/DLC1/voidraid/dccsVoidDonutMonsters.asset",
            "RoR2/DLC1/voidstage/dccsVoidStageInteractables.asset",
            "RoR2/DLC1/voidstage/dccsVoidStageMonsters.asset"
        };

        var pools = DccsPoolItem.All
            .Select(x => new
            {
                Item = x,
                Asset = Addressables.LoadAssetAsync<RoR2.DccsPool>(x.Asset).WaitForCompletion()
            })
            .ToList();

        var mapped = pools
            .Select(x => new DccsPool
            {
                item = x.Item,
                poolCategories = x.Asset.poolCategories
                    .Select(pc => new Category
                    {
                        name = pc.name,
                        categoryWeight = pc.categoryWeight,
                        alwaysIncluded = pc.alwaysIncluded
                            .Select(y => new PoolEntry
                            {
                                dccs = MapDCCS(y),
                                weight = y.weight
                            })
                            .ToArray(),
                        includedIfConditionsMet = pc.includedIfConditionsMet
                            .Select(y => new ConditionalPoolEntry
                            {
                                requiredExpansions = y.requiredExpansions
                                    .Select(z => z.name)
                                    .ToArray(),
                                dccs = MapDCCS(y),
                                weight = y.weight
                            })
                            .ToArray(),
                        includedIfNoConditionsMet = pc.includedIfNoConditionsMet
                            .Select(y => new PoolEntry
                            {
                                dccs = MapDCCS(y),
                                weight = y.weight
                            })
                            .ToArray()
                    })
                    .ToArray()
            })
            .ToArray();

        DCC MapDCCS(RoR2.DccsPool.PoolEntry y)
        {
            var family = y.dccs as FamilyDirectorCardCategorySelection; 


            return new DCC
            {
                name = y.dccs.name,
                selectionChatString = family?.selectionChatString,
                minimumStageCompletion = family?.minimumStageCompletion ?? -1,
                maximumStageCompletion = family?.maximumStageCompletion ?? -1,
                categories = y.dccs.categories
                    .Select(categorie => new Category2
                    {
                        name = categorie.name,
                        selectionWeight = categorie.selectionWeight,
                        cards = categorie.cards
                            .Select(c => new Card
                            {
                                DirectorCard = c.spawnCard?.name,
                                DirectorCardPrefab = c.spawnCard?.prefab?.name,
                                directorCreditCost = c.spawnCard?.directorCreditCost,
                                selectionWeight = c.selectionWeight,
                                spawnDistance = c.spawnDistance.ToString(),
                                minimumStageCompletions = c.minimumStageCompletions,
                                preventOverhead = c.preventOverhead,
                                requiredUnlockable = c.requiredUnlockable,
                                forbiddenUnlockable = c.forbiddenUnlockable,
                                requiredUnlockableDef = c.requiredUnlockableDef?.cachedName,
                                forbiddenUnlockableDef = c.forbiddenUnlockableDef?.cachedName
                            })
                            .ToArray()
                    })
                    .ToArray()
            };
        }

        //List<RoR2.DirectorCardCategorySelection> dccs = keys
        //    .Select(x => Addressables.LoadAssetAsync<RoR2.DirectorCardCategorySelection>(x).WaitForCompletion())
        //    .ToList();
        //
        //var mapped = dccs
        //    .Select(dcc => new DCC
        //    {
        //        name = dcc.name,
        //        categories = dcc.categories
        //            .Select(categorie => new Category
        //            {
        //                name = categorie.name,
        //                selectionWeight = categorie.selectionWeight,
        //                cards = categorie.cards
        //                    .Select(x => new Card
        //                    {
        //                        DirectorCard = x.spawnCard?.name,
        //                        DirectorCardPrefab = x.spawnCard?.prefab?.name,
        //                        directorCreditCost = x.spawnCard?.directorCreditCost,
        //                        selectionWeight = x.selectionWeight,
        //                        spawnDistance = x.spawnDistance.ToString(),
        //                        preventOverhead = x.preventOverhead,
        //                        requiredUnlockable = x.requiredUnlockable,
        //                        forbiddenUnlockable = x.forbiddenUnlockable,
        //                        requiredUnlockableDef = x.requiredUnlockableDef?.cachedName,
        //                        forbiddenUnlockableDef = x.forbiddenUnlockableDef?.cachedName
        //                    })
        //                    .ToArray()
        //            })
        //            .ToArray()
        //    })
        //    .ToArray();

        string json = JsonUtility.ToJson(new DCCS
        {
            dccs = mapped
        });
        File.WriteAllText("E:\\dccs.json", json);
    }

    [Serializable]
    public class PoolEntry
    {
        public DCC dccs;
        [Tooltip("The weight of this entry relative to its siblings")]
        public float weight;
    }

    [Serializable]
    public class ConditionalPoolEntry : PoolEntry
    {
        [Tooltip("ALL expansions in this list must be enabled for this run for this entry to be considered.")]
        public string[] requiredExpansions;
    }

    [Serializable]
    public class Category
    {
        [Tooltip("A name to help identify this category")]
        public string name;
        [Tooltip("The weight of all entries in this category relative to the sibling categories.")]
        public float categoryWeight = 1f;
        [Tooltip("These entries are always considered.")]
        public PoolEntry[] alwaysIncluded;
        [Tooltip("These entries are only considered if their individual conditions are met.")]
        public ConditionalPoolEntry[] includedIfConditionsMet;
        [Tooltip("These entries are considered only if no entries from 'includedIfConditionsMet' have been included.")]
        public PoolEntry[] includedIfNoConditionsMet;
    }

    [Serializable]
    public class DCCS
    {
        public DccsPool[] dccs;
    }

    [Serializable]
    public class DccsPool
    {
        public DccsPoolItem item;
        public Category[] poolCategories;
    }

    [Serializable]
    public class DCC
    {
        public string name;
        public string selectionChatString;
        public int minimumStageCompletion = 1;
        public int maximumStageCompletion = int.MaxValue;
        public Category2[] categories;
    }

    [Serializable]
    public class Category2
    {
        public string name;
        public float selectionWeight;
        public Card[] cards;

    }

    [Serializable]
    public class Card
    {
        public string DirectorCard;
        public string DirectorCardPrefab;
        public float? directorCreditCost;
        public int selectionWeight;
        public string spawnDistance;
        public bool preventOverhead;
        public int minimumStageCompletions;
        public string requiredUnlockable;
        public string forbiddenUnlockable;
        public string requiredUnlockableDef;
        public string forbiddenUnlockableDef;
    }
}

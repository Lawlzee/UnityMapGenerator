using RoR2;
using RoR2.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ProceduralStages
{
    [Serializable]
    public class DccsPoolGenerator
    {
        private static readonly Dictionary<string, string> _monstersVariations = new Dictionary<string, string>
        {
            ["cscTitanBlackBeach"] = "cscTitan",
            ["cscTitanDampCave"] = "cscTitan",
            ["cscTitanGooLake"] = "cscTitan",
            ["cscTitanGolemPlains"] = "cscTitan",
            ["cscGolemNature"] = "cscGolem",
            ["cscGolemSnowy"] = "cscGolem",
            ["cscFlyingVerminSnowy"] = "cscFlyingVermin",
            ["cscVerminSnowy"] = "cscVermin",
        };

        [Range(0, 1)]
        public float blendFactor = 0.5f;

        public DccsPool GenerateMonstersDccs(bool hasDLC1, bool hasDLC2)
        {
            int stageCleared = RunConfig.instance.nextStageClearCount;
            int currentStageInLoop = Application.isEditor
                ? MapGenerator.instance.editorStageInLoop
                : (stageCleared % Run.stagesPerLoop) + 1;

            var validPools = DccsPoolItem.All
                .Where(x => x.StageType == StageType.Regular)
                .Where(x => x.Type == DccsPoolItemType.Monsters)
                .Where(x => hasDLC1 || !x.DLC1)
                .Where(x => hasDLC2 || !x.DLC2)
                .Select(x =>
                {
                    DccsPool pool = Addressables.LoadAssetAsync<DccsPool>(x.Asset).WaitForCompletion();
                    DccsPool.Category standardCategory = pool.poolCategories
                        .Where(y => y.name == "Standard")
                        .First();

                    int stageDistance = Math.Abs(currentStageInLoop - x.StageIndex);
                    float weigth = Mathf.Pow(blendFactor, stageDistance);

                    List<DccsPool.PoolEntry> poolEntries = new List<DccsPool.PoolEntry>();

                    if (standardCategory.alwaysIncluded != null)
                    {
                        poolEntries.AddRange(standardCategory.alwaysIncluded);
                    }

                    if (standardCategory.includedIfNoConditionsMet != null)
                    {
                        poolEntries.AddRange(standardCategory.includedIfNoConditionsMet);
                    }

                    if (standardCategory.includedIfConditionsMet != null)
                    {
                        var validDCCS = standardCategory.includedIfConditionsMet
                            .Where(x => x.requiredExpansions == null || !x.requiredExpansions.Any(x => x?.name == "DLC1") || hasDLC1)
                            .Where(x => x.requiredExpansions == null || !x.requiredExpansions.Any(x => x?.name == "DLC2") || hasDLC2)
                            .ToList();

                        poolEntries.AddRange(validDCCS);
                    }

                    return new StagePool
                    {
                        Info = x,
                        Pool = pool,
                        StandardCategory = standardCategory,
                        PoolEntries = poolEntries,
                        Weigth = weigth
                    };
                })
                .ToList();

            WeightedSelection<StagePool> stageSelection = new WeightedSelection<StagePool>(validPools.Count);
            for (int i = 0; i < validPools.Count; i++)
            {
                var stage = validPools[i];
                stageSelection.AddChoice(stage, stage.Weigth);
            }

            StagePool templateStage = stageSelection.Evaluate(MapGenerator.rng.nextNormalizedFloat);

            var categories = templateStage.PoolEntries
                .SelectMany(x => x.dccs.categories)
                .Select(x => GenerateDccsCategory(x))
                .ToArray();

            DccsPool result = ScriptableObject.CreateInstance<DccsPool>();
            DirectorCardCategorySelection dccs = ScriptableObject.CreateInstance<DirectorCardCategorySelection>();
            dccs.categories = categories;

            result.poolCategories = new DccsPool.Category[]
            {
                new DccsPool.Category
                {
                    name = templateStage.StandardCategory.name,
                    categoryWeight = templateStage.StandardCategory.categoryWeight,
                    alwaysIncluded = new DccsPool.PoolEntry[0],
                    includedIfConditionsMet = new DccsPool.ConditionalPoolEntry[0],
                    includedIfNoConditionsMet = new DccsPool.PoolEntry[]
                    {
                        new DccsPool.PoolEntry
                        {
                            dccs = dccs,
                            weight = templateStage.StandardCategory.categoryWeight,
                        }
                    },
                },
                templateStage.Pool.poolCategories
                    .Where(x => x.name == "Family")
                    .First(),
                templateStage.Pool.poolCategories
                    .Where(x => x.name == "VoidInvasion" || x.name == "VoildInvasion")
                    .First()
            };

            return result;

            DirectorCardCategorySelection.Category GenerateDccsCategory(DirectorCardCategorySelection.Category template)
            {
                var stagesCards = validPools
                    .SelectMany(stage =>
                    {
                        List<DirectorCardCategorySelection.Category> dccsCategories = stage.PoolEntries
                            .SelectMany(x => x.dccs.categories)
                            .Where(x => x.name == template.name)
                            .ToList();

                        return dccsCategories
                            .SelectMany(x => x.cards)
                            .Where(x => x?.spawnCard?.name != null)
                            .Where(x => x.minimumStageCompletions <= stageCleared)
                            .Select(card => new
                            {
                                stage.Info.StageIndex,
                                stage.Weigth,
                                Card = card
                            });
                    })
                    .GroupBy(x => new
                    {
                        x.StageIndex
                    })
                    .Select(grp => new
                    {
                        Weigth = grp.First().Weigth,
                        Cards = grp
                            .Select(x => x.Card)
                            .GroupBy(x => GetMonsterSpecieName(x))
                            .Select(x => x.First())
                            .ToList()
                    })
                    .ToList();

                WeightedSelection<DirectorCard> groundCardsSelection = new WeightedSelection<DirectorCard>();
                WeightedSelection<DirectorCard> airCardsSelection = new WeightedSelection<DirectorCard>();

                foreach (var stage in stagesCards)
                {
                    foreach (var card in stage.Cards)
                    {
                        if (card.spawnCard.nodeGraphType == MapNodeGroup.GraphType.Air)
                        {
                            airCardsSelection.AddChoice(card, stage.Weigth);
                        }
                        else
                        {
                            groundCardsSelection.AddChoice(card, stage.Weigth);
                        }
                    }
                }

                HashSet<string> usedMonsters = new HashSet<string>();
                DirectorCard[] cards = new DirectorCard[template.cards.Length];

                for (int i = 0; i < template.cards.Length;)
                {
                    var cardsSelection = template.cards[i].spawnCard.nodeGraphType == MapNodeGroup.GraphType.Air
                        ? airCardsSelection 
                        : groundCardsSelection;

                    var card = cardsSelection.Evaluate(MapGenerator.rng.nextNormalizedFloat);
                    string monster = GetMonsterSpecieName(card);
                    if (!usedMonsters.Contains(monster))
                    {
                        cards[i] = card;
                        usedMonsters.Add(monster);
                        i++;
                    }
                }

                return new DirectorCardCategorySelection.Category
                {
                    name = template.name,
                    selectionWeight = template.selectionWeight,
                    cards = cards
                };
            }
        }

        private string GetMonsterSpecieName(DirectorCard directorCard)
        {
            return _monstersVariations.TryGetValue(directorCard.spawnCard.name, out string name)
                ? name
                : directorCard.spawnCard.name;
        }

        private class StagePool
        {
            public DccsPoolItem Info;
            public DccsPool Pool;
            public DccsPool.Category StandardCategory;
            public List<DccsPool.PoolEntry> PoolEntries;
            public float Weigth;
        }
    }
}

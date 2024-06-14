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

        public DccsPool GenerateMonstersDccs(bool hasDLC1)
        {
            int stageCleared = Run.instance?.stageClearCount ?? 0;
            int currentStageInLoop = (stageCleared % Run.stagesPerLoop) + 1;

            var validPools = DccsPoolItem.All
                .Where(x => x.StageType == StageType.Regular)
                .Where(x => x.Type == DccsPoolItemType.Monsters)
                .Where(x => hasDLC1 || !x.DLC1)
                .Select(x =>
                {
                    DccsPool pool = Addressables.LoadAssetAsync<DccsPool>(x.Asset).WaitForCompletion();
                    DccsPool.Category standardCategory = pool.poolCategories
                        .Where(y => y.name == "Standard")
                        .First();

                    int stageDistance = Math.Abs(currentStageInLoop - x.StageIndex);
                    float weigth = Mathf.Pow(blendFactor, stageDistance);

                    return new StagePool
                    {
                        Info = x,
                        Pool = pool,
                        StandardCategory = standardCategory,
                        PoolEntry = hasDLC1 && standardCategory.includedIfConditionsMet.Length > 0
                            ? standardCategory.includedIfConditionsMet[0]
                            : standardCategory.includedIfNoConditionsMet[0],
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

            var categories = templateStage.PoolEntry.dccs.categories
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
                    .Where(x => x.name == "VoidInvasion")
                    .First()
            };

            return result;

            DirectorCardCategorySelection.Category GenerateDccsCategory(DirectorCardCategorySelection.Category template)
            {
                var stagesCards = validPools
                    .SelectMany(stage =>
                    {
                        DirectorCardCategorySelection.Category dccsCategory = stage.PoolEntry.dccs.categories
                            .Where(x => x.name == template.name)
                            .FirstOrDefault();

                        return dccsCategory.cards
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
            public DccsPool.PoolEntry PoolEntry;
            public float Weigth;
        }
    }
}

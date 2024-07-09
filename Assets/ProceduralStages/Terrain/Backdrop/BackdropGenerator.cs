using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProceduralStages
{
    [CreateAssetMenu(fileName = "BackdropGenerator", menuName = "ProceduralStages/BackdropGenerator", order = 2)]
    public class BackdropGenerator : ScriptableObject
    {
        public RequiredGenerator[] requiredGenerators;
        public Generator[] generators;
        public IntervalInt count;

        [Serializable]
        public struct Generator
        {
            public BackdropTerrainGenerator value;

            public float weight;
        }

        [Serializable]
        public struct RequiredGenerator
        {
            public BackdropTerrainGenerator value;
            public int count;
        }

        public GameObject[] Generate(
            Material material,
            Texture2D colorGradiant,
            PropsDefinitionCollection propsCollection)
        {
            var rng = MapGenerator.rng;
            int actualCount = rng.RangeInt(count.min, count.max);

            WeightedSelection<BackdropTerrainGenerator> terrainSelection = new WeightedSelection<BackdropTerrainGenerator>(generators.Length);
            for (int i = 0; i < generators.Length; i++)
            {
                var generator = generators[i];
                terrainSelection.AddChoice(generator.value, generator.weight);
            }

            Vector3 mapCenter = 0.5f * MapGenerator.instance.mapScale * (Vector3)MapGenerator.instance.stageSize;

            GameObject[] gameObjects = new GameObject[actualCount];

            int propsIndex = 0;
            for (int i = 0; i < requiredGenerators.Length; i++)
            {
                var generator = requiredGenerators[i];
                for (int j = 0; j < generator.count; j++)
                {
                    if (propsIndex >= actualCount)
                    {
                        break;
                    }

                    ulong seed = rng.nextUlong;

                    BackdropParams args = new BackdropParams
                    {
                        center = mapCenter,
                        colorGradiant = colorGradiant,
                        material = material,
                        propsCollection = propsCollection,
                        seed = seed
                    };

                    gameObjects[propsIndex] = generator.value.Generate(args);
                    propsIndex++;
                }
            }

            for (int i = propsIndex; i < actualCount; i++)
            {
                ulong seed = rng.nextUlong;
                BackdropTerrainGenerator generator = terrainSelection.Evaluate(rng.nextNormalizedFloat);

                BackdropParams args = new BackdropParams
                {
                    center = mapCenter,
                    colorGradiant = colorGradiant,
                    material = material,
                    propsCollection = propsCollection,
                    seed = seed
                };

                gameObjects[i] = generator.Generate(args);
            }

            return gameObjects;
        }
    }
}

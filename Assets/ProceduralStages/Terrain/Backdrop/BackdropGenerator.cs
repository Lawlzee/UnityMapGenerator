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
        public Generator[] generators;
        public IntervalInt count;
        public GameObject backdropTerrainPrefab;
        public int maxPropKind;

        [Serializable]
        public struct Generator
        {
            public BackdropTerrainGenerator value;

            public float weight;
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

            ulong[] seeds = new ulong[actualCount];
            BackdropTerrainGenerator[] selectedGenerators = new BackdropTerrainGenerator[actualCount];

            for (int i = 0; i < actualCount; i++)
            {
                seeds[i] = rng.nextUlong;
                selectedGenerators[i] = terrainSelection.Evaluate(rng.nextNormalizedFloat);
            }

            Vector3 mapCenter = 0.5f * MapGenerator.instance.mapScale * (Vector3)MapGenerator.instance.stageSize;

            GameObject[] gameObjects = new GameObject[actualCount];
            for (int i = 0; i < actualCount; i++)
            {
                var terrain = selectedGenerators[i].Generate(mapCenter, seeds[i], ProfilerLog.Current);
                //backdropTerrains[i] = terrain;

                GameObject gameObject = Instantiate(backdropTerrainPrefab);
                gameObject.transform.position = terrain.position;

                gameObject.GetComponent<MeshFilter>().mesh = terrain.meshResult.mesh;
                gameObject.GetComponent<MeshRenderer>().material = material;

                Graphs graphs = MapGenerator.instance.nodeGraphCreator.CreateBackdropGraphs(terrain);

                MapGenerator.instance.propsPlacer.PlaceAll(
                    terrain.position,
                    graphs,
                    propsCollection,
                    MapGenerator.instance.meshColorer,
                    colorGradiant,
                    material,
                    0,
                    terrain.propsWeigth,
                    bigObjectOnly: true,
                    maxPropKind);

                gameObjects[i] = gameObject;
            }

            return gameObjects;
        }
    }
}

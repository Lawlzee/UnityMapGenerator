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
    public class PropsPlacer
    {
        public PropsDefinition[] props = new PropsDefinition[0];
        public int propsCount = 10;

        public GameObject propsObject;

        [HideInInspector]
        public List<GameObject> instances = new List<GameObject>();


        public void PlaceAll(Graphs graphs, MeshColorer meshColorer, Texture2D colorGradiant, Material terrainMaterial)
        {
            HashSet<int> choosenPropsIndex = new HashSet<int>();

            propsCount = Math.Min(props.Length, propsCount);

            while (choosenPropsIndex.Count < propsCount)
            {
                choosenPropsIndex.Add(MapGenerator.rng.RangeInt(0, props.Length));
            }

            HashSet<int> usedFloorIndexes = new HashSet<int>();
            HashSet<int> usedCeillingIndexes = new HashSet<int>();

            foreach (int j in choosenPropsIndex)
            {
                var prop = props[j];

                Vector3Int colorSeed = new Vector3Int(
                    MapGenerator.rng.RangeInt(0, short.MaxValue),
                    MapGenerator.rng.RangeInt(0, short.MaxValue),
                    MapGenerator.rng.RangeInt(0, short.MaxValue));

                List<PropsNode> graph = prop.ground
                    ? graphs.floorProps
                    : graphs.ceilingProps;

                HashSet<int> usedIndexes = prop.ground
                    ? usedFloorIndexes
                    : usedCeillingIndexes;

                GameObject prefab = Addressables.LoadAssetAsync<GameObject>(prop.asset).WaitForCompletion();

                float? lod = prop.lod < 0
                    ? default(float?)
                    : prop.lod;

                for (int i = 0; i < prop.count; i++)
                {
                    int attempt = 0;
                    int index;
                    do
                    {
                        index = MapGenerator.rng.RangeInt(0, graph.Count);
                        attempt++;
                    }
                    while (usedIndexes.Contains(index) && attempt <= 5);
                    
                    if (attempt > 5)
                    {
                        continue;
                    }

                    usedIndexes.Add(index);

                    var propsNode = graph[index];

                    Color? color = null;
                    if (prop.changeColor)
                    {
                        var uv = meshColorer.GetUV(propsNode.position, propsNode.normal, colorSeed);
                        color = colorGradiant.GetPixelBilinear(uv.x ,uv.y);
                    }

                    Material material = null;
                    if (prop.isRock)
                    {
                        material = terrainMaterial;
                    }

                    GameObject instance = propsNode.Place(
                        prefab, 
                        propsObject, 
                        material, 
                        color, 
                        normal: prop.normal != Vector3.zero
                            ? prop.normal
                            : default(Vector3?),
                        prop.scale);

                    instances.Add(instance);
                }
            }
        }


        [Serializable]
        public class PropsDefinition
        {
            public string asset;
            public float scale = 1;
            public bool ground;
            public int count;
            public bool changeColor;
            public bool isRock;
            public Vector3 normal;

            [Range(-1f, 1f)]
            public float lod = -1;
        }
    }
}

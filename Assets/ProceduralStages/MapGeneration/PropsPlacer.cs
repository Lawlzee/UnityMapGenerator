using RoR2;
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
        //todo:
        //RoR2/DLC1/SulfurPod/SulfurPodBody.prefab
        //RoR2/Base/ExplosivePotDestructible/ExplosivePotDestructibleBody.prefab
        //RoR2/Base/FusionCellDestructible/FusionCellDestructibleBody.prefab
        //RoR2/Base/goolake/GLPressurePlate.prefab

        public PropsDefinition[] props = new PropsDefinition[0];
        public int propsCount = 10;

        public GameObject propsObject;

        [HideInInspector]
        public List<GameObject> instances = new List<GameObject>();


        public void PlaceAll(Graphs graphs, MeshColorer meshColorer, Texture2D colorGradiant, Material terrainMaterial)
        {
            if (Application.isEditor)
            {
                var rows = props
                    .Select(x => new
                    {
                        Prop = x,
                        TriangleCount = Addressables.LoadAssetAsync<GameObject>(x.asset).WaitForCompletion().GetComponentsInChildren<MeshFilter>()
                            .Select(y => y.sharedMesh.triangles.Length)
                            .Sum()
                    })
                    .OrderByDescending(x => x.TriangleCount * x.Prop.count)
                    .ToList();

                StringBuilder sb = new StringBuilder();
                foreach (var x in rows)
                {
                    sb.AppendLine($"{x.Prop.asset}: {x.TriangleCount} * {x.Prop.count} = {x.TriangleCount * x.Prop.count}");
                }

                Log.Debug(sb.ToString());
            }

            int stageInLoop = ((Run.instance?.stageClearCount ?? 0) % Run.stagesPerLoop) + 1;

            HashSet<int> choosenPropsIndex = new HashSet<int>();

            int stagePropCount = Math.Min(props.Length, propsCount + stageInLoop);

            while (choosenPropsIndex.Count < stagePropCount)
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

                    if (prop.occupySpace && prop.ground)
                    {
                        graphs.OccupySpace(propsNode.position);
                    }

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

                    instance.transform.position += prop.offset;

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
            public Vector3 offset;
            public bool occupySpace;

            [Range(-1f, 1f)]
            public float lod = -1;
        }
    }
}

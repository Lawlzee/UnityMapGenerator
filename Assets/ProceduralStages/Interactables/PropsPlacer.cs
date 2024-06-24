using RoR2;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public int propsCount = 10;

        public GameObject propsObject;
        public GameObject occlusionCullingObject;

        [HideInInspector]
        public List<GameObject> instances = new List<GameObject>();


        public void PlaceAll(
            Graphs graphs, 
            PropsDefinitionCollection propsCollection, 
            MeshColorer meshColorer, 
            Texture2D colorGradiant, 
            Material terrainMaterial,
            float ceillingWeight)
        {
            List<PropsDefinition> props = propsCollection.categories
                .SelectMany(x => x.props)
                .ToList();

            WeightedSelection<PropsDefinition> propsSelection = new WeightedSelection<PropsDefinition>();
            foreach (var prop in props)
            {
                if (prop.ground)
                {
                    propsSelection.AddChoice(prop, 1);
                }
                else if (ceillingWeight > 0)
                {
                    propsSelection.AddChoice(prop, ceillingWeight);
                }
            }

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
            int stagePropCount = Math.Min(propsSelection.Count, propsCount + stageInLoop);

            HashSet<int> usedFloorIndexes = new HashSet<int>();
            HashSet<int> usedCeillingIndexes = new HashSet<int>();

            for (int j = 0; j < stagePropCount; j++)
            {
                int propIndex = propsSelection.EvaluateToChoiceIndex(MapGenerator.rng.nextNormalizedFloat);
                var prop = propsSelection.choices[propIndex].value;
                propsSelection.RemoveChoice(propIndex);

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

                for (int i = 0; i < prop.count; i++)
                {
                    if (graph.Count == 0)
                    {
                        continue;
                    }

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

                    if (prop.ground)
                    {
                        graphs.OccupySpace(propsNode.position, prop.isSolid);
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

                    float scale = MapGenerator.rng.RangeFloat(prop.minScale, prop.maxScale);

                    GameObject instance = propsNode.Place(
                        prop.prefab, 
                        propsObject, 
                        material, 
                        color, 
                        normal: prop.normal != Vector3.zero
                            ? prop.normal
                            : default(Vector3?),
                        scale,
                        initialRotation: prop.initialRotation != Vector3.zero
                            ? prop.initialRotation
                            : default(Vector3?));

                    instance.transform.position += prop.offset;

                    if (prop.addCollision)
                    {
                        foreach (var meshRenderer in instance.GetComponentsInChildren<MeshRenderer>(includeInactive: true))
                        {
                            meshRenderer.gameObject.AddComponent<MeshCollider>();
                        }
                    }

                    foreach (var renderer in instance.GetComponentsInChildren<Renderer>(includeInactive: true))
                    {
                        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    }

                    foreach (var collider in instance.GetComponentsInChildren<Collider>(includeInactive: true))
                    {
                        if (collider.gameObject.GetComponent<SurfaceDefProvider>() == null)
                        {
                            SurfaceDefProvider surfaceDefProvider = collider.gameObject.AddComponent<SurfaceDefProvider>();
                            surfaceDefProvider.surfaceDef = prop.surfaceDef;
                        }
                    }

                    if (prop.isSolid)
                    {
                        SetLayer(instance, LayerIndex.world.intVal);
                    }

                    instances.Add(instance);
                }
            }

            occlusionCullingObject.GetComponent<OcclusionCulling>().SetTargets(instances, MapGenerator.instance.mapScale * (Vector3)MapGenerator.instance.stageSize);
        }

        void SetLayer(GameObject gameObject, int layer)
        {
            gameObject.layer = layer;

            foreach (Transform transform in gameObject.transform)
            {
                SetLayer(transform.gameObject, layer);
            }
        }
    }
}

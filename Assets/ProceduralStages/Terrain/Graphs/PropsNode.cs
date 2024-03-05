using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProceduralStages
{
    public struct PropsNode
    {
        public Vector3 position;
        public Vector3 normal;

        public GameObject Place(
            GameObject prefab,
            GameObject parent,
            Material material,
            Color? color,
            Vector3? normal,
            float scale)
        {
            var rotation = Quaternion.FromToRotation(Vector3.up, normal ?? this.normal)
                * prefab.transform.rotation;

            GameObject gameObject = GameObject.Instantiate(prefab, position, rotation, parent.transform);

            SetLayer(gameObject, LayerIndex.world.intVal);

            gameObject.transform.Rotate(normal ?? this.normal, MapGenerator.rng.nextNormalizedFloat * 360f, Space.World);
            gameObject.transform.localScale = new Vector3(scale, scale, scale);

            if (Application.isEditor)
            {
                LODGroup[] lodGroups = gameObject.GetComponentsInChildren<LODGroup>();
                foreach (LODGroup lodGroup in lodGroups)
                {
                    var lods = lodGroup.GetLODs();

                    lods[lods.Length - 1].screenRelativeTransitionHeight = 0;
                    lodGroup.SetLODs(lods);
                }
            }

            if (material != null)
            {
                var meshRenderers = gameObject.transform.GetComponentsInChildren<MeshRenderer>();
                for (int i = 0; i < meshRenderers.Length; i++)
                {
                    meshRenderers[i].material = material;
                }
            }

            if (color.HasValue)
            {
                var meshRenderers = gameObject.transform.GetComponentsInChildren<MeshRenderer>();
                for (int i = 0; i < meshRenderers.Length; i++)
                {
                    foreach (var m in meshRenderers[i].materials)
                    {
                        m.SetColor("_Color", color.Value);
                    }
                }
            }

            //gameObject.transform.Rotate(Vector3.up, MapGenerator.rng.RangeFloat(0.0f, 360f), Space.Self);
            return gameObject;
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

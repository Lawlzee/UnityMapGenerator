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
            float scale,
            Vector3? initialRotation)
        {
            var rotation = Quaternion.FromToRotation(Vector3.up, normal ?? this.normal)
                * (initialRotation != null
                    ? Quaternion.Euler(initialRotation.Value)
                    : prefab.transform.rotation);

            GameObject gameObject = GameObject.Instantiate(prefab, position, rotation, parent.transform);

            gameObject.transform.Rotate(normal ?? this.normal, MapGenerator.rng.nextNormalizedFloat * 360f, Space.World);
            gameObject.transform.localScale = new Vector3(scale, scale, scale);

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
    }
}

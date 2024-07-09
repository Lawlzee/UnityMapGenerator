using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine;

namespace ProceduralStages
{
    [DefaultExecutionOrder(-1)]
    public class RendererMaterialSetter : MonoBehaviour
    {
        public new Renderer renderer;
        public string asset;
        public ShaderValue[] shaderValues;

        public enum ShaderValueType
        {
            Float
        }

        [Serializable]
        public struct ShaderValue
        {
            public string name;
            public ShaderValueType type;
            public float valueFloat;
        }

        public void Awake()
        {
            var actualRender = renderer != null ? renderer : GetComponent<Renderer>();
            actualRender.material = Addressables.LoadAssetAsync<Material>(asset).WaitForCompletion();

            if (shaderValues.Length > 0)
            {
                actualRender.material = new Material(actualRender.material);
            }

            for (int i = 0; i < shaderValues.Length; i++)
            {
                ShaderValue shaderValue = shaderValues[i];
                actualRender.material.SetFloat(shaderValue.name, shaderValue.valueFloat);
            }
        }
    }
}

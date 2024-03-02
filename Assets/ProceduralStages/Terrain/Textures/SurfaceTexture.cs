using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ProceduralStages
{
    [CreateAssetMenu(fileName = "Texture", menuName = "ProceduralStages/Texture", order = 1)]
    public class SurfaceTexture : ScriptableObject
    {
        public string textureAsset;
        public string normalAsset;

        [Range(0, 1)]
        public float bias;
        public Color averageColor;

        public float scale;
        public float bumpScale;

        [Range(0, 2)]
        public float constrast = 1;

        [Range(0, 1)]
        public float glossiness;
        [Range(0, 1)]
        public float metallic;

        private Texture2D _texture;
        public Texture2D texture => _texture 
            ? _texture
            : (_texture = Addressables.LoadAssetAsync<Texture2D>(textureAsset).WaitForCompletion());

        private Texture2D _normal;
        public Texture2D normal => string.IsNullOrEmpty(normalAsset)
            ? null
            : _normal
                ? _normal        
                : (_normal = Addressables.LoadAssetAsync<Texture2D>(normalAsset).WaitForCompletion());
    }
}

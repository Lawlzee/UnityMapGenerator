using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

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
    }
}

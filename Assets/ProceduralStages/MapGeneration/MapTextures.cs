using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProceduralStages
{
    [Serializable]
    public class MapTextures
    {
        public SurfaceTexture[] walls = new SurfaceTexture[0];
        public SurfaceTexture[] floor = new SurfaceTexture[0];

        [Serializable]
        public class SurfaceTexture
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

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.Playables;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace ProceduralStages
{
    [DefaultExecutionOrder(-1)]
    public class PostProcessVolumeProfileSetter : MonoBehaviour
    {
        public PostProcessVolume postProcessVolume;
        public string asset;

        public void Awake()
        {
            postProcessVolume.profile = Addressables.LoadAssetAsync<PostProcessProfile>(asset).WaitForCompletion();
        }
    }
}

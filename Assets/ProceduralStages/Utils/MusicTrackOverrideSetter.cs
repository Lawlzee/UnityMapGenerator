using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine;
using RoR2;

namespace ProceduralStages
{
    [DefaultExecutionOrder(-1)]
    public class MusicTrackOverrideSetter : MonoBehaviour
    {
        public MusicTrackOverride musicTrackOverride;
        public string asset;

        public void Awake()
        {
            musicTrackOverride.track = Addressables.LoadAssetAsync<MusicTrackDef>(asset).WaitForCompletion();
        }
    }
}

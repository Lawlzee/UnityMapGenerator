using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine;
using UnityEngine.Playables;

namespace ProceduralStages
{
    [DefaultExecutionOrder(-1)]
    public class PlayableDirectorSetter : MonoBehaviour
    {
        public PlayableDirector playableDirector;
        public string asset;

        public void Awake()
        {
            playableDirector.playableAsset = Addressables.LoadAssetAsync<PlayableAsset>(asset).WaitForCompletion();
        }
    }
}
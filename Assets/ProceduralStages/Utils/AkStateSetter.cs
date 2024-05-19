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
    public class AkStateSetter : MonoBehaviour
    {
        public AkState AkState;
        public string asset;

        public void Awake()
        {
            AkState.data.WwiseObjectReference = Addressables.LoadAssetAsync<WwiseStateReference>(asset).WaitForCompletion();
        }
    }
}

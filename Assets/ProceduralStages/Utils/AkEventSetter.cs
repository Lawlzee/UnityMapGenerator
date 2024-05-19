using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ProceduralStages
{
    [DefaultExecutionOrder(-1)]
    public class AkEventSetter : MonoBehaviour
    {
        public AkEvent akEvent;
        public string asset;

        public void Awake()
        {
            akEvent.data.WwiseObjectReference = Addressables.LoadAssetAsync<WwiseEventReference>(asset).WaitForCompletion(); 
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.Playables;
using UnityEngine;
using UnityEngine.Networking;

namespace ProceduralStages
{
    [DefaultExecutionOrder(-1)]
    public class NetworkServerSpawn : NetworkBehaviour
    {
        public void Awake()
        {
            if (NetworkServer.active)
            {
                //NetworkServer.Spawn(gameObject);
            }
        }
    }
}
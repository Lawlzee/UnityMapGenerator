using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace ProceduralStages
{
    [DefaultExecutionOrder(-100)]
    public class ServerStarter : MonoBehaviour
    {
        public void Awake()
        {
            Log.Debug("Starting server");
            GetComponent<NetworkManager>().StartHost();
            Log.Debug(NetworkServer.active);
        }
    }
}

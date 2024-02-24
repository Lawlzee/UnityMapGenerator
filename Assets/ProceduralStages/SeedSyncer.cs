using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace ProceduralStages
{
    public class SeedSyncer : NetworkBehaviour
    {
        public static Xoroshiro128Plus randomStageRng;

        [SyncVar]
        public ulong seed;

        public override void OnStartClient()
        {
            Log.Debug($"Client seed initalised: {seed}");
            randomStageRng = new Xoroshiro128Plus(seed);
        }
    }
}

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

        public void Awake()
        {
            //Log.Debug($"Default seed: {seed}");
            //randomStageRng = new Xoroshiro128Plus(seed);
        }

        //public void OnSeedChange(ulong newSeed)
        //{
        //    Log.Debug($"Server seed initalised: {newSeed}");
        //    randomStageRng = new Xoroshiro128Plus(newSeed);
        //}

        public override void OnStartClient()
        {
            Log.Debug($"Client seed initalised: {seed}");
            randomStageRng = new Xoroshiro128Plus(seed);
        }
    }
}

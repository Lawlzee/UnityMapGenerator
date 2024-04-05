using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace ProceduralStages
{
    public class RunConfig : NetworkBehaviour
    {
        public static RunConfig instance;

        [SyncVar]
        public ulong seed;

        [SyncVar]
        public string stageSeed;

        [SyncVar]
        public bool infiniteMapScaling;

        public Xoroshiro128Plus stageRng;

        void Awake()
        {
            Log.Debug($"RunConfig.Awake");
            if (instance != null)
            {
                Destroy(instance);
            }

            DontDestroyOnLoad(this);
            instance = this;
        }
        public override void OnStartClient()
        {
            Log.Debug($"Client seed initalised: {seed}");
            stageRng = new Xoroshiro128Plus(seed);
        }

        void OnDestroy()
        {
            Log.Debug($"RunConfig.OnDestroy");
            instance = null;
            Main.StageSeed.SettingChanged -= StageSeed_SettingChanged;
            Main.InfiniteMapScaling.SettingChanged -= InfiniteMapScaling_SettingChanged;
        }

        public void InitHostConfig(ulong runSeed)
        {
            seed = runSeed;

            stageSeed = Main.StageSeed.Value;
            Main.StageSeed.SettingChanged += StageSeed_SettingChanged;

            infiniteMapScaling = Main.InfiniteMapScaling.Value;
            Main.InfiniteMapScaling.SettingChanged += InfiniteMapScaling_SettingChanged;
        }

        private void StageSeed_SettingChanged(object sender, EventArgs e)
        {
            stageSeed = Main.StageSeed.Value;
        }

        private void InfiniteMapScaling_SettingChanged(object sender, EventArgs e)
        {
            infiniteMapScaling = Main.InfiniteMapScaling.Value;
        }
    }
}

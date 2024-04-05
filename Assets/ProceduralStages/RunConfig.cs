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
    public class RunConfig : NetworkBehaviour
    {
        public static RunConfig instance;

        [SyncVar]
        public ulong seed;

        [SyncVar]
        public string stageSeed;

        [SyncVar]
        public bool infiniteMapScaling;

        [SyncVar]
        private int _selectedTerrainType;

        public TerrainType selectedTerrainType
        {
            get => (TerrainType)_selectedTerrainType;
            set => _selectedTerrainType = (int)value;
        }

        //Keep track of stageClearCount here, because Run.stageClearCount
        //is not synced yet to the client before generating the next stage
        [SyncVar]
        public int nextStageClearCount;

        public Xoroshiro128Plus stageRng;
        public Xoroshiro128Plus seerRng;

        void Awake()
        {
            Log.Debug($"RunConfig.Awake");
            if (instance != null)
            {
                Destroy(instance);
            }

            if (!Application.isEditor)
            {
                DontDestroyOnLoad(this);
            }
            instance = this;
        }
        public override void OnStartClient()
        {
            Log.Debug($"Client seed initalised: {seed}");
            stageRng = new Xoroshiro128Plus(seed);
            seerRng = new Xoroshiro128Plus(stageRng.nextUlong);
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

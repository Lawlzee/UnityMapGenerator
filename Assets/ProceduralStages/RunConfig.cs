﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace ProceduralStages
{
    public struct TerrainTypePercent
    {
        public int StageIndex;
        public TerrainType TerrainType;
        public float Percent;
    }

    public struct ThemePercent
    {
        public Theme Theme;
        public float Percent;
    }

    public struct VanillaThemePercent
    {
        public string Stage;
        public float Percent;
    }

    [Serializable]
    public struct TerrainTypeVisit : IEquatable<TerrainTypeVisit>
    {
        public int stageCount;
        public TerrainType terrainType;

        public bool Equals(TerrainTypeVisit other)
        {
            return stageCount == other.stageCount 
                && terrainType == other.terrainType;
        }
    }

    public class SyncListTerrainTypeVisit : SyncListStruct<TerrainTypeVisit> { }

    [DefaultExecutionOrder(-100)]
    public class RunConfig : NetworkBehaviour
    {
        public static RunConfig instance;

        private bool _isHost;

        [SyncVar]
        public ulong seed;

        [SyncVar]
        public string stageSeed;

        [SyncVar]
        public bool infiniteMapScaling;

        [SyncVar]
        public int moonRequiredPillarsCount;

        [SyncVar]
        private int _selectedTerrainType;

        public TerrainType selectedTerrainType
        {
            get => (TerrainType)_selectedTerrainType;
            set => _selectedTerrainType = (int)value;
        }

        [SyncVar]
        private int _selectedTheme;

        public Theme selectedTheme
        {
            get => (Theme)_selectedTheme;
            set => _selectedTheme = (int)value;
        }

        //Keep track of stageClearCount here, because Run.stageClearCount
        //is not synced yet to the client before generating the next stage
        [SyncVar]
        public int nextStageClearCount;

        [SyncVar]
        public int minStageCount;
        
        [SyncVar]
        private int _terrainRepetition;

        public TerrainRepetition terrainRepetition
        {
            get => (TerrainRepetition)_terrainRepetition;
            set => _terrainRepetition = (int)value;
        }

        public SyncListTerrainTypeVisit terrainTypeVisits;

        private SyncListFloat _terrainTypesPercents;

        public TerrainTypePercent[] terrainTypesPercents
        {
            get
            {
                var result = new TerrainTypePercent[ModConfig.TerrainTypesPercents.Count];
                for (int i = 0; i < ModConfig.TerrainTypesPercents.Count; i++)
                {
                    var config = ModConfig.TerrainTypesPercents[i];
                    ref var resultConfig = ref result[i];

                    resultConfig.StageIndex = config.StageIndex;
                    resultConfig.TerrainType = config.TerrainType;
                    resultConfig.Percent = _terrainTypesPercents[i];
                }

                return result;
            }
        }

        private EventHandler[] _terrainTypesPercentsSettingChanged;

        private SyncListFloat _vanillaStageThemePercents;

        public VanillaThemePercent[] vanillaStageThemePercents
        {
            get
            {
                var result = new VanillaThemePercent[ModConfig.VanillaStageThemePercents.Count];
                for (int i = 0; i < ModConfig.VanillaStageThemePercents.Count; i++)
                {
                    var config = ModConfig.VanillaStageThemePercents[i];
                    ref var resultConfig = ref result[i];

                    resultConfig.Stage = config.Stage.sceneName;
                    resultConfig.Percent = _vanillaStageThemePercents[i];
                }

                return result;
            }
        }

        private EventHandler[] _vanillaStageThemePercentsSettingChanged;

        private SyncListFloat _themePercents;

        public ThemePercent[] themePercents
        {
            get
            {
                var result = new ThemePercent[ModConfig.ThemeConfigs.Count];
                for (int i = 0; i < ModConfig.ThemeConfigs.Count; i++)
                {
                    var config = ModConfig.ThemeConfigs[i];
                    ref var resultConfig = ref result[i];

                    resultConfig.Theme = config.Theme;
                    resultConfig.Percent = _themePercents[i];
                }

                return result;
            }
        }

        private EventHandler[] _themePercentsSettingChanged;

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
            if (_isHost)
            {
                ModConfig.StageSeed.SettingChanged -= StageSeed_SettingChanged;
                ModConfig.InfiniteMapScaling.SettingChanged -= InfiniteMapScaling_SettingChanged;
                ModConfig.MoonRequiredPillarsCount.SettingChanged -= MoonRequiredPillarsCount_SettingChanged;
                ModConfig.MinStageCount.SettingChanged -= MinStageCount_SettingChanged;
                ModConfig.TerrainTypeRepetition.SettingChanged -= TerrainTypeRepetition_SettingChanged;

                for (int i = 0; i < ModConfig.TerrainTypesPercents.Count; i++)
                {
                    var config = ModConfig.TerrainTypesPercents[i];
                    config.Config.SettingChanged -= _terrainTypesPercentsSettingChanged[i];
                }

                for (int i = 0; i < ModConfig.VanillaStageThemePercents.Count; i++)
                {
                    var config = ModConfig.VanillaStageThemePercents[i];
                    config.Config.SettingChanged -= _vanillaStageThemePercentsSettingChanged[i];
                }

                for (int i = 0; i < ModConfig.ThemeConfigs.Count; i++)
                {
                    var config = ModConfig.ThemeConfigs[i];
                    config.Config.SettingChanged -= _themePercentsSettingChanged[i];
                }
            }
        }

        public void InitHostConfig(ulong runSeed)
        {
            _isHost = true;
            seed = runSeed;

            stageSeed = ModConfig.StageSeed.Value;
            ModConfig.StageSeed.SettingChanged += StageSeed_SettingChanged;

            infiniteMapScaling = ModConfig.InfiniteMapScaling.Value;
            ModConfig.InfiniteMapScaling.SettingChanged += InfiniteMapScaling_SettingChanged;

            moonRequiredPillarsCount = ModConfig.MoonRequiredPillarsCount.Value;
            ModConfig.MoonRequiredPillarsCount.SettingChanged += MoonRequiredPillarsCount_SettingChanged;

            minStageCount = ModConfig.MinStageCount.Value;
            ModConfig.MinStageCount.SettingChanged += MinStageCount_SettingChanged;

            terrainRepetition = ModConfig.TerrainTypeRepetition.Value;
            ModConfig.TerrainTypeRepetition.SettingChanged += TerrainTypeRepetition_SettingChanged;

            _terrainTypesPercentsSettingChanged = new EventHandler[ModConfig.TerrainTypesPercents.Count];

            for (int i = 0; i < ModConfig.TerrainTypesPercents.Count; i++)
            {
                var config = ModConfig.TerrainTypesPercents[i];

                _terrainTypesPercents.Add(config.Config.Value);

                int index = i;
                EventHandler settingsChanged = (object o, EventArgs e) =>
                {
                    _terrainTypesPercents[index] = config.Config.Value;
                };

                config.Config.SettingChanged += settingsChanged;
                _terrainTypesPercentsSettingChanged[i] = settingsChanged;
            }

            _vanillaStageThemePercentsSettingChanged = new EventHandler[ModConfig.VanillaStageThemePercents.Count];

            for (int i = 0; i < ModConfig.VanillaStageThemePercents.Count; i++)
            {
                var config = ModConfig.VanillaStageThemePercents[i];

                _vanillaStageThemePercents.Add(config.Config.Value);

                int index = i;
                EventHandler settingsChanged = (object o, EventArgs e) =>
                {
                    _vanillaStageThemePercents[index] = config.Config.Value;
                };

                config.Config.SettingChanged += settingsChanged;
                _vanillaStageThemePercentsSettingChanged[i] = settingsChanged;
            }

            _themePercentsSettingChanged = new EventHandler[ModConfig.ThemeConfigs.Count];

            for (int i = 0; i < ModConfig.ThemeConfigs.Count; i++)
            {
                var config = ModConfig.ThemeConfigs[i];

                _themePercents.Add(config.Config.Value);

                int index = i;
                EventHandler settingsChanged = (object o, EventArgs e) =>
                {
                    _themePercents[index] = config.Config.Value;
                };

                config.Config.SettingChanged += settingsChanged;
                _themePercentsSettingChanged[i] = settingsChanged;
            }
        }

        private void StageSeed_SettingChanged(object sender, EventArgs e)
        {
            stageSeed = ModConfig.StageSeed.Value;
        }

        private void InfiniteMapScaling_SettingChanged(object sender, EventArgs e)
        {
            infiniteMapScaling = ModConfig.InfiniteMapScaling.Value;
        }

        private void MoonRequiredPillarsCount_SettingChanged(object sender, EventArgs e)
        {
            moonRequiredPillarsCount = ModConfig.MoonRequiredPillarsCount.Value;
        }

        private void MinStageCount_SettingChanged(object sender, EventArgs e)
        {
            minStageCount = ModConfig.MinStageCount.Value;
        }

        private void TerrainTypeRepetition_SettingChanged(object sender, EventArgs e)
        {
            terrainRepetition = ModConfig.TerrainTypeRepetition.Value;
        }
    }
}

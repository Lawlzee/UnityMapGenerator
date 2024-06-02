using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace ProceduralStages
{
    [DefaultExecutionOrder(-90)]
    public class RunInitializer : MonoBehaviour
    {
        public GameObject director;

        public void Awake()
        {
            SceneDirector.onPreGeneratePlayerSpawnPointsServer += SceneDirector_onPreGeneratePlayerSpawnPointsServer;
            CombatDirector.eliteTiers = new CombatDirector.EliteTierDef[]
            {
                new CombatDirector.EliteTierDef()
                {
                    isAvailable = _ => true,
                    availableDefs = new List<EliteDef>(),
                    costMultiplier = 0,
                    eliteTypes = new EliteDef[0],
                    canSelectWithoutAvailableEliteDef = true
                }
            };

            //On.RoR2.RuleBook.ctor += RuleBook_ctor;
            RuleBook.defaultValues = new byte[1000];

            BuffCatalog.buffDefs = new BuffDef[0];
            PlayerCharacterMasterController._instancesReadOnly = new List<PlayerCharacterMasterController>() { null }.AsReadOnly();
            RoR2Content.Artifacts.Sacrifice = Addressables.LoadAssetAsync<ArtifactDef>("RoR2/Base/Sacrifice/Sacrifice.asset").WaitForCompletion();
            RoR2Content.Artifacts.SingleMonsterType = RoR2Content.Artifacts.Sacrifice;
            RoR2Content.Artifacts.MixEnemy = RoR2Content.Artifacts.Sacrifice;
            RoR2Content.Artifacts.EliteOnly = RoR2Content.Artifacts.Sacrifice;
            CU8Content.Artifacts.Devotion = RoR2Content.Artifacts.Sacrifice;

            DebugRun debugRun = gameObject.AddComponent<DebugRun>();
            debugRun.runRNG = new Xoroshiro128Plus(debugRun.seed);
            debugRun.nextStageRng = new Xoroshiro128Plus(debugRun.runRNG.nextUlong);
            debugRun.stageRngGenerator = new Xoroshiro128Plus(debugRun.runRNG.nextUlong);
            debugRun.GenerateStageRNG();

            //debugRun.stageClearCount = 0;

            //Instantiate(Addressables.LoadAssetAsync<GameObject>("RoR2/Base/ClassicRun/ClassicRun.prefab").WaitForCompletion());
            //Run.cvRunSceneOverride.value = "";
            //Run.instance.startingSceneGroup = null;
            //Run.instance.startingScenes = new SceneDef[0];

            Run.instance.Start();
            if (Run.instance)
            {
                Log.Debug("Instance found");
                if (Run.instance.runRNG != null)
                {
                    Log.Debug("Run.instance.runRNG");
                }
            }
            Log.Debug("Run initialized");
            //Run

            NetworkServer.Spawn(gameObject);
        }

        private void SceneDirector_onPreGeneratePlayerSpawnPointsServer(SceneDirector sceneDirector, ref Action generationMethod)
        {
            generationMethod = null;
        }

        //private void RuleBook_ctor(On.RoR2.RuleBook.orig_ctor orig, RoR2.RuleBook self)
        //{
        //    
        //}

        public void Start()
        {
            director.SetActive(true);
            NetworkServer.Spawn(director);
        }

        private class DebugRun : Run
        {
            public override void Start()
            {
                
            }

            //public override bool autoGenerateSpawnPoints => true;

            public override bool ShouldUpdateRunStopwatch()
            {
                return true;
            }
        }
    }
}

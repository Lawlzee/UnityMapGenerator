using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace ProceduralStages
{
    public static class ConfigHooks
    {
        public static void Init()
        {
            On.RoR2.PreGameController.Awake += PreGameController_Awake;
            Stage.onStageStartGlobal += Stage_onStageStartGlobal;

            Run.onRunDestroyGlobal += _ =>
            {
                if (RunConfig.instance != null)
                {
                    UnityEngine.Object.Destroy(RunConfig.instance);
                }
            };
        }

        private static void PreGameController_Awake(On.RoR2.PreGameController.orig_Awake orig, PreGameController self)
        {
            orig(self);

            if (NetworkServer.active)
            {
                var runConfig = UnityEngine.Object.Instantiate(ContentProvider.runConfigPrefab);
                runConfig.GetComponent<RunConfig>().InitHostConfig(self.runSeed);
                NetworkServer.Spawn(runConfig);
            }
        }

        private static void Stage_onStageStartGlobal(Stage obj)
        {
            if (NetworkServer.active)
            {
                int nextStageClearCount = Run.instance.stageClearCount;
                if (SceneCatalog.GetSceneDefForCurrentScene().sceneType == SceneType.Stage)
                {
                    nextStageClearCount++;
                }
                RunConfig.instance.nextStageClearCount = nextStageClearCount;
            }
        }
    }
}

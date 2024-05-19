using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine;
using RoR2;
using UnityEngine.Events;
using EntityStates;
using RoR2.EntityLogic;
using RoR2.Networking;
using UnityEngine.Networking;

namespace ProceduralStages
{
    public class MoonEscapeSequence : NetworkBehaviour
    {
        public GameObject dropshipZone;

        public void Start()
        {
            var escapeSequenceObjects = transform.Find("EscapeSequenceObjects").gameObject;

            if (NetworkServer.active)
            {
                var globalEventMethodLibrary = Addressables.LoadAssetAsync<GlobalEventMethodLibrary>("RoR2/Base/Core/GlobalEventMethodLibrary.asset").WaitForCompletion();
                var mainEnding = Addressables.LoadAssetAsync<GameEndingDef>("RoR2/Base/ClassicRun/MainEnding.asset").WaitForCompletion();
                var escapeSequenceFailed = Addressables.LoadAssetAsync<GameEndingDef>("RoR2/Base/ClassicRun/EscapeSequenceFailed.asset").WaitForCompletion();

                EscapeSequenceController escapeSequenceController = GetComponent<EscapeSequenceController>();

                escapeSequenceController.onCompleteEscapeSequenceServer.AddListener(() => globalEventMethodLibrary.RunBeginGameOverServer(mainEnding));
                escapeSequenceController.onFailEscapeSequenceServer.AddListener(() => globalEventMethodLibrary.RunBeginGameOverServer(escapeSequenceFailed));

                GameObject brotherHauntMasterPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/BrotherHaunt/BrotherHauntMaster.prefab").WaitForCompletion();
                var brotherHauntMaster = Instantiate(brotherHauntMasterPrefab, transform);
                brotherHauntMaster.SetActive(false);

                GameObject freeDropship = new GameObject();
                freeDropship.transform.parent = escapeSequenceObjects.transform;
                OnEnableEvent onEnableEvent = freeDropship.AddComponent<OnEnableEvent>();
                onEnableEvent.action = new UnityEvent();
                onEnableEvent.action.AddListener(() => globalEventMethodLibrary.ActivateGameObjectIfServer(dropshipZone.transform.Find("States").Find("Escape").gameObject));

                GameObject frogSpawner = new GameObject("FrogSpawner");
                frogSpawner.transform.parent = escapeSequenceObjects.transform;
                frogSpawner.transform.position = new Vector3(1105.95f, -283.0119f, 1182.11f);
                GenericSceneSpawnPoint frogSpawnPoint = frogSpawner.AddComponent<GenericSceneSpawnPoint>();
                frogSpawnPoint.networkedObjectPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/moon/FrogInteractable.prefab").WaitForCompletion();

                GameObject spawnMasters = new GameObject("SpawnMasters");
                spawnMasters.transform.parent = escapeSequenceObjects.transform;
                StartEvent startEvent = spawnMasters.AddComponent<StartEvent>();
                startEvent.action = new UnityEvent();
                startEvent.action.AddListener(() => brotherHauntMaster.GetComponent<CharacterMaster>().SpawnBodyHere());
            }

            //RunningOutOfTimePostProcess
            //VoidReaverDirector
            //LunarMonsterDirector
            //LowRumbleShakeEmitter
            //TiltEmitter
            //TiltEmitter
            //Post-Process
            //Rock Particles, Fast
            //Mega Glows
            //SpawnMasters
            GameObject musicObject = new GameObject("Music");
            musicObject.transform.parent = escapeSequenceObjects.transform;
            MusicTrackOverride musicTrackOverride = musicObject.AddComponent<MusicTrackOverride>();
            musicTrackOverride.track = Addressables.LoadAssetAsync<MusicTrackDef>("RoR2/Base/Common/muEscape.asset").WaitForCompletion();
            musicTrackOverride.priority = 3;

            //FreeDropship
            //ExitOrbHolders
        }
    }
}

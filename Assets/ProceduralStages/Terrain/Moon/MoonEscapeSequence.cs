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
        public Vector3 frogPosition;

        public void Start()
        {
            var escapeSequenceObjects = transform.Find("EscapeSequenceObjects").gameObject;

            if (NetworkServer.active && MapGenerator.instance.stageType == StageType.Moon)
            {
                var globalEventMethodLibrary = Addressables.LoadAssetAsync<GlobalEventMethodLibrary>("RoR2/Base/Core/GlobalEventMethodLibrary.asset").WaitForCompletion();
                var mainEnding = Addressables.LoadAssetAsync<GameEndingDef>("RoR2/Base/ClassicRun/MainEnding.asset").WaitForCompletion();
                var escapeSequenceFailed = Addressables.LoadAssetAsync<GameEndingDef>("RoR2/Base/ClassicRun/EscapeSequenceFailed.asset").WaitForCompletion();

                EscapeSequenceController escapeSequenceController = GetComponent<EscapeSequenceController>();

                escapeSequenceController.onCompleteEscapeSequenceServer.AddListener(() => globalEventMethodLibrary.RunBeginGameOverServer(mainEnding));
                escapeSequenceController.onFailEscapeSequenceServer.AddListener(() => globalEventMethodLibrary.RunBeginGameOverServer(escapeSequenceFailed));
                escapeSequenceController.scheduledEvents[1].onEnter.AddListener(() => dropshipZone.transform.Find("StragglerKiller").gameObject.SetActive(true));
                escapeSequenceController.scheduledEvents[1].onExit.AddListener(() => dropshipZone.transform.Find("StragglerKiller").gameObject.SetActive(false));

                GameObject freeDropship = new GameObject("FreeDropship");
                freeDropship.transform.parent = escapeSequenceObjects.transform;
                OnEnableEvent onEnableEvent = freeDropship.AddComponent<OnEnableEvent>();
                onEnableEvent.action = new UnityEvent();
                onEnableEvent.action.AddListener(() => globalEventMethodLibrary.ActivateGameObjectIfServer(dropshipZone.transform.Find("States").Find("Escape").gameObject));

                GameObject frogSpawner = new GameObject("FrogSpawner");
                frogSpawner.transform.parent = escapeSequenceObjects.transform;
                frogSpawner.transform.position = frogPosition;
                GenericSceneSpawnPoint frogSpawnPoint = frogSpawner.AddComponent<GenericSceneSpawnPoint>();
                frogSpawnPoint.networkedObjectPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/moon/FrogInteractable.prefab").WaitForCompletion();

                var brotherHauntMaster = transform.Find("BrotherHauntMaster").gameObject;
                var brotherHauntCharacterMaster = brotherHauntMaster.GetComponent<CharacterMaster>();

                var spawnMasters = new GameObject("SpawnMasters");
                spawnMasters.transform.parent = escapeSequenceObjects.transform;
                StartEvent startEvent = spawnMasters.AddComponent<StartEvent>();
                startEvent.action = new UnityEvent();
                startEvent.action.AddListener(brotherHauntCharacterMaster.SpawnBodyHere);

                dropshipZone.transform.Find("States").Find("EscapeComplete").Find("ServerLogic").gameObject.GetComponent<OnEnableEvent>().action.AddListener(() => escapeSequenceController.CompleteEscapeSequence());
            }

            //Rock Particles, Fast
            //Mega Glows
        }
    }
}

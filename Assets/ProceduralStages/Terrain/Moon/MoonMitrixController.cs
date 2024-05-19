using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;
using RoR2;
using EntityStates;
using EntityStates.Missions.BrotherEncounter;
using UnityEngine.Networking;

namespace ProceduralStages
{
    public class MoonMitrixController : NetworkBehaviour
    {
        public void Awake()
        {
            if (NetworkServer.active)
            {
                NetworkServer.Spawn(gameObject);
            }

            //var centerOrbSound = Addressables.LoadAssetAsync<GameObject>("Wwise/E02BAD62-435B-4950-9187-0A0C9822A4C9.asset").WaitForCompletion();
            //
            //
            //var preEncounterPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/moon/EntityStates.Missions.BrotherEncounter.PreEncounter.asset").WaitForCompletion();
            //var phase2Prefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/moon/EntityStates.Missions.BrotherEncounter.Phase2.asset").WaitForCompletion();
            //var phase3Prefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/moon/EntityStates.Missions.BrotherEncounter.Phase3.asset").WaitForCompletion();
            //var phase4Prefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/moon/EntityStates.Missions.BrotherEncounter.Phase4.asset").WaitForCompletion();
            //var pillarPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/moon/MoonArenaDynamicPillar.prefab").WaitForCompletion();

            //Inst
            /*
            RoR2/Base/moon/EntityStates.Missions.BrotherEncounter.PreEncounter.asset
RoR2/Base/moon/EntityStates.Missions.BrotherEncounter.Phase1.asset
RoR2/Base/moon/EntityStates.Missions.BrotherEncounter.Phase2.asset
RoR2/Base/moon/EntityStates.Missions.BrotherEncounter.Phase3.asset
RoR2/Base/moon/EntityStates.Missions.BrotherEncounter.Phase4.asset
RoR2/Base/moon/MoonArenaDynamicPillar.prefab


            */
        }



        //public static GameObject Place()
        //{
        //    GameObject controllerObject = new GameObject("BrotherMissionController");
        //    controllerObject.transform.position = new Vector3(82.2f, 32.6006f, -215.1f);
        //    controllerObject.transform.localScale = new Vector3(1.542426f, 1.542426f, 1.542426f);
        //
        //    EntityStateMachine stateMachine = controllerObject.AddComponent<EntityStateMachine>();
        //    stateMachine.initialStateType = new SerializableEntityStateType(typeof(Idle));
        //    stateMachine.mainStateType = new SerializableEntityStateType(typeof(PreEncounter));
        //
        //    controllerObject.AddComponent<NetworkIdentity>();
        //
        //    controllerObject.
        //}
    }
}

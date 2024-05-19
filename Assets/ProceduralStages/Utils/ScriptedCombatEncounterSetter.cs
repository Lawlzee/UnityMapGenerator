using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine;
using RoR2;

namespace ProceduralStages
{
    [DefaultExecutionOrder(-1)]
    public class ScriptedCombatEncounterSetter : MonoBehaviour
    {
        public ScriptedCombatEncounter scriptedCombatEncounter;
        public string[] spawnCards;

        public void Awake()
        {
            for (int i = 0; i < spawnCards.Length; i++)
            {
                scriptedCombatEncounter.spawns[i].spawnCard = Addressables.LoadAssetAsync<SpawnCard>(spawnCards[i]).WaitForCompletion();
            }
        }
    }
}

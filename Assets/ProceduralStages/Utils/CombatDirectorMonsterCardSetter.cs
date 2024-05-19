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
    public class CombatDirectorMonsterCardSetter : MonoBehaviour
    {
        public CombatDirector combatDirector;
        public string asset;

        public void Awake()
        {
            combatDirector.monsterCards = Addressables.LoadAssetAsync<DirectorCardCategorySelection>(asset).WaitForCompletion();
        }
    }
}

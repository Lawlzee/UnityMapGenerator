using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProceduralStages
{
    public class ArenaTriggerEvent : MonoBehaviour
    {
        public void Awake()
        {
            AllPlayersTrigger playersTrigger = GetComponent<AllPlayersTrigger>();

            playersTrigger.onTriggerStart.AddListener(() => MapGenerator.instance.directorObject.GetComponent<CombatDirector>().enabled = false);
            playersTrigger.onTriggerStart.AddListener(() => gameObject.SetActive(false));
        }
    }
}

using RoR2;
using RoR2.UI;
using RoR2.UI.SkinControllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ProceduralStages
{
    public static class PauseMenu
    {
        public static void Init()
        {
            On.RoR2.UI.PauseScreenController.Awake += (orig, self) =>
            {
                orig(self);
                if (SceneCatalog.currentSceneDef?.cachedName == Main.SceneName)
                {
                    AddTeleportButton(self);
                }
            };
        }

        private static void AddTeleportButton(PauseScreenController pauseScreenController)
        {
            GameObject buttonPrefab = pauseScreenController.GetComponentInChildren<ButtonSkinController>().gameObject;
            GameObject respawnButton = UnityEngine.Object.Instantiate<GameObject>(buttonPrefab, buttonPrefab.transform.parent);
            respawnButton.name = "TeleportButton";
            //todo: add config
            //respawnButton.transform.SetSiblingIndex(1);
            respawnButton.transform.SetAsLastSibling();
            respawnButton.GetComponent<ButtonSkinController>().GetComponent<LanguageTextMeshController>().token = "Teleport To Playable Area";
            HGButton button = respawnButton.GetComponent<HGButton>();
            button.interactable = true;

            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            {
                button.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);
            }

            button.onClick.AddListener(() =>
            {
                pauseScreenController.gameObject.SetActive(false);

                foreach (CharacterBody characterBody in CharacterBody.instancesList.Where(x => x.hasEffectiveAuthority))
                {
                    Vector3 teleportPosition = Run.instance.FindSafeTeleportPosition(characterBody, null, 0, 0);
                    Log.Debug("Teleported to Playable Area");
                    TeleportHelper.TeleportBody(characterBody, teleportPosition);
                    GameObject effectPrefab = Run.instance.GetTeleportEffectPrefab(characterBody.gameObject);

                    if (effectPrefab)
                    {
                        EffectManager.SimpleEffect(effectPrefab, teleportPosition, Quaternion.identity, true);
                    }
                }

                pauseScreenController.gameObject.SetActive(false);
            });
        }
    }
}

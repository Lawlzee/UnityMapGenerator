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
            On.RoR2.CameraRigController.OnEnable += (orig, self) =>
            {
                Log.Debug("On.RoR2.CameraRigController.OnEnable");
                orig(self);
            };

            On.RoR2.CameraRigController.OnDisable += (orig, self) =>
            {
                Log.Debug("On.RoR2.CameraRigController.OnDisable");
                orig(self);
            };

            On.RoR2.UI.PauseScreenController.Awake += (orig, self) =>
            {
                orig(self);
                //GameObject gameObject1 = self.GetComponentInChildren<ButtonSkinController>().gameObject;
                //GameObject gameObject2 = UnityEngine.Object.Instantiate<GameObject>(gameObject1, gameObject1.transform.parent);
                //gameObject2.name = "GenericMenuButton (Photo mode)";
                //gameObject2.SetActive(true);
                //gameObject2.GetComponent<ButtonSkinController>().GetComponent<LanguageTextMeshController>().token = "Photo mode";
                //HGButton component = gameObject2.GetComponent<HGButton>();
                //component.interactable = this.cameraRigController.localUserViewer != null;
                //component.onClick.AddListener((UnityAction)(() => new GameObject("PhotoModeController").AddComponent<PhotoModeController>().EnterPhotoMode(pauseScreenController, this.cameraRigController)));
                //gameObject2.transform.SetSiblingIndex(PhotoModePlugin.buttonPlacement.Value);

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

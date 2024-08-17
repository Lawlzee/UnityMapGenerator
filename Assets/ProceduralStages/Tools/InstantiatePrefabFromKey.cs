using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ProceduralStages
{
#if UNITY_EDITOR
    public class InstantiatePrefabFromKey : EditorWindow
    {
        private string assetKey = "";

        [MenuItem("Tools/Instantiate Prefab from Key")]
        private static void InstantiatePrefab()
        {
            GetWindow<InstantiatePrefabFromKey>("Text Modal");
        }

        private void OnGUI()
        {
            assetKey = EditorGUILayout.TextField("Asset key", assetKey);

            if (GUILayout.Button("Submit"))
            {
                // Check if the user entered a key
                if (string.IsNullOrEmpty(assetKey))
                {
                    Debug.LogWarning("No asset key entered.");
                    return;
                }

                // Load the prefab using the asset key
                GameObject prefab = Addressables.LoadAssetAsync<GameObject>(assetKey).WaitForCompletion();

                // Check if the prefab was loaded successfully
                if (prefab == null)
                {
                    Debug.LogError($"No prefab found at asset key: {assetKey}");
                    return;
                }

                // Instantiate the prefab in the scene
                GameObject instantiatedPrefab = UnityEngine.Object.Instantiate(prefab) as GameObject;

                // If instantiation was successful, select the instantiated prefab
                if (instantiatedPrefab != null)
                {
                    Selection.activeGameObject = instantiatedPrefab;
                    Undo.RegisterCreatedObjectUndo(instantiatedPrefab, "Instantiate Prefab");
                }
                Close();
            }

            if (GUILayout.Button("Cancel"))
            {
                Close();
            }
        }
    }
#endif
}

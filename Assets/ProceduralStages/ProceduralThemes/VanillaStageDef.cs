using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProceduralStages
{
    [CreateAssetMenu(fileName = "VanillaStageDef", menuName = "ProceduralStages/VanillaStageDef", order = 1)]
    public class VanillaStageDef : ScriptableObject
    {
        public string sceneName;
        public string[] gameObjectsToDisable;
        public TerrainRendererDef[] terrainRenderers;

        public void DisableProps()
        {
            foreach (string path in gameObjectsToDisable)
            {
                foreach (Transform tranform in FindMany(path))
                {
                    tranform.gameObject.SetActive(false);
                }
            }
        }

        public void ApplyTerrainMaterial(Material terrainMaterial, MaterialInfo materialInfo)
        {
            SurfaceDef surfaceDef = materialInfo.floorTexture.surfaceDef;

            foreach (TerrainRendererDef terrainRendererDef in terrainRenderers)
            {
                foreach (Transform tranform in FindMany(terrainRendererDef.path))
                {
                    Renderer renderer = tranform.GetComponent<Renderer>();
                    renderer.material = new Material(terrainMaterial);

                    materialInfo.ApplyTo(renderer.material, terrainRendererDef.useUV);

                    SurfaceDefProvider surfaceDefProvider = tranform.GetComponent<SurfaceDefProvider>();
                    if (surfaceDef != null && surfaceDefProvider != null)
                    {
                        surfaceDefProvider.surfaceDef = surfaceDef;
                    }
                }
            }
        }

        private IEnumerable<Transform> FindMany(string path)
        {
            GameObject gameObject = GameObject.Find(path);
            if (gameObject != null)
            {
                var parent = gameObject.transform.parent;
                if (parent == null)
                {
                    GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();

                    for (int i = 0; i < rootObjects.Length; i++)
                    {
                        GameObject child = rootObjects[i];
                        if (child.name == gameObject.name)
                        {
                            yield return child.transform;
                        }
                    }
                }
                else
                {
                    int siblingCount = gameObject.transform.parent.childCount;

                    for (int i = 0; i < siblingCount; i++)
                    {
                        Transform child = parent.GetChild(i);
                        if (child.name == gameObject.name)
                        {
                            yield return child;
                        }
                    }
                }
                
            }
            else
            {
                Log.Warning($"GameObject {path} not found");
            }
        }
    }
}

using System.Linq;
using UnityEngine;

namespace ProceduralStages
{
    [CreateAssetMenu(fileName = "MapThemeCollection", menuName = "ProceduralStages/MapThemeCollection", order = 1)]
    public class MapThemeCollection : ScriptableObject
    {
        public MapTheme[] themes;

        //Loading assets seems to corrupt the terrain Texture2d, so GetPixelBilinear doesn't work.
        //Preload everything to fix this issue.
        public void WarmUp()
        {
            foreach (MapTheme theme in themes)
            {
                foreach (SurfaceTexture surfaceTexture in theme.walls.Concat(theme.floor).Concat(theme.detail))
                {
                    _ = surfaceTexture.texture;
                    _ = surfaceTexture.surfaceDef;
                    _ = surfaceTexture.normal;
                }

                foreach (SkyboxDef skyboxDef in theme.skyboxes)
                {
                    _ = skyboxDef.material;
                }

                foreach (WaterDef waterDef in theme.waters)
                {
                    _ = waterDef.material;
                }

                foreach (PropsDefinitionCollection propsCollection in theme.propCollections)
                {
                    foreach (PropsDefinitionCategory category in propsCollection.categories)
                    {
                        foreach (PropsDefinition prop in category.props)
                        {
                            _ = prop.prefab;
                            _ = prop.surfaceDef;
                        }
                    }
                }
            }
        }
    }
}
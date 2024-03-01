using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ProceduralStages
{
    [CreateAssetMenu(fileName = "theme", menuName = "ProceduralStages/Theme", order = 1)]
    public class MapTheme : ScriptableObject
    {
        public SurfaceTexture[] walls = new SurfaceTexture[0];
        public SurfaceTexture[] floor = new SurfaceTexture[0];
        public ThemeColorPalettes[] colorPalettes;
        public PropsDefinitionCollection[] propCollections = new PropsDefinitionCollection[0];

        public Texture2D ApplyTheme(Material material, RampFog fog, SurfaceDef surface)
        {
            ThemeColorPalettes colorPalette = colorPalettes[MapGenerator.rng.RangeInt(0, colorPalettes.Length)];
            Texture2D colorGradiant = SetTexture(material, colorPalette);
            surface.approximateColor = colorPalette.AverageColor(colorGradiant);

            float sunHue = MapGenerator.rng.nextNormalizedFloat;
            RenderSettings.sun.color = Color.HSVToRGB(sunHue, colorPalette.light.saturation, colorPalette.light.value);

            SetFog(fog, sunHue, colorPalette);

            return colorGradiant;
        }

        private Texture2D SetTexture(Material material, ThemeColorPalettes colorPalette)
        {
            var rng = MapGenerator.rng;

            Texture2D colorGradiant = colorPalette.CreateTexture();
            material.SetTexture("_ColorTex", colorGradiant);

            int floorIndex = rng.RangeInt(0, floor.Length);
            int wallIndex = rng.RangeInt(0, walls.Length);

            SurfaceTexture floorTexture = floor[floorIndex];
            SurfaceTexture wallTexture = walls[wallIndex];

            material.mainTexture = Addressables.LoadAssetAsync<Texture2D>(wallTexture.textureAsset).WaitForCompletion();
            if (string.IsNullOrEmpty(wallTexture.normalAsset))
            {
                material.SetTexture("_WallNormalTex", null);
            }
            else
            {
                material.SetTexture("_WallNormalTex", Addressables.LoadAssetAsync<Texture2D>(wallTexture.normalAsset).WaitForCompletion());
            }
            material.SetFloat("_WallBias", wallTexture.bias);
            material.SetColor("_WallColor", wallTexture.averageColor);
            material.SetFloat("_WallScale", wallTexture.scale);
            material.SetFloat("_WallBumpScale", wallTexture.bumpScale);
            material.SetFloat("_WallContrast", wallTexture.constrast);
            material.SetFloat("_WallGlossiness", wallTexture.glossiness);
            material.SetFloat("_WallMetallic", wallTexture.metallic);

            material.SetTexture("_FloorTex", Addressables.LoadAssetAsync<Texture2D>(floorTexture.textureAsset).WaitForCompletion());
            if (string.IsNullOrEmpty(floorTexture.normalAsset))
            {
                material.SetTexture("_FloorNormalTex", null);
            }
            else
            {
                material.SetTexture("_FloorNormalTex", Addressables.LoadAssetAsync<Texture2D>(floorTexture.normalAsset).WaitForCompletion());
            }
            material.SetFloat("_FloorBias", floorTexture.bias);
            material.SetColor("_FloorColor", floorTexture.averageColor);
            material.SetFloat("_FloorScale", floorTexture.scale);
            material.SetFloat("_FloorBumpScale", floorTexture.bumpScale);
            material.SetFloat("_FloorContrast", floorTexture.constrast);
            material.SetFloat("_FloorGlossiness", floorTexture.glossiness);
            material.SetFloat("_FloorMetallic", floorTexture.metallic);

            return colorGradiant;
        }



        private void SetFog(RampFog fog, float sunHue, ThemeColorPalettes colorPalette)
        {
            

            var fogColor = Color.HSVToRGB(sunHue, colorPalette.fog.saturation, colorPalette.fog.value);

            fog.fogColorStart.value = fogColor;
            fog.fogColorStart.value.a = colorPalette.fog.colorStartAlpha;
            fog.fogColorMid.value = fogColor;
            fog.fogColorMid.value.a = colorPalette.fog.colorMidAlpha;
            fog.fogColorEnd.value = fogColor;
            fog.fogColorEnd.value.a = colorPalette.fog.colorEndAlpha;
            fog.fogZero.value = colorPalette.fog.zero;
            fog.fogOne.value = colorPalette.fog.one;

            fog.fogIntensity.value = colorPalette.fog.intensity;
            fog.fogPower.value = colorPalette.fog.power;
        }
    }
}

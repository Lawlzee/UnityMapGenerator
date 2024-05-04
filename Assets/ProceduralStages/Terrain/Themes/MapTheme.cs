using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Rendering.PostProcessing;

namespace ProceduralStages
{
    [CreateAssetMenu(fileName = "theme", menuName = "ProceduralStages/Theme", order = 1)]
    public class MapTheme : ScriptableObject
    {
        public Theme Theme;
        public SurfaceTexture[] walls = new SurfaceTexture[0];
        public SurfaceTexture[] floor = new SurfaceTexture[0];
        public ThemeColorPalettes[] colorPalettes;
        public SkyboxDef[] skyboxes = new SkyboxDef[0];
        public WaterDef[] waters = new WaterDef[0];
        public PropsDefinitionCollection[] propCollections = new PropsDefinitionCollection[0];

        public Texture2D ApplyTheme(
            TerrainGenerator terrainGenerator, 
            Material material, 
            RampFog fog, 
            Vignette vignette,
            MeshRenderer waterMeshRenderer,
            ColorGrading waterColorGrading,
            MeshRenderer seaFloorMeshRenderer)
        {
            ThemeColorPalettes colorPalette = colorPalettes[MapGenerator.rng.RangeInt(0, colorPalettes.Length)];
            Texture2D colorGradiant = SetTexture(material, colorPalette);
            
            var skybox = skyboxes[MapGenerator.rng.RangeInt(0, skyboxes.Length)].material;
            RenderSettings.skybox = skybox;

            var water = waters[MapGenerator.rng.RangeInt(0, waters.Length)];
            waterMeshRenderer.material = new Material(water.material);
            waterMeshRenderer.material.SetTexture("_Cube", skybox.GetTexture("_Tex"));

            var skyColor = (skybox.GetColor("_Tint") * skybox.GetFloat("_Exposure")).ToHSV();
            
            if (skyColor.hue == 0 && skyColor.saturation == 0)
            {
                skyColor.hue = 0.5f;
                skyColor.saturation = 0.2f;
            }

            ColorHSV minLightHsv = colorPalette.light.minColor.ToHSV();
            ColorHSV maxLightHsv = colorPalette.light.maxColor.ToHSV();

            skyColor.hue = ColorHSV.ClampHue(skyColor.hue, minLightHsv.hue, maxLightHsv.hue);

            float depthColorSaturation = Mathf.Clamp(skyColor.saturation, water.minSaturation, water.maxSaturation);
            float depthColorValue = Mathf.Clamp(skyColor.value, water.minValue, water.maxValue);

            var depthColor = Color.HSVToRGB(skyColor.hue, depthColorSaturation, depthColorValue);

            var cubeColor = Color.HSVToRGB(
                skyColor.hue,
                Mathf.Clamp01(water.cubeSaturation + depthColorSaturation),
                Mathf.Clamp01(water.cubeValue + depthColorValue));

            waterMeshRenderer.material.SetColor("_DepthColor", depthColor);
            waterMeshRenderer.material.SetColor("_CubeColor", cubeColor);
            waterMeshRenderer.material.SetFloat("_Reflection", water.reflection);
            waterMeshRenderer.material.SetFloat("_Distortion", water.distortion);

            var seaFloorColor = Color.HSVToRGB(
                skyColor.hue,
                Mathf.Clamp01(water.seaFloorSaturation + depthColorSaturation),
                Mathf.Clamp01(water.seaFloorValue + depthColorValue));

            seaFloorMeshRenderer.material.color = seaFloorColor;

            Vector4 waterVector = depthColor;
            Vector3 waterVector3 = waterVector;

            waterColorGrading.lift.Override(waterVector3 * water.lift);
            waterColorGrading.gamma.Override(waterVector3 * water.gamma);
            waterColorGrading.gain.Override(waterVector3 * water.gain);

            //waterColorGrading.mixerRedOutRedIn.Override(100 + waterColor.r * 100);
            //waterColorGrading.mixerGreenOutGreenIn.Override(100 + waterColor.g * 100);
            //waterColorGrading.mixerBlueOutBlueIn.Override(100 + waterColor.b * 100);

            ColorHSV lightHsv = ColorHSV.GetRandom(minLightHsv, maxLightHsv, MapGenerator.rng);

            RenderSettings.sun.color = lightHsv.ToRGB();
            RenderSettings.ambientLight = new Color(terrainGenerator.ambiantLightIntensity, terrainGenerator.ambiantLightIntensity, terrainGenerator.ambiantLightIntensity);

            SetFog(fog, lightHsv.hue, terrainGenerator.fogPower, terrainGenerator.fogIntensityCoefficient, colorPalette);

            vignette.intensity.value = terrainGenerator.vignetteInsentity;

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

            material.mainTexture = wallTexture.texture;
            material.SetTexture("_WallNormalTex", wallTexture.normal);
            material.SetFloat("_WallBias", wallTexture.bias);
            material.SetColor("_WallColor", wallTexture.averageColor);
            material.SetFloat("_WallScale", wallTexture.scale);
            material.SetFloat("_WallBumpScale", wallTexture.bumpScale);
            material.SetFloat("_WallContrast", wallTexture.constrast);
            material.SetFloat("_WallGlossiness", wallTexture.glossiness);
            material.SetFloat("_WallMetallic", wallTexture.metallic);

            material.SetTexture("_FloorTex", floorTexture.texture);
            material.SetTexture("_FloorNormalTex", floorTexture.normal);
            material.SetFloat("_FloorBias", floorTexture.bias);
            material.SetColor("_FloorColor", floorTexture.averageColor);
            material.SetFloat("_FloorScale", floorTexture.scale);
            material.SetFloat("_FloorBumpScale", floorTexture.bumpScale);
            material.SetFloat("_FloorContrast", floorTexture.constrast);
            material.SetFloat("_FloorGlossiness", floorTexture.glossiness);
            material.SetFloat("_FloorMetallic", floorTexture.metallic);

            return colorGradiant;
        }

        private void SetFog(RampFog fog, float sunHue, float power, float intensityCoefficient, ThemeColorPalettes colorPalette)
        {
            ColorHSV minColor = colorPalette.fog.minColor.ToHSV();
            ColorHSV maxColor = colorPalette.fog.maxColor.ToHSV();

            ColorHSV fogColorHSV = ColorHSV.GetRandom(minColor, maxColor, MapGenerator.rng);
            float fogHue = ColorHSV.ClampHue(sunHue, minColor.hue, maxColor.hue);

            var fogColor = Color.HSVToRGB(fogHue, fogColorHSV.saturation, fogColorHSV.value);

            fog.fogColorStart.value = fogColor;
            fog.fogColorStart.value.a = colorPalette.fog.colorStartAlpha;
            fog.fogColorMid.value = fogColor;
            fog.fogColorMid.value.a = colorPalette.fog.colorMidAlpha;
            fog.fogColorEnd.value = fogColor;
            fog.fogColorEnd.value.a = colorPalette.fog.colorEndAlpha;
            fog.fogZero.value = colorPalette.fog.zero;
            fog.fogOne.value = colorPalette.fog.one;

            fog.fogIntensity.value = colorPalette.fog.intensity * intensityCoefficient;
            fog.fogPower.value = power;
        }
    }
}

﻿using RoR2;
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
        public SurfaceTexture[] walls = new SurfaceTexture[0];
        public SurfaceTexture[] floor = new SurfaceTexture[0];
        public ThemeColorPalettes[] colorPalettes;
        public SkyboxDef[] skyboxes = new SkyboxDef[0];
        public WaterDef[] waters = new WaterDef[0];
        public SurfaceTexture[] seaFloors = new SurfaceTexture[0];
        public PropsDefinitionCollection[] propCollections = new PropsDefinitionCollection[0];

        public Texture2D ApplyTheme(
            TerrainGenerator terrainGenerator, 
            Material material, 
            RampFog fog, 
            Vignette vignette,
            MeshRenderer waterMeshRenderer,
            MeshRenderer seaFloorMeshRenderer)
        {
            ThemeColorPalettes colorPalette = colorPalettes[MapGenerator.rng.RangeInt(0, colorPalettes.Length)];
            Texture2D colorGradiant = SetTexture(material, colorPalette);
            
            var skybox = skyboxes[MapGenerator.rng.RangeInt(0, skyboxes.Length)].material;
            RenderSettings.skybox = skybox;

            var water = waters[MapGenerator.rng.RangeInt(0, waters.Length)];
            waterMeshRenderer.material = new Material(water.material);
            waterMeshRenderer.material.SetTexture("_Cube", skybox.GetTexture("_Tex"));

            var skyTint = skybox.GetColor("_Tint") * skybox.GetFloat("_Exposure"); ;
            Color.RGBToHSV(skyTint, out float skyHue, out float skySaturation, out float skyValue);

            if (skyHue == 0 && skySaturation == 0)
            {
                skyHue = 0.5f;
                skySaturation = 0.2f;
            }

            var waterColor = Color.HSVToRGB(
                skyHue,
                Mathf.Clamp(skySaturation, water.minSaturation, water.maxSaturation),
                Mathf.Clamp(skyValue, water.minValue, water.maxValue));

            waterMeshRenderer.material.SetColor("_CubeColor", waterColor);
            waterMeshRenderer.material.SetColor("_DepthColor", waterColor);
            waterMeshRenderer.material.SetFloat("_Reflection", water.reflection);
            waterMeshRenderer.material.SetFloat("_Distortion", water.distortion);

            seaFloorMeshRenderer.material.color = waterColor;

            //seaFloorMeshRenderer.material.mainTexture = seaFloors[MapGenerator.rng.RangeInt(0, seaFloors.Length)].texture;

            float sunHue = MapGenerator.rng.nextNormalizedFloat;
            RenderSettings.sun.color = Color.HSVToRGB(sunHue, colorPalette.light.saturation, colorPalette.light.value);

            RenderSettings.ambientLight = new Color(terrainGenerator.ambiantLightIntensity, terrainGenerator.ambiantLightIntensity, terrainGenerator.ambiantLightIntensity);

            SetFog(fog, sunHue, terrainGenerator.fogPower, colorPalette);

            vignette.intensity.value = terrainGenerator.vignetteInsentity;
            //DynamicGI.UpdateEnvironment();

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

        private void SetFog(RampFog fog, float sunHue, float power, ThemeColorPalettes colorPalette)
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
            fog.fogPower.value = power;
        }
    }
}
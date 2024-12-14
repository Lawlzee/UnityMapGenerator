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
        public SurfaceTexture[] detail = new SurfaceTexture[0];
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
            MeshRenderer seaFloorMeshRenderer,
            SurfaceDefProvider terrainSurfaceDefProvider)
        {
            MaterialInfo materialInfo = SetTexture(material, terrainSurfaceDefProvider, MapGenerator.rng);

            var skybox = skyboxes[MapGenerator.rng.RangeInt(0, skyboxes.Length)].material;
            if (Application.isEditor)
            {
                skybox = MapGenerator.instance.editorSkybox?.material ?? skybox;
            }

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

            ColorHSV minLightHsv = materialInfo.colorPalette.light.minColor.ToHSV();
            ColorHSV maxLightHsv = materialInfo.colorPalette.light.maxColor.ToHSV();

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

            SetFog(fog, terrainGenerator.fogPower, terrainGenerator.fogIntensityCoefficient, materialInfo.colorPalette);

            vignette.intensity.value = terrainGenerator.vignetteInsentity;

            return materialInfo.grassColorGradiant;
        }

        public MaterialInfo SetTexture(Material material, SurfaceDefProvider terrainSurfaceDefProvider, Xoroshiro128Plus rng)
        {
            MaterialInfo materialInfo = GenerateMaterialInfo(rng);
            terrainSurfaceDefProvider.surfaceDef = materialInfo.floorTexture.surfaceDef;

            materialInfo.ApplyTo(material);
            return materialInfo;
        }

        public MaterialInfo GenerateMaterialInfo(Xoroshiro128Plus rng)
        {
            ThemeColorPalettes colorPalette = colorPalettes[rng.RangeInt(0, colorPalettes.Length)];
            Texture2D colorGradiant = colorPalette.CreateTerrainTexture(rng);

            int floorIndex = rng.RangeInt(0, floor.Length);
            int wallIndex = rng.RangeInt(0, walls.Length);
            int detailIndex = rng.RangeInt(0, walls.Length);

            SurfaceTexture floorTexture = floor[floorIndex];
            SurfaceTexture wallTexture = walls[wallIndex];
            SurfaceTexture detailTexture = detail[detailIndex];

            if (Application.isEditor)
            {
                floorTexture = MapGenerator.instance?.editorFloorTexture ?? floorTexture;
                wallTexture = MapGenerator.instance?.editorWallTexture ?? wallTexture;
                detailTexture = MapGenerator.instance?.editorDetailTexture ?? detailTexture;
            }

            return new MaterialInfo
            {
                colorPalette = colorPalette,
                colorGradiant = colorGradiant,
                grassColorGradiant = colorPalette.grass != null
                    ? colorPalette.CreateGrassTexture(rng)
                    : colorGradiant,
                floorTexture = floorTexture,
                wallTexture = wallTexture,
                detailTexture = detailTexture
            };
        }

        private void SetFog(RampFog fog, float power, float intensityCoefficient, ThemeColorPalettes colorPalette)
        {
            ColorHSV minColor = colorPalette.fog.minColor.ToHSV();
            ColorHSV maxColor = colorPalette.fog.maxColor.ToHSV();

            fog.fogColorStart.value = GetRandomFogColor();
            fog.fogColorStart.value.a = colorPalette.fog.colorStartAlpha;
            fog.fogColorMid.value = GetRandomFogColor();
            fog.fogColorMid.value.a = colorPalette.fog.colorMidAlpha;
            fog.fogColorEnd.value = GetRandomFogColor();
            fog.fogColorEnd.value.a = colorPalette.fog.colorEndAlpha;
            fog.fogZero.value = colorPalette.fog.zero;
            fog.fogOne.value = colorPalette.fog.one;

            fog.fogIntensity.value = colorPalette.fog.intensity * intensityCoefficient;
            fog.fogPower.value = power;

            Color GetRandomFogColor()
            {
                ColorHSV fogColorHSV = ColorHSV.GetRandom(minColor, maxColor, MapGenerator.rng);
                return fogColorHSV.ToRGB();
            }
        }

        public void CheckAssets()
        {
            var invalidTextures = walls.Concat(floor).Concat(detail)
                .Where(x => x.texture == null)
                .Select(x => x.textureAsset)
                .ToList();

            if (invalidTextures.Count > 0)
            {
                Log.Debug("Invalid textures: " + string.Join("\r\n", invalidTextures));
            }

            var invalidMaterials = skyboxes
                .Where(x => x.material == null)
                .Select(x => x.asset)
                .ToList();

            if (invalidTextures.Count > 0)
            {
                Log.Debug("Invalid materials: " + string.Join("\r\n", invalidMaterials));
            }

            var invalidWaters = waters
                .Where(x => x.material == null)
                .Select(x => x.asset)
                .ToList();

            if (invalidWaters.Count > 0)
            {
                Log.Debug("Invalid waters: " + string.Join("\r\n", invalidMaterials));
            }

            var invalidProps = propCollections
                .SelectMany(x => x.categories)
                .SelectMany(x => x.props)
                .Where(x => Addressables.LoadAssetAsync<GameObject>(x.asset).WaitForCompletion() == null)
                .Select(x => x.asset)
                .ToList();

            if (invalidProps.Count > 0)
            {
                Log.Debug("Invalid props: " + string.Join("\r\n", invalidMaterials));
            }
        }
    }
}

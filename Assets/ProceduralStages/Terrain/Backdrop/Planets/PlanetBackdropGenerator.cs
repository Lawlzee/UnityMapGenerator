using Rewired.ComponentControls.Effects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ProceduralStages
{
    [CreateAssetMenu(fileName = "PlanetBackdropGenerator", menuName = "ProceduralStages/PlanetBackdropGenerator", order = 2)]
    public class PlanetBackdropGenerator : BackdropTerrainGenerator
    {
        public GameObject planetHolderPrefab;
        public Interval planetHolderRotation;

        public Interval planetDistance;
        public Interval planetScale;
        public Interval planetRotation;
        public Interval planetAngleX;

        [Range(0, 1)]
        public float ringSpawnRate;
        public Interval ringScale;
        public Vector3 ringAngle;

        public string mdlPlanetPath;
        public string mdlPlanetVertexShadowPath;
        public string mdlPlanetRingPath;

        public string opaqueMaterial;
        public string edgeLightMaterial;
        public string[] diffuseMaterial;
        public string[] ringMaterial;

        public override GameObject Generate(BackdropParams args)
        {
            Xoroshiro128Plus rng = new Xoroshiro128Plus(args.seed);

            GameObject planetHolder = Instantiate(planetHolderPrefab);
            planetHolder.transform.position = args.center;
            planetHolder.GetComponent<RotateAroundAxis>().slowRotationSpeed = rng.RangeFloat(planetHolderRotation.min, planetHolderRotation.max);

            float distance = rng.RangeFloat(planetDistance.min, planetDistance.max);
            Vector3 planetAngle = new Vector3(
                rng.RangeFloat(planetAngleX.min, planetAngleX.max),
                rng.nextNormalizedFloat * 360,
                0);

            Vector3 planetPosition = Quaternion.Euler(planetAngle) * Vector3.forward * distance;
            float scale = rng.RangeFloat(planetScale.min, planetScale.max);

            GameObject planet = planetHolder.transform.GetChild(0).gameObject;
            planet.transform.position = planetPosition;
            planet.transform.localEulerAngles = new Vector3(
                rng.nextNormalizedFloat * 360,
                rng.nextNormalizedFloat * 360,
                rng.nextNormalizedFloat * 360);            
            planet.transform.localScale = new Vector3(scale, scale, scale);
            planet.GetComponent<RotateAroundAxis>().slowRotationSpeed = rng.RangeFloat(planetRotation.min, planetRotation.max);

            Transform opaqueBase = planet.transform.GetChild(0);
            opaqueBase.GetComponent<MeshFilter>().mesh = ExtractMesh(mdlPlanetPath);
            opaqueBase.GetComponent<MeshRenderer>().material = Addressables.LoadAssetAsync<Material>(opaqueMaterial).WaitForCompletion();

            Transform diffuse = planet.transform.GetChild(1);
            Mesh vertexShadowMesh = ExtractMesh(mdlPlanetVertexShadowPath);
            diffuse.GetComponent<MeshFilter>().mesh = vertexShadowMesh;

            string selectedDiffuseMaterial = rng.NextElementUniform(diffuseMaterial);
            diffuse.GetComponent<MeshRenderer>().material = Addressables.LoadAssetAsync<Material>(selectedDiffuseMaterial).WaitForCompletion();

            Transform edgeLight = planet.transform.GetChild(2);
            edgeLight.GetComponent<MeshFilter>().mesh = vertexShadowMesh;
            edgeLight.GetComponent<MeshRenderer>().material = Addressables.LoadAssetAsync<Material>(edgeLightMaterial).WaitForCompletion();

            Transform rings = planet.transform.GetChild(3);
            rings.gameObject.SetActive(rng.nextNormalizedFloat < ringSpawnRate);

            float planetRingScale = rng.RangeFloat(ringScale.min, ringScale.max);
            rings.localScale = new Vector3(planetRingScale, planetRingScale, planetRingScale);

            rings.localEulerAngles = new Vector3(
                rng.nextNormalizedFloat * 360,
                rng.nextNormalizedFloat * 360,
                rng.nextNormalizedFloat * 360);

            rings.eulerAngles = ringAngle;

            rings.GetComponent<MeshFilter>().mesh = ExtractMesh(mdlPlanetRingPath);

            string selectedRingMaterial = rng.NextElementUniform(ringMaterial);
            rings.GetComponent<MeshRenderer>().material = Addressables.LoadAssetAsync<Material>(selectedRingMaterial).WaitForCompletion();

            return planetHolder;
        }

        private Mesh ExtractMesh(string assetKey)
        {
            GameObject prefab = Addressables.LoadAssetAsync<GameObject>(assetKey).WaitForCompletion();
            return prefab.transform.GetChild(0).GetComponent<MeshFilter>().sharedMesh;
        }
    }
}

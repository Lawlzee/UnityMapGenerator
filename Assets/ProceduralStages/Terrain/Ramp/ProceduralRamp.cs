using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProceduralStages
{
    public class ProceduralRamp : MonoBehaviour
    {
        public MapTheme[] themes;
        public MeshColorer meshColorer = new MeshColorer();
        public PropsPlacer propsPlacer = new PropsPlacer();
        public NodeGraphCreator nodeGraphCreator = new NodeGraphCreator();
        
        public float scale;
        public int maxPropKind;

        public FBM fbm;
        public ThreadSafeCurve noiseRemap;

        public MeshFilter meshFilter;
        public MeshRenderer meshRenderer;
        public MeshCollider meshCollider;
        public SurfaceDefProvider surfaceDefProvider;
        public Xoroshiro128Plus rng;

        public static ProceduralRamp instance;

        public void Awake()
        {
            rng = new Xoroshiro128Plus((ulong)DateTime.Now.Ticks);
            instance = this;
        }

        public void OnDestroy()
        {
            instance = null;
        }

        public void Generate(Vector3 size, float distance, float yOffset, float noiseLevel, float propsWeight)
        {
            for (int i = 0; i < propsPlacer.instances.Count; i++)
            {
                Destroy(propsPlacer.instances[i]);
            }
            propsPlacer.instances.Clear();

            var camera = Camera.main.transform;
            Vector3 angle = new Vector3(0, camera.eulerAngles.y, 0);

            Quaternion rotation = Quaternion.Euler(angle);
            Vector3 forwardVector = rotation * Vector3.forward;
            Vector3 leftVector = rotation * Vector3.left;
            Vector3 rampPosition = camera.position + forwardVector * distance + leftVector * 0.5f * size.x + new Vector3(0, yOffset, 0);

            transform.position = rampPosition;
            transform.eulerAngles = angle;

            Vector3Int sizeInt = new Vector3Int(
                Mathf.CeilToInt(size.x / scale),
                Mathf.CeilToInt(size.y / scale),
                Mathf.CeilToInt(size.z / scale));

            float[,,] densityMap = GenerateRamp(sizeInt, noiseLevel);

            var meshResult = MarchingCubes.CreateMesh(densityMap, scale);

            Graphs graphs = nodeGraphCreator.CreateBackdropGraphs(new MeshBackdropTerrain
            {
                meshResult = meshResult,
                propsWeigth = propsWeight
            });

            var theme = rng.NextElementUniform(themes);
            var colorGradiant = theme.ApplyTextures(meshRenderer.material, surfaceDefProvider, rng);

            meshColorer.ColorMesh(meshResult, rng);
            meshFilter.mesh = meshResult.mesh;
            meshCollider.sharedMesh = meshResult.mesh;

            propsPlacer.PlaceAll(
                rng,
                Vector3.zero,
                graphs,
                theme.propCollections[0],
                meshColorer,
                colorGradiant,
                meshRenderer.material,
                0,
                propsWeight,
                bigObjectOnly: false,
                maxPropKind);
        }

        private float[,,] GenerateRamp(Vector3Int size, float noiseLevel)
        {
            float[,,] densityMap = new float[size.x, size.y, size.z];

            int seedX = rng.RangeInt(0, short.MaxValue);
            int seedZ = rng.RangeInt(0, short.MaxValue);

            Parallel.For(1, size.x - 1, x =>
            {
                for (int z = 1; z < size.z - 1; z++)
                {
                    float noiseBonues = noiseLevel * noiseRemap.Evaluate(0.5f * (fbm.Evaluate(x + seedX, z + seedZ) + 1)) / size.y;

                    float depth = Mathf.Lerp(0.5f, 1, (float)z / (size.z - 1));
                    for (int y = 1; y < size.y - 1; y++)
                    {
                        float yNoise = Mathf.Lerp(0, -0.5f, (float)y / (size.y - 1));
                        float noise = depth + yNoise + noiseBonues;

                        densityMap[x, y, z] = Mathf.Clamp01(noise);
                    }
                }
            });

            return densityMap;
        }
    }
}

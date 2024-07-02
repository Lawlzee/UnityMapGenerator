using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProceduralStages
{
    [CreateAssetMenu(fileName = "IslandBackdropGenerator", menuName = "ProceduralStages/IslandBackdropGenerator", order = 2)]
    public class IslandBackdropGenerator : BackdropTerrainGenerator
    {
        public FBM fbm;
        public ThreadSafeCurve floorCurve;
        public ThreadSafeCurve finalCurve;
        public ThreadSafeCurve ellipsisDistanceCurve;
        public float blendFactor;
        public float propsWeigth;

        public override BackdropTerrain Generate(Vector3 center, ulong seed, ProfilerLog log)
        {
            var rng = new Xoroshiro128Plus(seed);

            Vector3 size = new Vector3(
                rng.RangeFloat(minSize.x, maxSize.x),
                rng.RangeFloat(minSize.y, maxSize.y),
                rng.RangeFloat(minSize.z, maxSize.z));

            float islandDistance = rng.RangeFloat(distance.min, distance.max)
                + Mathf.Sqrt(size.x * size.x + size.z * size.z);

            float islandAngle = rng.RangeFloat(0, 2 * Mathf.PI);

            Vector3 position = islandDistance * new Vector3(Mathf.Cos(islandAngle), 0, Mathf.Sin(islandAngle))
                + center
                - 0.5f * size;
            position.y = 0;

            float scale = scalePerDistance * islandDistance;

            Vector3Int sizeInt = new Vector3Int(
                Mathf.CeilToInt(size.x / scale),
                Mathf.CeilToInt(size.y / scale),
                Mathf.CeilToInt(size.z / scale));

            Vector2 islandCenter = 0.5f * new Vector2(sizeInt.x, sizeInt.z);

            int seedX = rng.RangeInt(0, short.MaxValue);
            int seedZ = rng.RangeInt(0, short.MaxValue);

            float[,,] densityMap = new float[sizeInt.x, sizeInt.y, sizeInt.z];
            Parallel.For(0, sizeInt.x, x =>
            {
                float dx = x - islandCenter.x;
                for (int z = 0; z < sizeInt.z; z++)
                {
                    float dz = z - islandCenter.y;
                    float ellipsisDistance = Mathf.Sqrt((dx * dx) / (islandCenter.x * islandCenter.x) + (dz * dz) / (islandCenter.y * islandCenter.y));

                    Vector2 scaledPos = scale * new Vector2(x, z);

                    float floorNoise = floorCurve.Evaluate(0.5f * (fbm.Evaluate(scaledPos.x + seedX, scaledPos.y + seedZ) + 1));
                    float finalNoise = finalCurve.Evaluate(floorNoise * ellipsisDistanceCurve.Evaluate(ellipsisDistance));

                    float floorHeight = finalNoise * sizeInt.y;
                    //floorHeight = 0.5f * sizeInt.y;
                    int y = 0;
                    for (; y < sizeInt.y; y++)
                    {
                        float noise = Mathf.Clamp01((floorHeight - y) * blendFactor * scale + 0.5f);
                        if (noise == 0f)
                        {
                            break;
                        }
                        densityMap[x, y, z] = noise;
                    }

                    //for (; y < height3d; y++)
                    //{
                    //    floorMap[x, y, z] = map[x, y, z];
                    //}
                }
            });

            var meshResult = MarchingCubes.CreateMesh(densityMap, scale);

            return new BackdropTerrain
            {
                meshResult = meshResult,
                position = position,
                propsWeigth = propsWeigth
            };
        }
    }
}

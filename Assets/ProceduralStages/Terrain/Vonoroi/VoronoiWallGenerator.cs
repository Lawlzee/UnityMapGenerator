using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProceduralStages
{
    [CreateAssetMenu(fileName = "VoronoiWallGenerator", menuName = "ProceduralStages/VoronoiWallGenerator", order = 2)]
    public class VoronoiWallGenerator : ScriptableObject
    {
        public FBM wallNoiseFBM;
        [Range(0, 1)]
        public float wallSurfaceLevel = 0.5f;
        public ThreadSafeCurve wallCurve;

        public FBM carverNoiseFBM;
        [Range(0, 1)]
        public float carverSurfaceLevel = 0.5f;
        public ThreadSafeCurve carverCurve;
        public float carverVerticalScale;

        public ThreadSafeCurve finalCurve;

        public Voronoi3D voronoi;

        public float[,,] Create(Vector3Int size)
        {
            int wallSeedX = MapGenerator.rng.RangeInt(0, short.MaxValue);
            int wallSeedZ = MapGenerator.rng.RangeInt(0, short.MaxValue);

            int carverSeedX = MapGenerator.rng.RangeInt(0, short.MaxValue);
            int carverSeedY = MapGenerator.rng.RangeInt(0, short.MaxValue);
            int carverSeedZ = MapGenerator.rng.RangeInt(0, short.MaxValue);

            float[,,] map = new float[size.x, size.y, size.z];

            Parallel.For(0, size.x, x =>
            {
                for (int y = 0; y < size.y; y++)
                {
                    for (int z = 0; z < size.z; z++)
                    {
                        Voronoi3DResult voronoiResult = voronoi[x, y, z];
                        Vector3 pos1 = new Vector3(x, y, z) + voronoiResult.displacement1;
                        Vector3 pos2 = new Vector3(x, y, z) + voronoiResult.displacement2;

                        float wallNoise1 = 0.5f * (wallNoiseFBM.Evaluate(pos1.x + wallSeedX, pos1.z + wallSeedZ) + 1);
                        bool isWall1 = wallNoise1 > wallSurfaceLevel;
                        if (isWall1)
                        {
                            float carverNoise1 = 0.5f * (carverNoiseFBM.Evaluate(pos1.x + carverSeedX, carverVerticalScale * pos1.y + carverSeedY, pos1.z + carverSeedZ) + 1);
                            isWall1 = carverNoise1 > carverSurfaceLevel;
                        }

                        float wallNoise2 = 0.5f * (wallNoiseFBM.Evaluate(pos2.x + wallSeedX, pos2.z + wallSeedZ) + 1);
                        bool isWall2 = wallNoise2 > wallSurfaceLevel;
                        if (isWall2)
                        {
                            float carverNoise2 = 0.5f * (carverNoiseFBM.Evaluate(pos2.x + carverSeedX, carverVerticalScale * pos2.y + carverSeedY, pos2.z + carverSeedZ) + 1);
                            isWall2 = carverNoise2 > carverSurfaceLevel;
                        }

                        if (isWall1 && isWall2)
                        {
                            map[x, y, z] = 1;
                        }
                        else if (isWall1)
                        {
                            map[x, y, z] = 1 - voronoiResult.weight;
                        }
                        else if (isWall2)
                        {
                            map[x, y, z] = voronoiResult.weight;
                        }
                    }
                }
            });

            return map;
        }
    }
}

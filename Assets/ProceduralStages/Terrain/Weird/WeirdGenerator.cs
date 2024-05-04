using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityMeshSimplifier;

namespace ProceduralStages
{
    [CreateAssetMenu(fileName = "WeirdGenerator", menuName = "ProceduralStages/WeirdGenerator", order = 2)]
    public class WeirdGenerator : TerrainGenerator
    {
        private static readonly Vector3Int[] _directions = new Vector3Int[]
        {
            new Vector3Int(0, 1, 1),
            new Vector3Int(0, 1, -1),
            new Vector3Int(0, -1, 1),
            new Vector3Int(0, -1, -1),
            new Vector3Int(1, 1, 0),
            new Vector3Int(-1, 1, 0),
            new Vector3Int(1, -1, 0),
            new Vector3Int(-1, -1, 0),
            Vector3Int.left,
            Vector3Int.left,
            Vector3Int.left,
            Vector3Int.right,
            Vector3Int.right,
            Vector3Int.right,
            Vector3Int.right,
            new Vector3Int(0, 0, 1),
            new Vector3Int(0, 0, 1),
            new Vector3Int(0, 0, 1),
            new Vector3Int(0, 0, 1),
            new Vector3Int(0, 0, -1),
            new Vector3Int(0, 0, -1),
            new Vector3Int(0, 0, -1),
            new Vector3Int(0, 0, -1)
        };

        public int randomWalks;
        public int randomWalkIterations = 1000;
        public int randomWalkMinLength;
        public int randomWalkMaxLength;
        [Range(0f, 1f)]
        public float randomWalkAddedWeight;

        [Range(0f, 1f)]
        public float maxYSlop;


        [Range(0f, 1f)]
        public float directionChangeWeight;

        public CellularAutomata3d smoother;

        public override Terrain Generate()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            //float[,,] map3d = RandomWalk(MapGenerator.instance.stageSize);
            float[,,] map3d = AngleRandomWalk(MapGenerator.instance.stageSize);

            map3d = smoother.SmoothMap(map3d);
            LogStats("smoother.SmoothMap");

            var unOptimisedMesh = MarchingCubes.CreateMesh(map3d, MapGenerator.instance.mapScale);
            LogStats("marchingCubes");

            MeshSimplifier simplifier = new MeshSimplifier(unOptimisedMesh);
            simplifier.SimplifyMesh(MapGenerator.instance.meshQuality);
            var optimisedMesh = simplifier.ToMesh();
            LogStats("MeshSimplifier");

            return new Terrain
            {
                meshResult = new MeshResult
                {
                    mesh = optimisedMesh,
                    normals = optimisedMesh.normals,
                    triangles = optimisedMesh.triangles,
                    vertices = optimisedMesh.vertices
                },
                floorlessDensityMap = map3d,
                densityMap = map3d,
                maxGroundheight = float.MaxValue
            };

            void LogStats(string name)
            {
                Log.Debug($"{name}: {stopwatch.Elapsed}");
                stopwatch.Restart();
            }
        }

        private float[,,] RandomWalk(Vector3Int size)
        {
            float[,,] map = new float[size.x, size.y, size.z];

            Xoroshiro128Plus[] rngs = new Xoroshiro128Plus[randomWalks];

            for (int i = 0; i < randomWalks; i++)
            {
                rngs[i] = new Xoroshiro128Plus(MapGenerator.rng.nextUlong);
            }

            Parallel.For(0, randomWalks, i =>
            {
                var rng = rngs[i];

                Vector3Int currentPosition = size / 2;
                for (int j = 0; j < randomWalkIterations; j++)
                {
                    int directionIndex = rng.RangeInt(0, _directions.Length);
                    Vector3Int direction = _directions[directionIndex];

                    int length = rng.RangeInt(randomWalkMinLength, randomWalkMaxLength);

                    for (int k = 0; k < length; k++)
                    {
                        currentPosition += direction;
                        if (currentPosition.x > 0
                            && currentPosition.y > 0
                            && currentPosition.z > 0
                            && currentPosition.x < size.x
                            && currentPosition.y < size.y
                            && currentPosition.z < size.z)
                        {
                            map[currentPosition.x, currentPosition.y, currentPosition.z] = 1;
                            map[currentPosition.x, currentPosition.y, currentPosition.z - 1] = 1;
                            map[currentPosition.x, currentPosition.y - 1, currentPosition.z] = 1;
                            map[currentPosition.x, currentPosition.y - 1, currentPosition.z - 1] = 1;
                            map[currentPosition.x - 1, currentPosition.y, currentPosition.z] = 1;
                            map[currentPosition.x - 1, currentPosition.y, currentPosition.z - 1] = 1;
                            map[currentPosition.x - 1, currentPosition.y - 1, currentPosition.z] = 1;
                            map[currentPosition.x - 1, currentPosition.y - 1, currentPosition.z - 1] = 1;
                        }
                        else
                        {
                            currentPosition -= direction;
                            break;
                        }
                    }
                }
            });

            return map;
        }

        private float[,,] AngleRandomWalk(Vector3Int size)
        {
            float[,,] map = new float[size.x, size.y, size.z];

            Xoroshiro128Plus[] rngs = new Xoroshiro128Plus[randomWalks];

            for (int i = 0; i < randomWalks; i++)
            {
                rngs[i] = new Xoroshiro128Plus(MapGenerator.rng.nextUlong);
            }

            Parallel.For(0, randomWalks, i =>
            {
                var rng = rngs[i];

                Vector3 currentPosition = new Vector3((int)rng.RangeFloat(size.x * 0.25f, size.x * 0.75f), (int)rng.RangeFloat(size.y * 0.25f, size.y * 0.75f), (int)rng.RangeFloat(size.z * 0.25f, size.z * 0.75f));
                Vector3 currentDirection = new Vector3(rng.RangeFloat(-1, 1), rng.RangeFloat(-1, 1), rng.RangeFloat(-1, 1)).normalized;

                for (int j = 0; j < randomWalkIterations; j++)
                {
                    currentDirection += new Vector3(rng.RangeFloat(-1, 1), rng.RangeFloat(-1, 1), rng.RangeFloat(-1, 1)).normalized * directionChangeWeight;

                    if (currentDirection.y > maxYSlop)
                    {
                        currentDirection.y = maxYSlop;
                    }

                    if (currentDirection.y < -maxYSlop)
                    {
                        currentDirection.y = -maxYSlop;
                    }

                    currentDirection.Normalize();

                    currentPosition += currentDirection;
                    int x0 = Mathf.FloorToInt(currentPosition.x);
                    int x1 = Mathf.CeilToInt(currentPosition.x);
                    int y0 = Mathf.FloorToInt(currentPosition.y);
                    int y1 = Mathf.CeilToInt(currentPosition.y);
                    int z0 = Mathf.FloorToInt(currentPosition.z);
                    int z1 = Mathf.CeilToInt(currentPosition.z);

                    if (x0 > 0
                        && y0 > 0
                        && z0 > 0
                        && x1 < size.x
                        && y1 < size.y
                        && z1 < size.z)
                    {
                        float dx0 = currentDirection.x - x0;
                        float dx1 = x1 - currentDirection.x;
                        float dy0 = currentDirection.y - y0;
                        float dy1 = y1 - currentDirection.y;
                        float dz0 = currentDirection.z - z0;
                        float dz1 = z1 - currentDirection.z;

                        map[x0, y0, z0] = Math.Max(Mathf.Sqrt(dx0 * dx0 + dy0 * dy0 + dz0 * dz0), map[x0, y0, z0]);
                        map[x0, y0, z1] = Math.Max(Mathf.Sqrt(dx0 * dx0 + dy0 * dy0 + dz1 * dz1), map[x0, y0, z1]);
                        map[x0, y1, z0] = Math.Max(Mathf.Sqrt(dx0 * dx0 + dy1 * dy1 + dz0 * dz0), map[x0, y1, z0]);
                        map[x0, y1, z1] = Math.Max(Mathf.Sqrt(dx0 * dx0 + dy1 * dy1 + dz1 * dz1), map[x0, y1, z1]);
                        map[x1, y0, z0] = Math.Max(Mathf.Sqrt(dx1 * dx1 + dy0 * dy0 + dz0 * dz0), map[x1, y0, z0]);
                        map[x1, y0, z1] = Math.Max(Mathf.Sqrt(dx1 * dx1 + dy0 * dy0 + dz1 * dz1), map[x1, y0, z1]);
                        map[x1, y1, z0] = Math.Max(Mathf.Sqrt(dx1 * dx1 + dy1 * dy1 + dz0 * dz0), map[x1, y1, z0]);
                        map[x1, y1, z1] = Math.Max(Mathf.Sqrt(dx1 * dx1 + dy1 * dy1 + dz1 * dz1), map[x1, y1, z1]);
                    }
                    else
                    {
                        currentPosition -= currentDirection;
                        break;
                    }
                }
            });

            return map;
        }
    }
}

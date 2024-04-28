using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProceduralStages
{
    //https://www.shadertoy.com/view/stccDB
    [Serializable]
    public class SpaghettiCaver
    {
        public float frequency1;
        public float frequency2;
        public float verticalScale1;
        public float verticalScale2;
        public ThreadSafeCurve curve;
        public ThreadSafeCurve bonusNoiseByEllipsisDistance;
        public ThreadSafeCurve yDerivativeBonus;
        public ThreadSafeCurve yDerivativefloorDensityBonus;
        public FBM spaghettiNoise;

        public float layersDistance;
        public float layersAmplitude;
        public float layersFrequency;
        public ThreadSafeCurve layerCurve;

        private static readonly Vector3Int[] _adjacentPositions = new Vector3Int[]
        {
            Vector3Int.up,
            Vector3Int.down,
            Vector3Int.left,
            Vector3Int.right,
            new Vector3Int(0, 0, 1),
            new Vector3Int(0, 0, -1),
        };

        public (float[,,] map, float[,,] floorlessMap) Create(Vector3Int size)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            int seed1X = MapGenerator.rng.RangeInt(0, short.MaxValue);
            int seed1Y = MapGenerator.rng.RangeInt(0, short.MaxValue);
            int seed1Z = MapGenerator.rng.RangeInt(0, short.MaxValue);

            int seed2X = MapGenerator.rng.RangeInt(0, short.MaxValue);
            int seed2Y = MapGenerator.rng.RangeInt(0, short.MaxValue);
            int seed2Z = MapGenerator.rng.RangeInt(0, short.MaxValue);

            //float inverseMaxDistance = 1 / (layersAmplitude + layersFrequency);
            //int layerCount = Mathf.CeilToInt(size.y / layersDistance);
            //Vector2Int[] seeds = new Vector2Int[layerCount];
            //for (int i = 0; i < layerCount; i++)
            //{
            //    seeds[i] = new Vector2Int(
            //        MapGenerator.rng.RangeInt(0, short.MaxValue),
            //        MapGenerator.rng.RangeInt(0, short.MaxValue));
            //}

            float[,,] map = new float[size.x, size.y, size.z];
            float[,,] floorlessMap = new float[size.x, size.y, size.z];
            bool[,,] airNodes = new bool[size.x, size.y, size.z];

            Vector3 center = size / 2;
            //List<Vector3Int>[] airPositions = new List<Vector3Int>[size.x];

            Parallel.For(0, size.x, x =>
            {
                //var currentAirPositions = new List<Vector3Int>();
                //float[] layerHeights = new float[layerCount];

                for (int z = 0; z < size.z; z++)
                {
                    //for (int i = 0; i < layerCount; i++)
                    //{
                    //    layerHeights[i] = i * layersDistance + layersAmplitude * Mathf.PerlinNoise(layersFrequency * x + seeds[i].x, layersFrequency * z + seeds[i].y);
                    //}

                    for (int y = 0; y < size.y; y++)
                    {
                        if (x == 0
                            || y == 0
                            || z == 0
                            || x == size.x - 1
                            || y == size.y - 1
                            || z == size.z - 1)
                        {
                            map[x, y, z] = 1;
                            continue;
                        }

                        //float minDistance = float.MaxValue;

                        //for (int i = 0; i < layerCount; i++)
                        //{
                        //    float distance = Math.Abs(y - layerHeights[i]);
                        //    if (distance < minDistance)
                        //    {
                        //        minDistance = distance;
                        //    }
                        //}

                        float dx = x - center.x;
                        float dy = y - center.y;
                        float dz = z - center.z;

                        float ellipsisDistance = Mathf.Sqrt((dx * dx) / (center.x * center.x) + (dy * dy) / (center.y * center.y) + (dz * dz) / (center.z * center.z));
                        float bonusDistanceNoise = bonusNoiseByEllipsisDistance.Evaluate(ellipsisDistance);

                        (float noise1, Vector3 derivative1) = spaghettiNoise.EvaluateWithDerivative(x + seed1X, y * verticalScale1 + seed1Y, z + seed1Z);
                        (float noise2, Vector3 derivative2) = spaghettiNoise.EvaluateWithDerivative(x + seed2X, y * verticalScale2 + seed2Y, z + seed2Z);
                        Vector3 fullNoise = derivative1 + derivative2;
                        //float layersNoise = layerCurve.Evaluate(minDistance * inverseMaxDistance);

                        //float noiseAngle = (2 * Mathf.Atan2(fullNoise.y, Mathf.Sqrt(fullNoise.x * fullNoise.x + fullNoise.z * fullNoise.z))) / Mathf.PI;
                        //[-1, 1]
                        float noiseAngle = (2 * Mathf.Atan2(fullNoise.y, 1)) / Mathf.PI;

                        float noise = 0.5f * (noise1 * noise1 + noise2 * noise2);

                        float finalNoise = Mathf.Clamp01(curve.Evaluate(noise) + bonusDistanceNoise + yDerivativeBonus.Evaluate(noiseAngle));
                        map[x, y, z] = finalNoise;


                        if (finalNoise < 0.5f)
                        {
                            airNodes[x, y, z] = true;
                        }

                        float normalisedNoiseAngle = (2 * Mathf.Atan2(fullNoise.y, Mathf.Sqrt(fullNoise.x * fullNoise.x + fullNoise.z * fullNoise.z))) / Mathf.PI;
                        //floorlessMap[x, y, z] = Mathf.Clamp01(finalNoise + yDerivativefloorDensityBonus.Evaluate(normalisedNoiseAngle));
                        floorlessMap[x, y, z] = yDerivativefloorDensityBonus.Evaluate(normalisedNoiseAngle);
                        //float curveNoise = (;
                        //float scaledCurveNoise = curveMinNoise + (curveNoise * (1 - curveMinNoise));
                        //
                        //map[x, y, z] = Mathf.Clamp01(scaledWallNoise * scaledCurveNoise);
                    }
                }

                //airPositions[x] = currentAirPositions;
            });

            LogStats("caves");

            //HashSet<Vector3Int> airPositionsNotUsed = new HashSet<Vector3Int>(airPositions.SelectMany(x => x));
            //LogStats("airPositionsNotUsed");
            var zones = GetZones(airNodes, size);
            LogStats("GetZones");
            RemoveInaccessibleZones(map, zones);
            LogStats("RemoveInaccessibleZones");

            return (map, floorlessMap);

            void LogStats(string name)
            {
                Log.Debug($"{name}: {stopwatch.Elapsed}");
                stopwatch.Restart();
            }
        }

        private List<List<Vector3Int>> GetZones(bool[,,] airPositionsNotUsed, Vector3Int size)
        {
            List<List<Vector3Int>> zones = new List<List<Vector3Int>>();

            Queue<Vector3Int> queue = new Queue<Vector3Int>();

            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    for (int z = 0; z < size.z; z++)
                    {
                        if (airPositionsNotUsed[x, y, z])
                        {
                            var currentZone = new List<Vector3Int>();

                            Vector3Int rootPosition = new Vector3Int(x, y, z);
                            queue.Enqueue(rootPosition);

                            while (queue.Count > 0)
                            {
                                var nodePosition = queue.Dequeue();

                                currentZone.Add(nodePosition);

                                for (int i = 0; i < 6; i++)
                                {
                                    var neighbor = _adjacentPositions[i] + nodePosition;
                                    if (neighbor.x >= 0
                                        && neighbor.y >= 0
                                        && neighbor.z >= 0
                                        && neighbor.x < size.x
                                        && neighbor.y < size.y
                                        && neighbor.z < size.z
                                        && airPositionsNotUsed[neighbor.x, neighbor.y, neighbor.z])
                                    {
                                        queue.Enqueue(neighbor);
                                        airPositionsNotUsed[neighbor.x, neighbor.y, neighbor.z] = false;
                                    }
                                }
                            }

                            zones.Add(currentZone);
                        }
                    }
                }
            }

            return zones;
        }

        private void RemoveInaccessibleZones(float[,,] map, List<List<Vector3Int>> zones)
        {
            var zonesInaccessible = zones
                .OrderByDescending(x => x.Count)
                .Skip(1)
                .ToList();

            Parallel.ForEach(zonesInaccessible, zone =>
            {
                for (int i = 0; i < zone.Count; i++)
                {
                    Vector3Int position = zone[i];
                    map[position.x, position.y, position.z] = 1;
                }
            });
        }
    }
}

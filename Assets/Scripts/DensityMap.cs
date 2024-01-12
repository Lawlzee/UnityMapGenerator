using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts
{
    public class MapDensity
    {
        private readonly float[,,] _map;
        private readonly int _sampleWidth;
        private readonly float _groundYOffset;

        public MapDensity(float[,,] map, int sampleWidth, float groundYOffset)
        {
            _map = map;
            _sampleWidth = sampleWidth;
            _groundYOffset = groundYOffset;
        }

        public float GetDensity(Vector3 pos, bool isGround)
        {
            if (isGround)
            {
                pos.y += _groundYOffset;
            }
            Vector3 scaledPos = (pos / _sampleWidth) - new Vector3(0.5f, 0.5f, 0.5f);

            int x0 = Math.Max(0, Mathf.FloorToInt(scaledPos.x));
            int x1 = Math.Min(_map.GetLength(0) - 1, Mathf.CeilToInt(scaledPos.x));
            float dx = scaledPos.x - x0;

            int y0 = Math.Max(0, Mathf.FloorToInt(scaledPos.y));
            int y1 = Math.Min(_map.GetLength(1) - 1, Mathf.CeilToInt(scaledPos.y));
            float dy = scaledPos.y - y0;

            int z0 = Math.Max(0, Mathf.FloorToInt(scaledPos.z));
            int z1 = Math.Min(_map.GetLength(2) - 1, Mathf.CeilToInt(scaledPos.z));
            float dz = scaledPos.z - z0;

            try
            {
                float sample0 = _map[x0, y0, z0];
                float sample1 = _map[x0, y0, z1];
                float sample2 = _map[x0, y1, z0];
                float sample3 = _map[x0, y1, z1];
                float sample4 = _map[x1, y0, z0];
                float sample5 = _map[x1, y0, z1];
                float sample6 = _map[x1, y1, z0];
                float sample7 = _map[x1, y1, z1];

                float lerpz0 = Mathf.Lerp(sample0, sample1, dz);
                float lerpz1 = Mathf.Lerp(sample2, sample3, dz);
                float lerpz2 = Mathf.Lerp(sample4, sample5, dz);
                float lerpz3 = Mathf.Lerp(sample6, sample7, dz);

                float lerpy0 = Mathf.Lerp(lerpz0, lerpz1, dy);
                float lerpy1 = Mathf.Lerp(lerpz2, lerpz3, dy);

                float density = Mathf.Lerp(lerpy0, lerpy1, dx);
                return density;
            }
            catch
            {
                Debug.Log(scaledPos);
                Debug.Log(_map.GetLength(0));
                Debug.Log(_map.GetLength(1));
                Debug.Log(_map.GetLength(2));


                throw;
            }

            
        }
    }

    [Serializable]
    public class DensityMap
    {
        [Range(0, 25)]
        public int sampleWidth = 7;
        
        [Range(0, 5)]
        public float groundYOffset = 2;

        [Range(0f, 1f)]
        public float maxChestDensity = 0.9f;
        [Range(0f, 1f)]
        public float maxShrineDensity = 0.8f;
        [Range(0f, 1f)]
        public float minTeleporterDensity = 0f;
        [Range(0f, 1f)]
        public float maxTeleporterDensity = 0.3f;

        [Range(0f, 1f)]
        public float minNewtDensity = 0.5f;
        [Range(0f, 1f)]
        public float maxNewtDensity = 1f;

        public HullDensity air = new HullDensity
        {
            maxHumanDensity = 0.8f,
            maxGolemDensity = 0.7f,
            maxBeetleQueenDensity = 0.6f
        };
        public HullDensity ground = new HullDensity
        {
            maxHumanDensity = 0.8f,
            maxGolemDensity = 0.7f,
            maxBeetleQueenDensity = 0.6f
        };

        public MapDensity Create(bool[,,] map)
        {
            int width = map.GetLength(0);
            int height = map.GetLength(1);
            int depth = map.GetLength(2);

            float sampleWidthF = sampleWidth;

            float cellPerSample = sampleWidth * sampleWidth * sampleWidth;

            int newWidth = Mathf.CeilToInt(width / sampleWidthF);
            int newHeight = Mathf.CeilToInt(height / sampleWidthF);
            int newDepth = Mathf.CeilToInt(depth / sampleWidthF);

            float[,,] densityMap = new float[newWidth, newHeight, newDepth];

            Parallel.For(0, newWidth, posX =>
            {
                for (int posY = 0; posY < newHeight; posY++)
                {
                    for (int posZ = 0; posZ < newDepth; posZ++)
                    {
                        int airCount = 0;

                        for (int dx = 0; dx < sampleWidth; dx++)
                        {
                            int x = posX * sampleWidth + dx;

                            if (x >= width)
                            {
                                break;
                            }

                            for (int dy = 0; dy < sampleWidth; dy++)
                            {
                                int y = posY * sampleWidth + dy;

                                if (y >= height)
                                {
                                    break;
                                }

                                for (int dz = 0; dz < sampleWidth; dz++)
                                {
                                    int z = posZ * sampleWidth + dz;

                                    if (z >= depth)
                                    {
                                        break;
                                    }

                                    if (!map[x, y, z])
                                    {
                                        airCount++;
                                    }
                                }
                            }
                        }

                        densityMap[posX, posY, posZ] = 1 - (airCount / cellPerSample);
                    }
                }
            });

            return new MapDensity(densityMap, sampleWidth, groundYOffset);

            //float totalDistance = 0;
            //
            //for (int dx = -sampleWidth; dx <= sampleWidth; dx++)
            //{
            //    for (int dy = -sampleWidth; dy <= sampleWidth; dy++)
            //    {
            //        for (int dz = -sampleWidth; dz <= sampleWidth; dz++)
            //        {
            //            totalDistance += Math.Abs(dx) + Math.Abs(dy) + Math.Abs(dz);
            //        }
            //    }
            //}
            //
            //float min = float.MaxValue;
            //float max = float.MinValue;
            //
            //Parallel.For(0, width, posX =>
            //{
            //    for (int posY = 0; posY < height; posY++)
            //    {
            //        for (int posZ = 0; posZ < depth; posZ++)
            //        {
            //            //if (map[posX, posY, posZ])
            //            //{
            //            //    densityMap[posX, posY, posZ] = 1;
            //            //}
            //            //else
            //            //{
            //                int emptyness = 0;
            //
            //                for (int dx = -sampleWidth; dx <= sampleWidth; dx++)
            //                {
            //                    int x = posX + dx;
            //                    bool inBoundX = x >= 0 && x < width;
            //                    int adx = Math.Abs(dx);
            //
            //                    for (int dy = -sampleWidth; dy <= sampleWidth; dy++)
            //                    {
            //                        int y = posY + dy;
            //                        bool inBoundY = y >= 0 && y < height;
            //                        int ady = Math.Abs(dy);
            //
            //                        for (int dz = -sampleWidth; dz <= sampleWidth; dz++)
            //                        {
            //                            int z = posZ + dz;
            //                            bool inBoundZ = z >= 0 && z < depth;
            //                            int adz = Math.Abs(dz);
            //
            //                            if (inBoundX 
            //                                && inBoundY 
            //                                && inBoundZ
            //                                && !map[x, y, z])
            //                            {
            //                                emptyness += adx + ady + adz;
            //                            }
            //                            
            //                        }
            //                    }
            //                }
            //
            //                float value = 1 - (emptyness / totalDistance);
            //                densityMap[posX, posY, posZ] = value;
            //
            //                min = Math.Min(min, value);
            //                max = Math.Max(max, value);
            //            //}
            //        }
            //    }
            //});
            //
            //Debug.Log(min);
            //Debug.Log(max);
            //
            //return densityMap;
        }

        [Serializable]
        public class HullDensity
        {
            [Range(0f, 1f)]
            public float maxHumanDensity;
            [Range(0f, 1f)]
            public float maxGolemDensity;
            [Range(0f, 1f)]
            public float maxBeetleQueenDensity;
        }
    }
}

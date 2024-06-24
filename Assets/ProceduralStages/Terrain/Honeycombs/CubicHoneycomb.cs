using ProceduralStages;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace ProceduralStages
{
    [CreateAssetMenu(fileName = "CubicHoneycomb", menuName = "ProceduralStages/CubicHoneycomb", order = 10)]
    public class CubicHoneycomb : ScriptableObject
    {
        public Vector3Int size;

        public Vector3Int blockGrid;
        public BlockShape[] blockShapes;
        public int blockFillMaxIterations;
        public int maxBlockInsert;
        public int autofillBlocksLeft;


        public Vector3Int debugPos;

        public ulong seed;

        [SerializeField]
        [HideInInspector]
        private Voronoi3DResult[] voronoi;

        [ContextMenu("Bake")]
        public void Bake()
        {
            Xoroshiro128Plus rng = new Xoroshiro128Plus(seed);

            voronoi = new Voronoi3DResult[size.x * size.y * size.z];

            List<Vector3> blockCenters = new List<Vector3>();
            List<Vector3> blockSizes = new List<Vector3>();
            int[,,] blockMap = new int[blockGrid.x, blockGrid.y, blockGrid.z];

            for (int x = 0; x < blockGrid.x; x++)
            {
                for (int y = 0; y < blockGrid.y; y++)
                {
                    for (int z = 0; z < blockGrid.z; z++)
                    {
                        blockMap[x, y, z] = -1;
                    }
                }
            }

            int blocksLeft = blockGrid.x * blockGrid.y * blockGrid.z;
            Log.Debug("blocksLeft: " + blocksLeft);

            int i = 0;
            for (; i < blockFillMaxIterations && autofillBlocksLeft <= blocksLeft; i++)
            {
                var blockShape = blockShapes[rng.RangeInt(0, blockShapes.Length)];

                Vector3Int blockSize = new Vector3Int(
                    rng.RangeInt(blockShape.minSize.x, blockShape.maxSize.x + 1),
                    rng.RangeInt(blockShape.minSize.y, blockShape.maxSize.y + 1),
                    rng.RangeInt(blockShape.minSize.z, blockShape.maxSize.z + 1));

                for (int j = 0; j < maxBlockInsert; j++)
                {
                    bool hasRoom = true;
                    Vector3Int targetPosition = new Vector3Int(
                        rng.RangeInt(0, blockGrid.x),
                        rng.RangeInt(0, blockGrid.y),
                        rng.RangeInt(0, blockGrid.z));

                    for (int x = 0; hasRoom && x < blockSize.x; x++)
                    {
                        int posX = (targetPosition.x + x) % blockGrid.x;

                        for (int y = 0; hasRoom && y < blockSize.y; y++)
                        {
                            int posY = (targetPosition.y + y) % blockGrid.y;

                            for (int z = 0; hasRoom && z < blockSize.z; z++)
                            {
                                int posZ = (targetPosition.z + z) % blockGrid.z;

                                if (blockMap[posX, posY, posZ] != -1)
                                {
                                    hasRoom = false;
                                }
                            }
                        }
                    }

                    if (hasRoom)
                    {
                        blocksLeft -= blockSize.x * blockSize.y * blockSize.z;

                        for (int x = 0; hasRoom && x < blockSize.x; x++)
                        {
                            int posX = (targetPosition.x + x) % blockGrid.x;

                            for (int y = 0; hasRoom && y < blockSize.y; y++)
                            {
                                int posY = (targetPosition.y + y) % blockGrid.y;

                                for (int z = 0; hasRoom && z < blockSize.z; z++)
                                {
                                    int posZ = (targetPosition.z + z) % blockGrid.z;

                                    blockMap[posX, posY, posZ] = blockCenters.Count;
                                }
                            }
                        }

                        var center = targetPosition + ((Vector3)blockSize) / 2f;
                        Log.Debug(center);
                        blockCenters.Add(new Vector3(
                            center.x % blockGrid.x,
                            center.y % blockGrid.y,
                            center.z % blockGrid.z));

                        blockSizes.Add(blockSize);
                    }
                }
            }


            Log.Debug("blockCenters.Count: " + blockCenters.Count);
            Log.Debug("i: " + i);
            Log.Debug("blocksLeft: " + blocksLeft);

            for (int x = 0; x < blockGrid.x; x++)
            {
                for (int y = 0; y < blockGrid.y; y++)
                {
                    for (int z = 0; z < blockGrid.z; z++)
                    {
                        if (blockMap[x, y, z] == -1)
                        {
                            blockMap[x, y, z] = blockCenters.Count;

                            blockCenters.Add(new Vector3(
                                x + 0.5f,
                                y + 0.5f,
                                z + 0.5f));

                            blockSizes.Add(Vector3.one);
                        }
                    }
                }
            }

            Log.Debug("blockCenters.Count: " + blockCenters.Count);

            Vector3 scale = new Vector3(
                blockGrid.x / (float)size.x,
                blockGrid.y / (float)size.y,
                blockGrid.z / (float)size.z);

            Vector3 scaleReciprocal = new Vector3(
                1 / scale.x,
                1 / scale.y,
                1 / scale.z);

            Parallel.For(0, size.x, x =>
            {
                for (int y = 0; y < size.y; y++)
                {
                    for (int z = 0; z < size.z; z++)
                    {
                        Vector3 uvw = new Vector3(
                            x * scale.x,
                            y * scale.y,
                            z * scale.z);

                        Vector3Int uvwIntegral = new Vector3Int(
                            Mathf.FloorToInt(uvw.x),
                            Mathf.FloorToInt(uvw.y),
                            Mathf.FloorToInt(uvw.z));

                        Vector3 uvwFractional = uvw - uvwIntegral;

                        int blockIndex = blockMap[uvwIntegral.x, uvwIntegral.y, uvwIntegral.z];
                        Vector3 blockCenter = DemoduloVector(blockCenters[blockIndex], uvw, blockGrid);
                        Vector3 blockSize = blockSizes[blockIndex];


                        //for (int sign = -1; sign <= 1; sign += 2)
                        //{
                        //    for (int dimension = 0; dimension < 3; dimension++)
                        //    {
                        //        for (int offset = 1; offset < blockGrid[dimension]; offset++)
                        //        {
                        //            int neighborBlockIndex = blockMap[(uvwIntegral[dimension] + sign * offset) % blockGrid.x, y, z];
                        //            if (neighborBlockIndex != blockIndex)
                        //            {
                        //                Vector3 neighborBlockCenter = blockCenters[neighborBlockIndex];
                        //                float distance = 1 - (sign * offset - uvwIntegral[dimension]) / (uvwIntegral[dimension] + offset - neighborBlockCenter.x);
                        //                break;
                        //            }
                        //        }
                        //    }
                        //}


                        Vector3 delta = uvw - blockCenter;

                        float distanceX = Mathf.Abs(delta.x) / blockSize.x;
                        float distanceY = Mathf.Abs(delta.y) / blockSize.y;
                        float distanceZ = Mathf.Abs(delta.z) / blockSize.z;

                        float distance = Mathf.Max(
                            distanceX,
                            distanceY,
                            distanceZ);

                        int neighborBlockIndex = 0;

                        if (distance == distanceX)
                        {
                            if (delta.x > 0)
                            {
                                for (int offset = 1; offset < blockSize.x; offset++)
                                {
                                    int posX = (uvwIntegral.x + offset) % blockGrid.x;
                                    neighborBlockIndex = blockMap[posX, uvwIntegral.y, uvwIntegral.z];
                                    if (neighborBlockIndex != blockIndex)
                                    {
                                        break;
                                    }
                                }
                            }

                            else
                            {
                                for (int offset = 1; offset < blockSize.x; offset++)
                                {
                                    int posX = ((uvwIntegral.x - offset) + blockGrid.x) % blockGrid.x;
                                    neighborBlockIndex = blockMap[posX, uvwIntegral.y, uvwIntegral.z];
                                    if (neighborBlockIndex != blockIndex)
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                        else if (distance == distanceX)
                        {
                            if (delta.y > 0)
                            {
                                for (int offset = 1; offset < blockSize.y; offset++)
                                {
                                    int posY = (uvwIntegral.y + offset) % blockGrid.y;
                                    neighborBlockIndex = blockMap[uvwIntegral.x, posY, uvwIntegral.z];
                                    if (neighborBlockIndex != blockIndex)
                                    {
                                        break;
                                    }
                                }
                            }

                            else
                            {
                                for (int offset = 1; offset < blockSize.y; offset++)
                                {
                                    int posY = ((uvwIntegral.y - offset) + blockGrid.y) % blockGrid.y;
                                    neighborBlockIndex = blockMap[uvwIntegral.x, posY, uvwIntegral.z];
                                    if (neighborBlockIndex != blockIndex)
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (delta.z > 0)
                            {
                                for (int offset = 1; offset < blockSize.z; offset++)
                                {
                                    int posZ = (uvwIntegral.z + offset) % blockGrid.z;
                                    neighborBlockIndex = blockMap[uvwIntegral.x, uvwIntegral.y, posZ];
                                    if (neighborBlockIndex != blockIndex)
                                    {
                                        break;
                                    }
                                }
                            }

                            else
                            {
                                for (int offset = 1; offset < blockSize.z; offset++)
                                {
                                    int posZ = ((uvwIntegral.z - offset) + blockGrid.z) % blockGrid.z;
                                    neighborBlockIndex = blockMap[uvwIntegral.x, uvwIntegral.y, posZ];
                                    if (neighborBlockIndex != blockIndex)
                                    {
                                        break;
                                    }
                                }
                            }
                        }


                        float minDistance = distance;
                        Vector3 neighborCenter = DemoduloVector(blockCenters[neighborBlockIndex], uvw, blockGrid);

                        //for (int xOffset = 1; xOffset < blockGrid.x; xOffset++)
                        //{
                        //    int neighborBlockIndex = blockMap[(uvwIntegral.x + xOffset) % blockGrid.x, uvwIntegral.y, uvwIntegral.z];
                        //    if (neighborBlockIndex != blockIndex)
                        //    {
                        //        neighborCenter = blockCenters[neighborBlockIndex];
                        //        minDistance = (xOffset - uvwFractional.x) / (uvwIntegral.x + xOffset - blockCenter.x);
                        //
                        //        if (x == debugPos.x && y == debugPos.y && z == debugPos.z)
                        //        {
                        //            Log.Debug("distance: " + minDistance);
                        //        }
                        //
                        //        //if (x == 0 && y == 0 && z == 0)
                        //        //{
                        //        //    Log.Debug("xOffset: " + xOffset);
                        //        //    Log.Debug("uvwFractional.x: " + uvwFractional.x);
                        //        //    Log.Debug("uvwIntegral.x: " + uvwIntegral.x);
                        //        //    Log.Debug("blockCenter.x: " + blockCenter.x);
                        //        //    Log.Debug("blockCenter: " + blockCenter);
                        //        //    Log.Debug("minDistance: " + minDistance);
                        //        //}
                        //
                        //        break;
                        //    }
                        //}
                        //
                        //for (int xOffset = 1; xOffset < blockGrid.x; xOffset++)
                        //{
                        //    int neighborBlockIndex = blockMap[(((uvwIntegral.x - xOffset) % blockGrid.x) + blockGrid.x) % blockGrid.x, uvwIntegral.y, uvwIntegral.z];
                        //    if (neighborBlockIndex != blockIndex)
                        //    {
                        //        float distance = (uvwFractional.x - xOffset + 1) / (uvwIntegral.x + blockCenter.x - xOffset + 1);
                        //
                        //        if (x == debugPos.x && y == debugPos.y && z == debugPos.z)
                        //        {
                        //            Log.Debug("distance: " + distance);
                        //        }
                        //
                        //        //if (x == 0 && y == 0 && z == 0)
                        //        //{
                        //        //    Log.Debug("xOffset: " + xOffset);
                        //        //    Log.Debug("uvwFractional.x: " + uvwFractional.x);
                        //        //    Log.Debug("uvwIntegral.x: " + uvwIntegral.x);
                        //        //    Log.Debug("blockCenter.x: " + blockCenter.x);
                        //        //    Log.Debug("blockCenter: " + blockCenter);
                        //        //    Log.Debug("distance: " + distance);
                        //        //}
                        //
                        //        if (distance < minDistance)
                        //        {
                        //            minDistance = distance;
                        //            neighborCenter = blockCenters[neighborBlockIndex];
                        //        }
                        //
                        //        break;
                        //    }
                        //}
                        //
                        //
                        //for (int yOffset = 1; yOffset < blockGrid.y; yOffset++)
                        //{
                        //    int neighborBlockIndex = blockMap[uvwIntegral.x, (uvwIntegral.y + yOffset) % blockGrid.y, uvwIntegral.z];
                        //    if (neighborBlockIndex != blockIndex)
                        //    {
                        //        float distance = (yOffset - uvwFractional.y) / (uvwIntegral.y + yOffset - blockCenter.y);
                        //
                        //
                        //        if (x == debugPos.x && y == debugPos.y && z == debugPos.z)
                        //        {
                        //            Log.Debug("distance: " + distance);
                        //        }
                        //
                        //        if (distance < minDistance)
                        //        {
                        //            minDistance = distance;
                        //            neighborCenter = blockCenters[neighborBlockIndex];
                        //        }
                        //
                        //        break;
                        //    }
                        //}
                        //
                        //for (int yOffset = 1; yOffset < blockGrid.y; yOffset++)
                        //{
                        //    int neighborBlockIndex = blockMap[uvwIntegral.x, (((uvwIntegral.y - yOffset) % blockGrid.y) + blockGrid.y) % blockGrid.y, uvwIntegral.z];
                        //    if (neighborBlockIndex != blockIndex)
                        //    {
                        //        float distance = (uvwFractional.y - yOffset + 1) / (uvwIntegral.y + blockCenter.y - yOffset + 1);
                        //
                        //
                        //        if (x == debugPos.x && y == debugPos.y && z == debugPos.z)
                        //        {
                        //            Log.Debug("distance: " + distance);
                        //        }
                        //
                        //        if (distance < minDistance)
                        //        {
                        //            minDistance = distance;
                        //            neighborCenter = blockCenters[neighborBlockIndex];
                        //        }
                        //
                        //        break;
                        //    }
                        //}
                        //
                        //for (int zOffset = 1; zOffset < blockGrid.z; zOffset++)
                        //{
                        //    int neighborBlockIndex = blockMap[uvwIntegral.x, uvwIntegral.y, (uvwIntegral.z + zOffset) % blockGrid.z];
                        //    if (neighborBlockIndex != blockIndex)
                        //    {
                        //        float distance = (zOffset - uvwFractional.z) / (uvwIntegral.z + zOffset - blockCenter.z);
                        //
                        //
                        //        if (x == debugPos.x && y == debugPos.y && z == debugPos.z)
                        //        {
                        //            Log.Debug("distance: " + distance);
                        //        }
                        //
                        //        if (distance < minDistance)
                        //        {
                        //            minDistance = distance;
                        //            neighborCenter = blockCenters[neighborBlockIndex];
                        //        }
                        //
                        //        break;
                        //    }
                        //}
                        //
                        //for (int zOffset = 1; zOffset < blockGrid.z; zOffset++)
                        //{
                        //    int neighborBlockIndex = blockMap[uvwIntegral.x, uvwIntegral.y, (((uvwIntegral.z - zOffset) % blockGrid.z) + blockGrid.z) % blockGrid.z];
                        //    if (neighborBlockIndex != blockIndex)
                        //    {
                        //        //float distance = (uvwFractional.z - zOffset + 1) / (uvwIntegral.z - blockCenter.z - zOffset + 1);
                        //        float distance = (uvw.z - blockCenter.z) / (zOffset - blockCenter.z % 1);
                        //
                        //
                        //        if (x == debugPos.x && y == debugPos.y && z == debugPos.z)
                        //        {
                        //            Log.Debug("distance: " + distance);
                        //            Log.Debug("zOffset: " + zOffset);
                        //            Log.Debug("uvwFractional.z: " + uvwFractional.z);
                        //            Log.Debug("uvwIntegral.z: " + uvwIntegral.z);
                        //            Log.Debug("blockCenter.z: " + blockCenter.z);
                        //            Log.Debug("blockCenter: " + blockCenter);
                        //            Log.Debug("distance: " + distance);
                        //        }
                        //
                        //        if (distance < minDistance)
                        //        {
                        //            minDistance = distance;
                        //            neighborCenter = blockCenters[neighborBlockIndex];
                        //        }
                        //
                        //        break;
                        //    }
                        //}
                        //
                        //if (x == debugPos.x && y == debugPos.y && z == debugPos.z)
                        //{
                        //    Log.Debug("minDistance: " + minDistance);
                        //}

                        Vector3 displacement1 = new Vector3(
                            scaleReciprocal.x * (blockCenter.x - uvw.x),
                            scaleReciprocal.y * (blockCenter.y - uvw.y),
                            scaleReciprocal.z * (blockCenter.z - uvw.z));

                        Vector3 displacement2 = new Vector3(
                            scaleReciprocal.x * (neighborCenter.x - uvw.x),
                            scaleReciprocal.y * (neighborCenter.y - uvw.y),
                            scaleReciprocal.z * (neighborCenter.z - uvw.z));

                        float weight = 0.5f * (1 - minDistance);

                        voronoi[x * size.y * size.z + y * size.z + z] = new Voronoi3DResult
                        {
                            displacement1 = displacement1,
                            displacement2 = displacement2,
                            weight = minDistance
                        };
                    }
                }
            });

            Log.Debug("Voronoi baked");
        }

        private Vector3 DemoduloVector(Vector3 vector, Vector3 basis, Vector3 spaceSize)
        {
            Vector3 delta = vector - basis;
            if (delta.x > spaceSize.x / 2f)
            {
                vector.x -= spaceSize.x;
            }
            else if (delta.x < -spaceSize.x / 2f)
            {
                vector.x += spaceSize.x;
            }

            if (delta.y > spaceSize.y / 2f)
            {
                vector.y -= spaceSize.y;
            }
            else if (delta.y < -spaceSize.y / 2f)
            {
                vector.y += spaceSize.y;
            }

            if (delta.z > size.z / 2f)
            {
                vector.z -= spaceSize.z;
            }
            else if (delta.z < -spaceSize.z / 2f)
            {
                vector.z += spaceSize.z;
            }

            return vector;
        }

        public Voronoi3DResult this[int x, int y, int z]
        {
            get
            {
                x = ((x % size.x) + size.x) % size.x;
                y = ((y % size.y) + size.y) % size.y;
                z = ((z % size.z) + size.z) % size.z;

                return voronoi[x * size.y * size.z + y * size.z + z];
            }
        }
    }
}

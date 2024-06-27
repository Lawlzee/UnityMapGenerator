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
    [CreateAssetMenu(fileName = "SquareHoneycomb", menuName = "ProceduralStages/SquareHoneycomb", order = 10)]
    public class SquareHoneycomb : ScriptableObject
    {
        public Vector2Int size;

        public Vector2Int blockGrid;
        public BlockShape2[] blockShapes;
        public int blockFillMaxIterations;
        public int maxBlockInsert;
        public int autofillBlocksLeft;

        public ThreadSafeCurve roundingCurve;

        public ulong seed;

        [SerializeField]
        [HideInInspector]
        private Voronoi2DResult[] voronoi;

        [ContextMenu("Bake")]
        public void Bake()
        {
            Xoroshiro128Plus rng = new Xoroshiro128Plus(seed);

            voronoi = new Voronoi2DResult[size.x * size.y];

            List<Vector2> blockCenters = new List<Vector2>();
            List<Vector2> blockSizes = new List<Vector2>();
            int[,] blockMap = new int[blockGrid.x, blockGrid.y];

            for (int x = 0; x < blockGrid.x; x++)
            {
                for (int y = 0; y < blockGrid.y; y++)
                {
                    blockMap[x, y] = -1;
                }
            }

            int blocksLeft = blockGrid.x * blockGrid.y;
            Log.Debug("blocksLeft: " + blocksLeft);

            int i = 0;
            for (; i < blockFillMaxIterations && autofillBlocksLeft <= blocksLeft; i++)
            {
                var blockShape = blockShapes[rng.RangeInt(0, blockShapes.Length)];

                Vector2Int blockSize = new Vector2Int(
                    rng.RangeInt(blockShape.minSize.x, blockShape.maxSize.x + 1),
                    rng.RangeInt(blockShape.minSize.y, blockShape.maxSize.y + 1));

                for (int j = 0; j < maxBlockInsert; j++)
                {
                    bool hasRoom = true;
                    Vector2Int targetPosition = new Vector2Int(
                        rng.RangeInt(0, blockGrid.x),
                        rng.RangeInt(0, blockGrid.y));

                    for (int x = 0; hasRoom && x < blockSize.x; x++)
                    {
                        int posX = (targetPosition.x + x) % blockGrid.x;

                        for (int y = 0; hasRoom && y < blockSize.y; y++)
                        {
                            int posY = (targetPosition.y + y) % blockGrid.y;

                            if (blockMap[posX, posY] != -1)
                            {
                                hasRoom = false;
                            }
                        }
                    }

                    if (hasRoom)
                    {
                        blocksLeft -= blockSize.x * blockSize.y;

                        for (int x = 0; hasRoom && x < blockSize.x; x++)
                        {
                            int posX = (targetPosition.x + x) % blockGrid.x;

                            for (int y = 0; hasRoom && y < blockSize.y; y++)
                            {
                                int posY = (targetPosition.y + y) % blockGrid.y;

                                blockMap[posX, posY] = blockCenters.Count;
                            }
                        }

                        var center = targetPosition + ((Vector2)blockSize) / 2f;
                        Log.Debug(center);
                        blockCenters.Add(new Vector2(
                            center.x % blockGrid.x,
                            center.y % blockGrid.y));

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
                    if (blockMap[x, y] == -1)
                    {
                        blockMap[x, y] = blockCenters.Count;

                        blockCenters.Add(new Vector2(
                            x + 0.5f,
                            y + 0.5f));

                        blockSizes.Add(Vector2.one);
                    }
                }
            }

            Log.Debug("blockCenters.Count: " + blockCenters.Count);

            Vector2 scale = new Vector2(
                blockGrid.x / (float)size.x,
                blockGrid.y / (float)size.y);

            Vector2 scaleReciprocal = new Vector2(
                1 / scale.x,
                1 / scale.y);

            Parallel.For(0, size.x, x =>
            {
                for (int y = 0; y < size.y; y++)
                {
                    Vector2 uv = new Vector2(
                        x * scale.x,
                        y * scale.y);

                    Vector2Int uvIntegral = new Vector2Int(
                        Mathf.FloorToInt(uv.x),
                        Mathf.FloorToInt(uv.y));

                    Vector2 uvwFractional = uv - uvIntegral;

                    int blockIndex = blockMap[uvIntegral.x, uvIntegral.y];
                    Vector2 blockCenter = DemoduloVector(blockCenters[blockIndex], uv, blockGrid);
                    Vector2 blockSize = blockSizes[blockIndex];

                    Vector2 delta = uv - blockCenter;

                    float distanceX = Mathf.Abs(delta.x) / blockSize.x;
                    float distanceY = Mathf.Abs(delta.y) / blockSize.y;

                    float maxDistance = Mathf.Max(distanceX, distanceY);
                    float minDistance = Mathf.Min(distanceX, distanceY);

                    int neighborBlockIndex = 0;

                    if (maxDistance == distanceX)
                    {
                        if (delta.x > 0)
                        {
                            for (int offset = 1; offset < blockSize.x; offset++)
                            {
                                int posX = (uvIntegral.x + offset) % blockGrid.x;
                                neighborBlockIndex = blockMap[posX, uvIntegral.y];
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
                                int posX = ((uvIntegral.x - offset) + blockGrid.x) % blockGrid.x;
                                neighborBlockIndex = blockMap[posX, uvIntegral.y];
                                if (neighborBlockIndex != blockIndex)
                                {
                                    break;
                                }
                            }
                        }
                    }
                    else if (maxDistance == distanceX)
                    {
                        if (delta.y > 0)
                        {
                            for (int offset = 1; offset < blockSize.y; offset++)
                            {
                                int posY = (uvIntegral.y + offset) % blockGrid.y;
                                neighborBlockIndex = blockMap[uvIntegral.x, posY];
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
                                int posY = ((uvIntegral.y - offset) + blockGrid.y) % blockGrid.y;
                                neighborBlockIndex = blockMap[uvIntegral.x, posY];
                                if (neighborBlockIndex != blockIndex)
                                {
                                    break;
                                }
                            }
                        }
                    }

                    Vector2 neighborCenter = DemoduloVector(blockCenters[neighborBlockIndex], uv, blockGrid);

                    Vector2 displacement1 = new Vector2(
                        scaleReciprocal.x * (blockCenter.x - uv.x),
                        scaleReciprocal.y * (blockCenter.y - uv.y));

                    Vector2 displacement2 = new Vector2(
                        scaleReciprocal.x * (neighborCenter.x - uv.x),
                        scaleReciprocal.y * (neighborCenter.y - uv.y));

                    float weight = maxDistance + roundingCurve.Evaluate(minDistance / maxDistance);

                    voronoi[x * size.y + y] = new Voronoi2DResult
                    {
                        displacement1 = displacement1,
                        displacement2 = displacement2,
                        weight = weight
                    };
                }
            });

            Log.Debug("Voronoi baked");
        }

        private Vector2 DemoduloVector(Vector2 vector, Vector2 basis, Vector2 spaceSize)
        {
            Vector2 delta = vector - basis;
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

            return vector;
        }

        public Voronoi2DResult this[int x, int y]
        {
            get
            {
                x = ((x % size.x) + size.x) % size.x;
                y = ((y % size.y) + size.y) % size.y;

                return voronoi[x * size.y + y];
            }
        }
    }
}

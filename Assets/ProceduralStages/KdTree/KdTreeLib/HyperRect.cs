using UnityEngine;

namespace KdTree
{
    public struct HyperRect
    {
        private Vector3 minPoint;
        public Vector3 MinPoint
        {
            get
            {
                return minPoint;
            }
            set
            {
                minPoint = value;
            }
        }

        private Vector3 maxPoint;
        public Vector3 MaxPoint
        {
            get
            {
                return maxPoint;
            }
            set
            {
                maxPoint = value;
            }
        }

        public static HyperRect Infinite()
        {
            var rect = new HyperRect
            {
                MinPoint = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue),
                MaxPoint = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue)
            };

            return rect;
        }

        public Vector3 GetClosestPoint(Vector3 toPoint)
        {
            Vector3 closest = Vector3.zero;

            for (var dimension = 0; dimension < 3; dimension++)
            {
                if (minPoint[dimension].CompareTo(toPoint[dimension]) > 0)
                {
                    closest[dimension] = minPoint[dimension];
                }
                else if (maxPoint[dimension].CompareTo(toPoint[dimension]) < 0)
                {
                    closest[dimension] = maxPoint[dimension];
                }
                else
                    // Point is within rectangle, at least on this dimension
                    closest[dimension] = toPoint[dimension];
            }

            return closest;
        }

        public HyperRect Clone()
        {
            var rect = new HyperRect
            {
                MinPoint = MinPoint,
                MaxPoint = MaxPoint
            };
            return rect;
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProceduralStages
{
    public class Octree<T>
    {
        private enum State : byte
        {
            HasPlaceInBucket,
            BucketFull,
            HasOctants
        }

        public struct Point
        {
            public Vector3 Position;
            public T Value;
        }

        private readonly Bounds _bounds;
        private Octree<T>[] _octants;
        private Point[] _points;
        private int _pointCount;

        public Octree(Bounds bounds, int bucketSize)
        {
            _bounds = bounds;
            _points = new Point[bucketSize];
        }

        private struct QueuedPoint
        {
            public Octree<T> Octant;
            public Point Point;
        }

        private State GetState()
        {
            if (_octants != null)
            {
                return State.HasOctants;
            }

            if (_pointCount < _points.Length)
            {
                return State.HasPlaceInBucket;
            }

            return State.BucketFull;
        }

        public void Add(Point point)
        {
            if (GetState() == State.HasPlaceInBucket)
            {
                _points[_pointCount] = point;
                _pointCount++;
                return;
            }

            Queue<QueuedPoint> queue = new Queue<QueuedPoint>();
            queue.Enqueue(new QueuedPoint()
            {
                Octant = this,
                Point = point
            });

            while (queue.Count > 0)
            {
                QueuedPoint current = queue.Dequeue();
                Octree<T> currentOctree = current.Octant;
                point = current.Point;

                var state = currentOctree.GetState();

                if (state == State.HasPlaceInBucket)
                {
                    currentOctree._points[currentOctree._pointCount] = point;
                    currentOctree._pointCount++;
                }
                else
                {
                    if (state == State.BucketFull)
                    {
                        currentOctree._octants = currentOctree.CreateOctants();

                        for (int i = 0; i < currentOctree._pointCount; i++)
                        {
                            var subPoint = currentOctree._points[i];
                            int octantIndex = currentOctree.GetOctantIndex(subPoint.Position);

                            queue.Enqueue(new QueuedPoint()
                            {
                                Octant = currentOctree._octants[octantIndex],
                                Point = subPoint
                            });
                        }

                        currentOctree._points = null;
                        currentOctree._pointCount = 0;
                    }

                    int octantIndex2 = currentOctree.GetOctantIndex(point.Position);
                    queue.Enqueue(new QueuedPoint()
                    {
                        Octant = currentOctree._octants[octantIndex2],
                        Point = point
                    });
                }
            }
        }

        public (float DistanceSqr, Point Point) GetNearestNeighbour(Vector3 position)
        {
            Point minPoint = default;
            float minDistance = float.MaxValue;

            var queue = new PriorityQueue<Octree<T>, float>(8);
            queue.Enqueue(this, 0);

            while (queue.TryDequeue(out Octree<T> octant, out float currentOctantMinDistanceSqr))
            {
                if (currentOctantMinDistanceSqr > minDistance)
                {
                    return (minDistance, minPoint);
                }

                State state = octant.GetState();

                if (state == State.HasOctants)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        var subOctant = octant._octants[i];
                        if (subOctant.GetState() == State.HasPlaceInBucket && subOctant._pointCount == 0)
                        {
                            continue;
                        }

                        float octantDistance = subOctant._bounds.SqrDistance(position);
                        if (octantDistance < minDistance)
                        {
                            queue.Enqueue(subOctant, octantDistance);
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < octant._pointCount; i++)
                    {
                        var point = octant._points[i];
                        float distanceSqr = (point.Position - position).sqrMagnitude;
                        if (distanceSqr < minDistance)
                        {
                            minPoint = point;
                            minDistance = distanceSqr;
                        }
                    }
                }
            }

            return (minDistance, minPoint);
        }

        public IEnumerable<Point> GetNearestNeighbours(Vector3 position)
        {            
            var sortedPoints = new PriorityQueue<Point, float>(8);

            var queue = new PriorityQueue<Octree<T>, float>(8);
            queue.Enqueue(this, 0);

            while (queue.TryDequeue(out Octree<T> octant, out float currentOctantMinDistanceSqr))
            {
                while (sortedPoints.TryPeek(out var p, out float pointPriority) && pointPriority <= currentOctantMinDistanceSqr)
                {
                    yield return sortedPoints.Dequeue();
                }

                State state = octant.GetState();

                if (state == State.HasOctants)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        var subOctant = octant._octants[i];
                        if (subOctant.GetState() == State.HasPlaceInBucket && subOctant._pointCount == 0)
                        {
                            continue;
                        }

                        float octantDistance = subOctant._bounds.SqrDistance(position);
                        queue.Enqueue(subOctant, octantDistance);
                    }
                }
                else
                {
                    for (int i = 0; i < octant._pointCount; i++)
                    {
                        var point = octant._points[i];
                        float distanceSqr = (point.Position - position).sqrMagnitude;
                        sortedPoints.Enqueue(point, distanceSqr);
                    }
                }
            }

            while (sortedPoints.TryDequeue(out var p, out float _))
            {
                yield return p;
            }
        }

        public List<Point> RadialSearch(Vector3 position, float minDistance, float sqrtMaxDistance)
        {
            List<Point> points = new List<Point>();

            Queue<Octree<T>> queue = new Queue<Octree<T>>();
            queue.Enqueue(this);

            while (queue.Count > 0)
            {
                Octree<T> octant = queue.Dequeue();
                float octantDistance = octant._bounds.SqrDistance(position);
                if (octantDistance <= sqrtMaxDistance)
                {
                    State state = octant.GetState();

                    if (state == State.HasOctants)
                    {
                        for (int i = 0; i < 8; i++)
                        {
                            queue.Enqueue(octant._octants[i]);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < octant._pointCount; i++)
                        {
                            var point = octant._points[i];
                            float pointDistance = (point.Position - position).sqrMagnitude;
                            if (minDistance <= pointDistance && pointDistance < sqrtMaxDistance)
                            {
                                points.Add(point);
                            }
                        }
                    }
                }
            }

            return points;
        }

        public void Save()
        {
            StringBuilder sb = new StringBuilder();
            ToJson(sb, "    ");
            File.WriteAllText("E:\\octree.json", sb.ToString());
        }

        public void ToJson(StringBuilder sb, string indent)
        {
            sb.Append($@"{{
{indent}""bounds"": {{
{indent}    ""min"": {{ ""x"": {_bounds.min.x}, ""y"": {_bounds.min.y}, ""z"": {_bounds.min.z} }},
{indent}    ""max"": {{ ""x"": {_bounds.max.x}, ""y"": {_bounds.max.y}, ""z"": {_bounds.max.z} }}
{indent}}},
{indent}""state"": ""{GetState()}"",
{indent}""pointCount"": {_pointCount},
{indent}""points"": [");

            for (int i = 0; i < _pointCount; i++)
            {
                if (i > 0)
                {
                    sb.Append(",");
                }

                sb.Append($@"
{indent}    {{
{indent}        ""position"": {{ ""x"": {_points[i].Position.x}, ""y"": {_points[i].Position.y}, ""z"": {_points[i].Position.z} }},
{indent}        ""value"": ""{_points[i].Value.ToString()}""
{indent}    }}");
            }

            sb.Append($@"
{indent}],
{indent}""octants"": ");

            if (GetState() == State.HasOctants)
            {
                sb.Append("[");

                for (int i = 0; i < 8; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(',');
                    }

                    sb.Append($@"
{indent}    ");

                    _octants[i].ToJson(sb, indent + "    ");
                }

                sb.Append($@"
{indent}]");
            }
            else
            {
                sb.Append("null");
            }

            sb.Append($@"
{indent}}}");

            
        }

        private int GetOctantIndex(Vector3 position)
        {
            int index = 0;
            if (_bounds.center.x < position.x)
            {
                index += 1;
            }

            if (_bounds.center.y < position.y)
            {
                index += 2;
            }

            if (_bounds.center.z < position.z)
            {
                index += 4;
            }

            return index;
        }

        private Octree<T>[] CreateOctants()
        {
            var octants = new Octree<T>[8];

            for (int i = 0; i < 8; i++)
            {
                float x = _bounds.center.x + (i % 2 == 0 ? -0.5f : 0.5f) * _bounds.extents.x;
                float y = _bounds.center.y + ((i / 2) % 2 == 0 ? -0.5f : 0.5f) * _bounds.extents.y;
                float z = _bounds.center.z + (i / 4 == 0 ? -0.5f : 0.5f) * _bounds.extents.z;
                Vector3 center = new Vector3(x, y, z);
                octants[i] = new Octree<T>(new Bounds(center, _bounds.extents), _points.Length);
            }

            return octants;
        }
    }
}

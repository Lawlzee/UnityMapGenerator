using System;
using UnityEngine;

namespace KdTree
{
    public class NearestNeighbourList<T>
    {
        public NearestNeighbourList(int maxCapacity)
        {
            this.maxCapacity = maxCapacity;

            queue = new PriorityQueue<T>(maxCapacity);
        }

        public NearestNeighbourList()
        {
            this.maxCapacity = int.MaxValue;

            queue = new PriorityQueue<T>();
        }

        private PriorityQueue<T> queue;

        private int maxCapacity;
        public int MaxCapacity { get { return maxCapacity; } }

        public int Count { get { return queue.Count; } }

        public bool Add(KdTreeNode<T> item, float distance)
        {
            if (queue.Count >= maxCapacity)
            {
                // If the distance of this item is less than the distance of the last item
                // in our neighbour list then pop that neighbour off and push this one on
                // otherwise don't even bother adding this item
                if (distance.CompareTo(queue.GetHighestPriority()) < 0)
                {
                    queue.Dequeue();
                    queue.Enqueue(item, distance);
                    return true;
                }
                else
                    return false;
            }
            else
            {
                queue.Enqueue(item, distance);
                return true;
            }
        }

        public KdTreeNode<T> GetFurtherest()
        {
            if (Count == 0)
                throw new Exception("List is empty");
            else
                return queue.GetHighest();
        }

        public float GetFurtherestDistance()
        {
            if (Count == 0)
                throw new Exception("List is empty");
            else
                return queue.GetHighestPriority();
        }

        public KdTreeNode<T> RemoveFurtherest()
        {
            return queue.Dequeue();
        }

        public bool IsCapacityReached
        {
            get { return Count == MaxCapacity; }
        }
    }
}

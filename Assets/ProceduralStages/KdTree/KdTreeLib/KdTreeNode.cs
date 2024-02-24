using System;
using System.Text;
using UnityEngine;

namespace KdTree
{
    [Serializable]
    public class KdTreeNode<T>
    {
        public T Value;
        public Vector3 Point;

        internal KdTreeNode<T> LeftChild = null;
        internal KdTreeNode<T> RightChild = null;

        public KdTreeNode()
        {
        }

        public KdTreeNode(Vector3 point, T value)
        {
            Point = point;
            Value = value;
        }

        internal KdTreeNode<T> this[int compare]
        {
            get
            {
                if (compare <= 0)
                {
                    return LeftChild;
                }
                else
                {
                    return RightChild;
                }
            }
            set
            {
                if (compare <= 0)
                {
                    LeftChild = value;
                }
                else
                {
                    RightChild = value;
                }
            }
        }

        public bool IsLeaf => (LeftChild == null) && (RightChild == null);
    }
}
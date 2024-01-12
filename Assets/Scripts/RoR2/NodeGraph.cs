using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;

namespace RoR2.Navigation
{
    [Flags]
    public enum NodeFlags : byte
    {
        None = 0,
        NoCeiling = 1,
        TeleporterOK = 2,
        NoCharacterSpawn = 4,
        NoChestSpawn = 8,
        NoShrineSpawn = 16,
        Newt = 32
    }

    public class NodeGraph : ScriptableObject
    {
        public NodeGraph.Node[] nodes = Array.Empty<NodeGraph.Node>();
        public NodeGraph.Link[] links = Array.Empty<NodeGraph.Link>();

        public void Awake()
        {

        }

        public Vector3 GetQuadraticCoordinates(
            float t,
            Vector3 startPos,
            Vector3 apexPos,
            Vector3 endPos)
        {
            return Mathf.Pow(1f - t, 2f) * startPos + (float)(2.0 * (double)t * (1.0 - (double)t)) * apexPos + Mathf.Pow(t, 2f) * endPos;
        }

        public Mesh GenerateLinkDebugMesh(HullMask hullMask, NodeFlags nodeFlags)
        {
            using (WireMeshBuilder wireMeshBuilder = new WireMeshBuilder())
            {
                foreach (NodeGraph.Link link in this.links)
                {
                    if (((HullMask)link.hullMask & hullMask) != HullMask.None)
                    {
                        var nodeA = this.nodes[link.nodeIndexA.nodeIndex];
                        var nodeB = this.nodes[link.nodeIndexB.nodeIndex];

                        if ((nodeA.flags & nodeFlags) != NodeFlags.None
                            && (nodeB.flags & nodeFlags) != NodeFlags.None
                            && (nodeA.forbiddenHulls & hullMask) == HullMask.None
                            && (nodeB.forbiddenHulls & hullMask) == HullMask.None)
                        {
                            Vector3 position1 = this.nodes[link.nodeIndexA.nodeIndex].position;
                            Vector3 position2 = this.nodes[link.nodeIndexB.nodeIndex].position;
                            Vector3 vector3 = (position1 + position2) * 0.5f;
                            int num1 = (uint)((HullMask)link.jumpHullMask & hullMask) > 0U ? 1 : 0;
                            Color color = num1 != 0 ? Color.cyan : Color.green;
                            //color = Color.red;
                            if (num1 != 0)
                            {
                                Vector3 apexPos = new Vector3(vector3.x, position1.y + link.minJumpHeight, vector3.z);

                                int num2 = 8;
                                Vector3 p1 = position1;
                                for (int index = 1; index <= num2; ++index)
                                {
                                    if (index > num2 / 2)
                                        color.a = 0.1f;
                                    Vector3 quadraticCoordinates = this.GetQuadraticCoordinates((float)index / (float)num2, position1, apexPos, position2);
                                    wireMeshBuilder.AddLine(p1, color, quadraticCoordinates, color);
                                    p1 = quadraticCoordinates;
                                }
                            }
                            else
                            {
                                Color c2 = new Color(color.r, color.g, color.b, 0.1f);
                                wireMeshBuilder.AddLine(position1, color, (position1 + position2) * 0.5f, c2);
                            }
                        }
                    }
                }
                return wireMeshBuilder.GenerateMesh();
            }
        }

        [Serializable]
        public struct NodeIndex : IEquatable<NodeGraph.NodeIndex>
        {
            public int nodeIndex;
            public static readonly NodeGraph.NodeIndex invalid = new NodeGraph.NodeIndex(-1);

            public NodeIndex(int nodeIndex) => this.nodeIndex = nodeIndex;

            public static bool operator ==(NodeGraph.NodeIndex lhs, NodeGraph.NodeIndex rhs) => lhs.nodeIndex == rhs.nodeIndex;

            public static bool operator !=(NodeGraph.NodeIndex lhs, NodeGraph.NodeIndex rhs) => lhs.nodeIndex != rhs.nodeIndex;

            public override bool Equals(object other) => other is NodeGraph.NodeIndex nodeIndex && nodeIndex.nodeIndex == this.nodeIndex;

            public override int GetHashCode() => this.nodeIndex;

            public bool Equals(NodeGraph.NodeIndex other) => this.nodeIndex == other.nodeIndex;
        }

        [Serializable]
        public struct LinkIndex
        {
            public int linkIndex;
            public static readonly NodeGraph.LinkIndex invalid = new NodeGraph.LinkIndex()
            {
                linkIndex = -1
            };

            public static bool operator ==(NodeGraph.LinkIndex lhs, NodeGraph.LinkIndex rhs) => lhs.linkIndex == rhs.linkIndex;

            public static bool operator !=(NodeGraph.LinkIndex lhs, NodeGraph.LinkIndex rhs) => lhs.linkIndex != rhs.linkIndex;

            public override bool Equals(object other) => other is NodeGraph.LinkIndex linkIndex && linkIndex.linkIndex == this.linkIndex;

            public override int GetHashCode() => this.linkIndex;
        }

        [Serializable]
        public struct LinkListIndex
        {
            public int index;
            public uint size;
        }

        [Serializable]
        public struct Node
        {
            public Vector3 position;
            public NodeGraph.LinkListIndex linkListIndex;
            public HullMask forbiddenHulls;
            public SerializableBitArray lineOfSightMask;
            public byte gateIndex;
            public NodeFlags flags;
        }

        [Serializable]
        public struct Link
        {
            public NodeGraph.NodeIndex nodeIndexA;
            public NodeGraph.NodeIndex nodeIndexB;
            public float distanceScore;
            public float maxSlope;
            public float minJumpHeight;
            public int hullMask;
            public int jumpHullMask;
            public byte gateIndex;
        }
    }
}

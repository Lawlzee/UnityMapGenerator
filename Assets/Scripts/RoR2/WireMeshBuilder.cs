// Decompiled with JetBrains decompiler
// Type: RoR2.WireMeshBuilder
// Assembly: RoR2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: ABEE4F3B-4618-4662-B4D0-BD0BC9965114
// Assembly location: E:\SteamLibrary\steamapps\common\Risk of Rain 2\Risk of Rain 2_Data\Managed\RoR2.dll

using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoR2
{
    public class WireMeshBuilder : IDisposable
    {
        private int uniqueVertexCount;
        private Dictionary<WireMeshBuilder.LineVertex, int> uniqueVertexToIndex = new Dictionary<WireMeshBuilder.LineVertex, int>();
        private List<int> indices = new List<int>();
        private List<Vector3> positions = new List<Vector3>();
        private List<Color> colors = new List<Color>();

        private int GetVertexIndex(WireMeshBuilder.LineVertex vertex)
        {
            int vertexIndex;
            if (!this.uniqueVertexToIndex.TryGetValue(vertex, out vertexIndex))
            {
                vertexIndex = this.uniqueVertexCount++;
                this.positions.Add(vertex.position);
                this.colors.Add(vertex.color);
                this.uniqueVertexToIndex.Add(vertex, vertexIndex);
            }
            return vertexIndex;
        }

        public void Clear()
        {
            this.uniqueVertexToIndex.Clear();
            this.indices.Clear();
            this.positions.Clear();
            this.colors.Clear();
            this.uniqueVertexCount = 0;
        }

        public void AddLine(Vector3 p1, Color c1, Vector3 p2, Color c2)
        {
            WireMeshBuilder.LineVertex vertex1 = new WireMeshBuilder.LineVertex()
            {
                position = p1,
                color = c1
            };
            WireMeshBuilder.LineVertex vertex2 = new WireMeshBuilder.LineVertex()
            {
                position = p2,
                color = c2
            };
            int vertexIndex1 = this.GetVertexIndex(vertex1);
            int vertexIndex2 = this.GetVertexIndex(vertex2);
            this.indices.Add(vertexIndex1);
            this.indices.Add(vertexIndex2);
        }

        public Mesh GenerateMesh()
        {
            Mesh dest = new Mesh();
            this.GenerateMesh(dest);
            return dest;
        }

        public void GenerateMesh(Mesh dest)
        {
            dest.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            dest.SetTriangles(Array.Empty<int>(), 0);
            dest.SetVertices(this.positions);
            dest.SetColors(this.colors);
            dest.SetIndices(this.indices.ToArray(), MeshTopology.Lines, 0);
        }

        public void Dispose()
        {
            this.uniqueVertexToIndex = (Dictionary<WireMeshBuilder.LineVertex, int>)null;
            this.indices = (List<int>)null;
            this.positions = (List<Vector3>)null;
            this.colors = (List<Color>)null;
        }

        private struct LineVertex
        {
            public Vector3 position;
            public Color color;
        }
    }
}

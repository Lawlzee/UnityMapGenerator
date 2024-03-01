using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProceduralStages
{
    public class Terrain
    {
        public MeshResult meshResult;
        public float[,,] floorlessDensityMap;
        public float[,,] densityMap;
    }

    public class MeshResult
    {
        public Mesh mesh;
        public Vector3[] vertices;
        public int[] triangles;
        public Vector3[] normals;
    }

    public abstract class TerrainGenerator : ScriptableObject
    {
        public abstract Terrain Generate();
    }
}

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
        public float maxGroundheight;
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
        public Vector3Int size;
        public Vector3Int sizeIncreasePerStage;
        public Vector3 sizeVariation;
        public float fogPower = 0.75f;
        public float vignetteInsentity = 0.25f;
        public float ambiantLightIntensity;
        public float waterLevel = 0f;

        public abstract TerrainType TerrainType { get; }

        public abstract Terrain Generate();
    }
}

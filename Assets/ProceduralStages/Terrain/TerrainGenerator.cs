using RoR2.Navigation;
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
        public TerrainGenerator generator;
        public MeshResult meshResult;
        public float[,,] floorlessDensityMap;
        public float[,,] densityMap;
        public float maxGroundHeight;
        public float minInteractableHeight;
        public Vector3 oobScale = new Vector3(1, 1.5f, 1);
        public List<GameObject> customObjects = new List<GameObject>();

        public MoonTerrain moonTerrain;
    }

    public class MoonTerrain
    {
        public NodeGraph arenaGroundGraph;
        public NodeGraph arenaAirGraph;
    }

    public class MeshResult
    {
        public Mesh mesh;
        public Vector3[] vertices;
        public int verticesLength;
        public int[] triangles;
        public Vector3[] normals;
    }

    public abstract class TerrainGenerator : ScriptableObject
    {
        public Vector3Int size;
        public Vector3Int sizeIncreasePerStage;
        public Vector3 sizeVariation;
        public float fogPower = 0.75f;
        public float fogIntensityCoefficient = 1f;
        public float vignetteInsentity = 0.25f;
        public float ambiantLightIntensity;
        public float waterLevel = 0f;
        public TerrainType terrainType;
        public float propCountWeight = 1;
        public float ceillingPropsWeight = 1;
        public BackdropGenerator backdropGenerator;
        public float airNodesScale = 1;

        public abstract Terrain Generate();

        public virtual void AddProps(Terrain terrain, Graphs graphs)
        {

        }
    }
}

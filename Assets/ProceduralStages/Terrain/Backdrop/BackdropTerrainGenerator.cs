using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProceduralStages
{
    public class BackdropTerrain
    {
        public MeshResult meshResult;
        public Vector3 position;
        public float propsWeigth;
    }

    public abstract class BackdropTerrainGenerator : ScriptableObject
    {
        public Interval distance;

        public Vector3 minSize;
        public Vector3 maxSize;

        public float scalePerDistance;

        public abstract BackdropTerrain Generate(
            Vector3 center,
            ulong seed,
            ProfilerLog log);
    }
}

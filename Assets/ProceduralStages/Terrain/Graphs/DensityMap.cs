using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProceduralStages
{
    [Serializable]
    public class DensityMap
    {
        [Range(0f, 1f)]
        public float maxChestDensity = 0.9f;
        [Range(0f, 1f)]
        public float maxShrineDensity = 0.8f;
        [Range(0f, 1f)]
        public float minTeleporterDensity = 0f;
        [Range(0f, 1f)]
        public float maxTeleporterDensity = 0.3f;

        [Range(0f, 1f)]
        public float minNewtDensity = 0.5f;
        [Range(0f, 1f)]
        public float maxNewtDensity = 1f;


        [Range(0f, 1f)]
        public float maxSpawnDensity = 0.45f;

        public HullDensity air = new HullDensity
        {
            maxHumanDensity = 0.8f,
            maxGolemDensity = 0.7f,
            maxBeetleQueenDensity = 0.6f
        };
        public HullDensity ground = new HullDensity
        {
            maxHumanDensity = 0.8f,
            maxGolemDensity = 0.7f,
            maxBeetleQueenDensity = 0.6f
        };

        public float GetDensity(float[,,] map, Vector3 position, bool[,] bounds)
        {
            int x = Mathf.RoundToInt(position.x);
            int y = Mathf.RoundToInt(position.y);
            int z = Mathf.RoundToInt(position.z);

            int width = map.GetLength(0);
            int height = map.GetLength(1);
            int depth = map.GetLength(2);

            if (x < 0 || y < 0 || z < 0 || x >= width || y >= height || z >= depth)
            {
                return 1f;
            }

            if (bounds != null && !bounds[x, z])
            {
                return 1f;
            }

            return map[x, y, z];
        }

        [Serializable]
        public class HullDensity
        {
            [Range(0f, 1f)]
            public float maxHumanDensity;
            [Range(0f, 1f)]
            public float maxGolemDensity;
            [Range(0f, 1f)]
            public float maxBeetleQueenDensity;
        }
    }
}

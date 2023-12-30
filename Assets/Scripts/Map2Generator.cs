using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts
{
    public class Map2Generator : MonoBehaviour
    {
        public int width = 10;
        public int height = 10;
        public int depth = 10;

        [Range(0, 1)] 
        public float frequency = 0.5f;
        [Range(0, 1)]
        public float threshold = 0.5f;

        public string seed;

        private float[,,] _map;
        private void Start()
        {
            GenerateMap();
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                GenerateMap();
            }
        }

        private void GenerateMap()
        {
            int currentSeed = string.IsNullOrEmpty(seed)
                ? Time.time.GetHashCode() % Int16.MaxValue
                : seed.GetHashCode() % Int16.MaxValue;

            _map = new float[width, height, depth];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        _map[x, y, z] = PerlinNoise.Get(new Vector3(x + currentSeed, y + currentSeed, z + currentSeed), frequency);
                    }
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (_map != null)
            {
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        for (int z = 0; z < depth; z++)
                        {
                            float sample = _map[x, y, z];

                            if (sample > threshold)
                            {
                                sample = (sample + 1f) / 2f;
                                Gizmos.color = Color.white;// new Color(sample, sample, sample, 1);

                                Gizmos.DrawCube(new Vector3(x, y, z), Vector3.one);
                            }
                        }
                    }
                }
            }
        }
    }
}

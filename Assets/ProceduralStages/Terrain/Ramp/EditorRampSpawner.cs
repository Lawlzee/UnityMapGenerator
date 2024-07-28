using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProceduralStages
{
    public class EditorRampSpawner : MonoBehaviour
    {
        public GameObject rampPrefab;
        public Vector3 size;
        public float distance;
        public float yOffset;
        public float noiseLevel;
        public float propsWeight;

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.F10))
            {
                ProceduralRamp ramp = ProceduralRamp.instance ?? Instantiate(rampPrefab).GetComponent<ProceduralRamp>();
                ramp.Generate(size, distance, yOffset, noiseLevel, propsWeight);
            }
        }
    }
}

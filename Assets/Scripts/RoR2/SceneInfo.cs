using RoR2.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RoR2
{
    public class SceneInfo : MonoBehaviour
    {
        public HullMask hullMask;
        public NodeFlags nodeFlags;

        public bool showGroundMesh;
        public bool showAirMesh;

        public GameObject groundMeshObject;
        public GameObject airMeshObject;

        public NodeGraph groundNodes;
        public NodeGraph airNodes;

        public void Awake()
        {

        }

        public void Start()
        {
            UpdateMesh();
        }

        public void OnValidate()
        {
            UpdateMesh();
        }

        private void UpdateMesh()
        {
            if (showGroundMesh)
            {
                var mesh = groundNodes.GenerateLinkDebugMesh(hullMask, nodeFlags);
                groundMeshObject.GetComponent<MeshFilter>().mesh = mesh;
            }
            else
            {
                groundMeshObject.GetComponent<MeshFilter>().mesh = null;
            }

            if (showGroundMesh)
            {
                var mesh = airNodes.GenerateLinkDebugMesh(hullMask, nodeFlags);
                airMeshObject.GetComponent<MeshFilter>().mesh = mesh;
            }
            else
            {
                airMeshObject.GetComponent<MeshFilter>().mesh = null;
            }
        }
    }
}

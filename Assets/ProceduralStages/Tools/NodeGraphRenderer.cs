using RoR2.Navigation;
using RoR2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeGraphRenderer : MonoBehaviour
{
    public HullMask hullMask;
    public NodeFlags nodeFlags;

    public bool showGroundMesh;
    public bool showAirMesh;

    public GameObject groundMeshObject;
    public GameObject airMeshObject;
    public GameObject sceneInfoObject;
    private SceneInfo sceneInfo;
    private NodeGraph _cacheNodeGraph;

    public void Awake()
    {
        sceneInfo = sceneInfoObject.GetComponent<SceneInfo>();
    }

    public void OnValidate()
    {
        UpdateMesh();
    }

    private void Update()
    {
        if (_cacheNodeGraph != sceneInfo.groundNodes)
        {
            UpdateMesh();
        }
    }

    private void UpdateMesh()
    {
        Debug.Log("UpdateMesh");
        _cacheNodeGraph = sceneInfo.groundNodes;

        var groundNodes = sceneInfo.groundNodes;

        if (showGroundMesh)
        {
            var mesh = GenerateLinkDebugMesh(groundNodes);
            groundMeshObject.GetComponent<MeshFilter>().mesh = mesh;
        }
        else
        {
            groundMeshObject.GetComponent<MeshFilter>().mesh = null;
        }

        var airNodes = sceneInfo.airNodes;

        if (showAirMesh && airNodes != null)
        {
            var mesh = GenerateLinkDebugMesh(airNodes);
            airMeshObject.GetComponent<MeshFilter>().mesh = mesh;
        }
        else
        {
            airMeshObject.GetComponent<MeshFilter>().mesh = null;
        }
    }

    public Mesh GenerateLinkDebugMesh(NodeGraph nodeGraph)
    {
        using (WireMeshBuilder wireMeshBuilder = new WireMeshBuilder())
        {
            foreach (NodeGraph.Link link in nodeGraph.links)
            {
                if (((HullMask)link.hullMask & hullMask) != HullMask.None)
                {
                    var nodeA = nodeGraph.nodes[link.nodeIndexA.nodeIndex];
                    var nodeB = nodeGraph.nodes[link.nodeIndexB.nodeIndex];

                    if ((nodeA.flags & nodeFlags) != NodeFlags.None
                        && (nodeB.flags & nodeFlags) != NodeFlags.None
                        && (nodeA.forbiddenHulls & hullMask) == HullMask.None
                        && (nodeB.forbiddenHulls & hullMask) == HullMask.None)
                    {
                        Vector3 position1 = nodeGraph.nodes[link.nodeIndexA.nodeIndex].position;
                        Vector3 position2 = nodeGraph.nodes[link.nodeIndexB.nodeIndex].position;
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
                                Vector3 quadraticCoordinates = nodeGraph.GetQuadraticCoordinates((float)index / (float)num2, position1, apexPos, position2);
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

            Mesh mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            wireMeshBuilder.GenerateMesh(mesh);
            return mesh;
        }
    }
}

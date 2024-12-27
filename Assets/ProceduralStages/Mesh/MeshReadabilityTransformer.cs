using ProceduralStages;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(fileName = "MeshReadabilityTransformer", menuName = "ProceduralStages/MeshReadabilityTransformer", order = 20)]
public class MeshReadabilityTransformer : ScriptableObject
{
    public Material material;
    private RenderTexture renderTexture;

    void Awake()
    {
        renderTexture = new RenderTexture(1, 1, 24);
        renderTexture.enableRandomWrite = true;
        renderTexture.Create();
    }

    public Mesh CreateReadableCopy(Mesh mesh, string path)
    {
        if (mesh.subMeshCount != 1)
        {
            Log.Debug($"mesh.subMeshCount != 1 ({mesh.name}) {path}");
        }

        int vertexCount = mesh.vertexCount;
        int triangleCount = mesh.GetSubMesh(mesh.subMeshCount - 1).indexCount + mesh.GetSubMesh(mesh.subMeshCount - 1).indexStart;

        //Debug.Log("mesh: " + mesh.name);
        //Debug.Log("vertexCount: " + vertexCount);
        //Debug.Log("triangleCount: " + triangleCount);

        ComputeBuffer vertexBuffer = new ComputeBuffer(vertexCount, 12, ComputeBufferType.Default);
        ComputeBuffer normalBuffer = new ComputeBuffer(vertexCount, 12, ComputeBufferType.Default);
        ComputeBuffer triangleBuffer = new ComputeBuffer(triangleCount, 4, ComputeBufferType.Default);

        CommandBuffer commandBuffer = new CommandBuffer { name = "Custom Mesh Render" };

        material.SetBuffer("_VertexBuffer", vertexBuffer);
        material.SetBuffer("_NormalBuffer", normalBuffer);
        material.SetBuffer("_TriangleBuffer", triangleBuffer);

        commandBuffer.SetRenderTarget(renderTexture);
        commandBuffer.ClearRenderTarget(true, true, Color.clear);

        commandBuffer.ClearRandomWriteTargets();
        commandBuffer.SetRandomWriteTarget(1, vertexBuffer);
        commandBuffer.SetRandomWriteTarget(2, normalBuffer);
        commandBuffer.SetRandomWriteTarget(3, triangleBuffer);

        commandBuffer.DrawMesh(mesh, Matrix4x4.identity, material);

        Graphics.ExecuteCommandBuffer(commandBuffer);

        Vector3[] vertices = new Vector3[mesh.vertexCount];
        Vector3[] normals = new Vector3[mesh.vertexCount];
        int[] triangles = new int[triangleCount];

        vertexBuffer.GetData(vertices);
        normalBuffer.GetData(normals);
        triangleBuffer.GetData(triangles);

        //Debug.Log($"vertices: {vertices.Where(x => x != default).Count()} / {vertices.Length}");
        //Debug.Log($"normals: {normals.Where(x => x != default).Count()} / {normals.Length}");
        //Debug.Log($"triangles: {triangles.Where(x => x != default).Count()} / {triangles.Length}");

        vertexBuffer.Release();
        normalBuffer.Release();
        triangleBuffer.Release();
        commandBuffer.Dispose();

        return new Mesh
        {
            indexFormat = mesh.indexFormat,
            vertices = vertices,
            normals = normals,
            triangles = triangles,
            bounds = mesh.bounds,
            name = mesh.name
        };
    }
}

using System;
using System.Collections.Generic;
using Godot;
using GodotVoxelGame.VoxleData;

public partial class MeshManager : Node
{
    public VoxelWorld voxelWorld {get; set;}
    [Export] public Material material {get; set;}

    private Dictionary<Vector3I, MeshInstance3D> meshInstanceDict = [];

    public override void _Ready()
    {
        
    }

    public void 渲染网格(Chunk chunk)
    {
        MeshInstance3D meshInstance = new MeshInstance3D();

        var arrMesh = new ArrayMesh();
        Godot.Collections.Array surfaceArray = [];
        surfaceArray.Resize((int)Mesh.ArrayType.Max);

        List<Vector3> verts = chunk.顶点数据.vert;
        List<Vector2> uvs = chunk.顶点数据.uv;
        List<Vector3> normals = chunk.顶点数据.normal;
        List<int> indices = chunk.顶点数据.indice;

        surfaceArray[(int)Mesh.ArrayType.Vertex] = verts.ToArray();
        surfaceArray[(int)Mesh.ArrayType.TexUV] = uvs.ToArray();
        surfaceArray[(int)Mesh.ArrayType.Normal] = normals.ToArray();
        surfaceArray[(int)Mesh.ArrayType.Index] = indices.ToArray();

        arrMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArray);

        meshInstance.MaterialOverride = material;
        meshInstance.Mesh = arrMesh;
        meshInstanceDict[chunk.ChunkPosition] = meshInstance;
        AddChild(meshInstance);
    }

    public void 清除网格(Chunk chunk)
    {
        if(meshInstanceDict.TryGetValue(chunk.ChunkPosition, out MeshInstance3D meshInstance))
        {
            meshInstance.QueueFree();
            meshInstanceDict.Remove(chunk.ChunkPosition);
        }
    }

    
}
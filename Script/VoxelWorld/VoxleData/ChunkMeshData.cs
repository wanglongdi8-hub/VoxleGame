using System.Collections.Generic;
using Godot;

namespace GodotVoxelGame.VoxleData;

public class 区块顶点数据
{
    public List<Vector3> vert { get; set; } = [];
    public List<Vector2> uv { get; set; } = [];
    public List<Vector3> normal { get; set; } = [];
    public List<int> indice { get; set; } = [];

    public 区块顶点数据()
    {
        vert = [];
        uv = [];
        normal = [];
        indice = [];
    }
    public 区块顶点数据(List<Vector3> verts, List<Vector2> uvs, List<Vector3> normals, List<int> indices)
    {
        vert = verts;
        uv = uvs;
        normal = normals;
        indice = indices;
    }

    public void AddRange(区块顶点数据 a)
    {
        int vertexOffset = vert.Count;
        vert.AddRange(a.vert);
        uv.AddRange(a.uv);
        normal.AddRange(a.normal);
        
        // 调整索引，加上当前的顶点偏移
        foreach (var index in a.indice)
        {
            indice.Add(index + vertexOffset);
        }
    }

    public void Clear()
    {
        vert.Clear();
        uv.Clear();
        normal.Clear();
        indice.Clear();
    }
}

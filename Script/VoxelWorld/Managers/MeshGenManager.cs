using Godot;
using GodotVoxelGame.VoxleData;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;


[Tool]public partial class MeshGenManager : MeshInstance3D
{
	[Export] public StandardMaterial3D BlockMaterial { get; set; }
	[Export] public VoxelCollisionComponent 碰撞组件 { get; set; }
    
    private VoxelWorld _voxelWorld;
    
    // 存储所有区块的顶点数据，键为区块位置
    private Dictionary<Vector3I, 区块顶点数据> _chunkMeshData = new Dictionary<Vector3I, 区块顶点数据>();
    
    // 需要重新渲染的区块列表
    private HashSet<Vector3I> _chunksToUpdate = new HashSet<Vector3I>();
    private bool _fullRebuildRequested = false;

    public override void _Ready()
    {
        _voxelWorld = GetParent<VoxelWorld>();

    }


    public void 移除区块(Vector3I chunkPosition)
    {
        if (_chunkMeshData.ContainsKey(chunkPosition))
        {
            _chunkMeshData.Remove(chunkPosition);
            _fullRebuildRequested = true; // 需要重新构建所有网格
        }
    }


    public void 添加区块(Chunk chunk)
    {
        // 生成这个区块的顶点数据
        var meshData = 返回区块顶点数据(chunk);
        
        // 存储到字典中
        if (_chunkMeshData.ContainsKey(chunk.ChunkPosition))
        {
            _chunkMeshData[chunk.ChunkPosition] = meshData;
        }
        else
        {
            _chunkMeshData.Add(chunk.ChunkPosition, meshData);
        }
        
        // 标记需要更新
        _chunksToUpdate.Add(chunk.ChunkPosition);
    }


    public override void _Process(double delta)
    {
        base._Process(delta);
        
        if (_voxelWorld.重新生成网格)
        {
            _voxelWorld.重新生成网格 = false;
            _fullRebuildRequested = true;
        }

        if (_voxelWorld.清除现有网格)
        {
            ClearAllMeshData();
            _voxelWorld.清除现有网格 = false;
        }
        
        // 如果有需要更新的区块，重新渲染
        if (_chunksToUpdate.Count > 0 || _fullRebuildRequested)
        {
            重新渲染所有区块();
        }
    }

    private void 重新渲染所有区块()
    {
        if (_fullRebuildRequested)
        {
            // 完全重建
            _chunksToUpdate.Clear();
            _fullRebuildRequested = false;
        }
        else
        {
            // 只更新有变化的区块
            _chunksToUpdate.Clear();
        }
        
        // 合并所有区块的顶点数据
        区块顶点数据 combinedData = 合并所有区块数据();
        
        // 创建网格
        var arrMesh = new ArrayMesh();
        Godot.Collections.Array surfaceArray = [];
        surfaceArray.Resize((int)Mesh.ArrayType.Max);

        surfaceArray[(int)Mesh.ArrayType.Vertex] = combinedData.vert.ToArray();
        surfaceArray[(int)Mesh.ArrayType.TexUV] = combinedData.uv.ToArray();
        surfaceArray[(int)Mesh.ArrayType.Normal] = combinedData.normal.ToArray();
        surfaceArray[(int)Mesh.ArrayType.Index] = combinedData.indice.ToArray();

        arrMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArray);
        MaterialOverride = BlockMaterial;
        Mesh = arrMesh;

		/*
		// 更新碰撞形状
        if (碰撞组件 != null && combinedData.vert.Count > 0)
        {
            bool 成功 = 碰撞组件.更新碰撞形状(combinedData);
            if (!成功)
            {
                GD.Print("[MeshGenManager] 碰撞更新失败，网格顶点数: " + combinedData.vert.Count);
            }
        }
		*/
    }

    private 区块顶点数据 合并所有区块数据()
    {
        区块顶点数据 combinedData = new 区块顶点数据();
        
        foreach (var chunkData in _chunkMeshData.Values)
        {
            combinedData.AddRange(chunkData);
        }
        
        return combinedData;
    }

    private void ClearAllMeshData()
    {
        _chunkMeshData.Clear();
        _chunksToUpdate.Clear();
        _fullRebuildRequested = false;
        
        ArrayMesh arrMesh = new ArrayMesh();
        Mesh = arrMesh;

		 // 清除碰撞形状
        if (碰撞组件 != null)
        {
            碰撞组件.清除碰撞形状();
        }
    }


    private 区块顶点数据 返回区块顶点数据(Chunk chunk)
    {
        List<Vector3> verts = [];
        List<Vector2> uvs = [];
        List<Vector3> normals = [];
        List<int> indices = [];
        
        int indexOffset = 0;

        for (int x = 0; x < VoxelConst.CHUNK_SIZE_X; x++)
        {
            for (int y = 0; y < VoxelConst.CHUNK_SIZE_Y; y++)
            {
                for (int z = 0; z < VoxelConst.CHUNK_SIZE_Z; z++)
                { 
                    var blockId = chunk.GetVoxel(x, y, z);
                    if (blockId == 0) continue;
                    var worldPos = new Vector3(x, y, z) + (Vector3)chunk.ChunkPosition * VoxelConst.CHUNK_SIZE_X;

                    foreach (var dir in Dirs)
                    {
                        if (_voxelWorld.启用遮挡剔除)
                        {
                            int nx = x + dir.X;
                            int ny = y + dir.Y;
                            int nz = z + dir.Z;
                            if (IsBlockSolid(chunk, nx, ny, nz))
                                continue;
                        }

                        AddFace(
                            worldPos,
                            dir,
                            verts,
                            uvs,
                            normals,
                            indices,
                            ref indexOffset
                        );
                    }
                }
            }
        }
        
        return new 区块顶点数据(verts, uvs, normals, indices);
    }
    

    private bool IsBlockSolid(Chunk chunk, int x, int y, int z)
    {
        if (x < 0 || x >= VoxelConst.CHUNK_SIZE_X) return false;
        if (y < 0 || y >= VoxelConst.CHUNK_SIZE_Y) return false;
        if (z < 0 || z >= VoxelConst.CHUNK_SIZE_Z) return false;
        return chunk.GetVoxel(x, y, z) != 0;
    }
    
    private void AddFace(
        Vector3 pos, 
        Vector3I dir, 
        List<Vector3> verts, 
        List<Vector2> uvs, 
        List<Vector3> normals, 
        List<int> indices, 
        ref int offset)
    {
        Vector3[] faceVerts = GetFaceVertices(pos, dir);
        Vector3 normal = GetFaceNormal(dir);

        // 添加4个顶点
        verts.AddRange(faceVerts);

        // 添加4个UV（默认0~1，可后期扩展纹理）
        uvs.Add(new Vector2(0, 0));
        uvs.Add(new Vector2(1, 0));
        uvs.Add(new Vector2(0, 1));
        uvs.Add(new Vector2(1, 1));

        // 添加4个法线（同一个面法线相同）
        normals.Add(normal);
        normals.Add(normal);
        normals.Add(normal);
        normals.Add(normal);

        // 索引（完全和你 GenMesh 格式一样）
        indices.Add(offset + 0);
        indices.Add(offset + 1);
        indices.Add(offset + 2);
        indices.Add(offset + 2);
        indices.Add(offset + 1);
        indices.Add(offset + 3);

        offset += 4;
    }

    private Vector3 GetFaceNormal(Vector3I dir)
    {
        if (dir == Vector3I.Up) return Vector3.Up;
        if (dir == Vector3I.Down) return Vector3.Down;
        if (dir == Vector3I.Right) return Vector3.Right;
        if (dir == Vector3I.Left) return Vector3.Left;
        if (dir == new Vector3I(0, 0, 1)) return Vector3.Forward;
        return Vector3.Back;
    }

    private Vector3[] GetFaceVertices(Vector3 pos, Vector3I dir)
    {
        float s = VoxelConst.VOXEL_SIZE;

        if (dir == Vector3I.Up)
        {
            return new[]
            {
                pos + new Vector3(0, s, 0),
                pos + new Vector3(s, s, 0),
                pos + new Vector3(0, s, s),
                pos + new Vector3(s, s, s)
            };
        }
        if (dir == Vector3I.Down)
        {
            return new[]
            {
                pos + new Vector3(0, 0, s),
                pos + new Vector3(s, 0, s),
                pos + new Vector3(0, 0, 0),
                pos + new Vector3(s, 0, 0)
            };
        }
        if (dir == Vector3I.Right)
        {
            return new[]
            {
                pos + new Vector3(s, 0, s),
                pos + new Vector3(s, s, s),
                pos + new Vector3(s, 0, 0),
                pos + new Vector3(s, s, 0)
            };
        }
        if (dir == Vector3I.Left)
        {
            return new[]
            {
                pos + new Vector3(0, 0, 0),
                pos + new Vector3(0, s, 0),
                pos + new Vector3(0, 0, s),
                pos + new Vector3(0, s, s)
            };
        }
        if (dir == new Vector3I(0,0,1))
        {
            return new[]
            {
                pos + new Vector3(0, 0, s),
                pos + new Vector3(0, s, s),
                pos + new Vector3(s, 0, s),
                pos + new Vector3(s, s, s)
            };
        }
        else
        {
            return new[]
            {
                pos + new Vector3(s, 0, 0),
                pos + new Vector3(s, s, 0),
                pos + new Vector3(0, 0, 0),
                pos + new Vector3(0, s, 0)
            };
        }
    }

    private static readonly Vector3I[] Dirs = new Vector3I[]
    {
        new(0, 1, 0),  // 上
        new(0, -1, 0), // 下
        new(1, 0, 0),  // 右
        new(-1, 0, 0), // 左
        new(0, 0, 1),  // 前
        new(0, 0, -1)  // 后
    };


}

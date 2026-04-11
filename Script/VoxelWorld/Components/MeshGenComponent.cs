using Godot;
using System;
using GodotVoxelGame.VoxleData;
using System.Collections.Generic;

public partial class MeshGenComponent : MeshInstance3D
{
    public int DefaultBlockID { get; set; } = 1;
    private Material _blockMaterial  = new Material();
    BlockAtlasConfig atlasConfig = new BlockAtlasConfig();

    public override void _Ready()
    {
        atlasConfig = G.Instance.AtlasConfig;
        _blockMaterial = G.Instance.AtlasConfig.AtlasTexture;
    }

    public void 渲染带材质的网格(Chunk chunk)
    {
        var arrMesh = new ArrayMesh();
		Godot.Collections.Array surfaceArray = [];
        surfaceArray.Resize((int)Mesh.ArrayType.Max);

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
                        if (G.Instance.启用面剔除)
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

		surfaceArray[(int)Mesh.ArrayType.Vertex] = verts.ToArray();
        surfaceArray[(int)Mesh.ArrayType.TexUV] = uvs.ToArray();
        surfaceArray[(int)Mesh.ArrayType.Normal] = normals.ToArray();
        surfaceArray[(int)Mesh.ArrayType.Index] = indices.ToArray();

        arrMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArray);

        MaterialOverride = _blockMaterial;

		Mesh = arrMesh;
    }

	public void 渲染不带材质的网格(Chunk chunk)
    {
		var arrMesh = new ArrayMesh();
		Godot.Collections.Array surfaceArray = [];
        surfaceArray.Resize((int)Mesh.ArrayType.Max);

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
                        if (G.Instance.启用面剔除)
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

		surfaceArray[(int)Mesh.ArrayType.Vertex] = verts.ToArray();
        surfaceArray[(int)Mesh.ArrayType.TexUV] = uvs.ToArray();
        surfaceArray[(int)Mesh.ArrayType.Normal] = normals.ToArray();
        surfaceArray[(int)Mesh.ArrayType.Index] = indices.ToArray();

        arrMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArray);

        MaterialOverride = _blockMaterial;

		Mesh = arrMesh;
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

    public void 清空网格()
    {
        Mesh = new ArrayMesh();
    }	
}

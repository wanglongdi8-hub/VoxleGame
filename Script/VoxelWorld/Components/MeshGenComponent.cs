using Godot;
using System;
using GodotVoxelGame.VoxleData;
using System.Collections.Generic;
using System.Reflection.Metadata;

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

    public void ArrayMesh渲染带材质的网格(Chunk chunk)
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
                    
                    // 计算方块的8个顶点
                    var blockVerts = 计算方块顶点((Vector3I)worldPos);
                    
                    // 计算方块的UV坐标
                    var sideUvs = 计算方块UV(blockId);
                    var topUvs = sideUvs;
                    var bottomUvs = sideUvs;
                    
                    // 处理花草方块的特殊绘制方式
                    if (blockId == 27 || blockId == 28)
                    {
                        // 第一个交叉面
                        添加方块面(verts, uvs, indices, normals, ref indexOffset, 
                            [blockVerts[2], blockVerts[0], blockVerts[7], blockVerts[5]], sideUvs, new Vector3I(0, 0, 0));
                        // 第二个交叉面
                        添加方块面(verts, uvs, indices, normals, ref indexOffset, 
                            [blockVerts[3], blockVerts[1], blockVerts[6], blockVerts[4]], sideUvs, new Vector3I(0, 0, 0));
                        continue;
                    }
                    
                    // 处理需要不同顶部/底部纹理的方块
                    switch (blockId)
                    {
                        case 3:  // 草地：顶部用草(0)，底部用泥土(2)
                            topUvs = 计算方块UV(0);
                            bottomUvs = 计算方块UV(2);
                            break;
                        case 5:  // 熔炉：顶部和底部用特殊纹理(31)
                            topUvs = 计算方块UV(31);
                            bottomUvs = topUvs;
                            break;
                        case 12: // 木头：顶部和底部用年轮纹理(30)
                            topUvs = 计算方块UV(30);
                            bottomUvs = topUvs;
                            break;
                        case 19: // 书架：顶部和底部用木板纹理(4)
                            topUvs = 计算方块UV(4);
                            bottomUvs = topUvs;
                            break;
                    }
                    
                    // 检查六个方向的面是否需要渲染
                    var blockPos = (Vector3I)worldPos;
                    
                    // 左面
                    var otherPosition = blockPos + Vector3I.Left;
                    var otherId = 获取相邻方块ID(otherPosition, chunk);
                    if (blockId != otherId && 是透明方块(otherId))
                    {
                        添加方块面(verts, uvs, indices, normals, ref indexOffset, 
                            [blockVerts[2], blockVerts[0], blockVerts[3], blockVerts[1]], sideUvs, Vector3I.Left);
                    }
                    
                    // 右面
                    otherPosition = blockPos + Vector3I.Right;
                    otherId = 获取相邻方块ID(otherPosition, chunk);
                    if (blockId != otherId && 是透明方块(otherId))
                    {
                        添加方块面(verts, uvs, indices, normals, ref indexOffset, 
                            [blockVerts[7], blockVerts[5], blockVerts[6], blockVerts[4]], sideUvs, Vector3I.Right);
                    }
                    
                    // 前面
                    otherPosition = blockPos + Vector3I.Forward;
                    otherId = 获取相邻方块ID(otherPosition, chunk);
                    if (blockId != otherId && 是透明方块(otherId))
                    {
                        添加方块面(verts, uvs, indices, normals, ref indexOffset, 
                            [blockVerts[6], blockVerts[4], blockVerts[2], blockVerts[0]], sideUvs, Vector3I.Forward);
                    }
                    
                    // 后面
                    otherPosition = blockPos + Vector3I.Back;
                    otherId = 获取相邻方块ID(otherPosition, chunk);
                    if (blockId != otherId && 是透明方块(otherId))
                    {
                        添加方块面(verts, uvs, indices, normals, ref indexOffset, 
                            [blockVerts[3], blockVerts[1], blockVerts[7], blockVerts[5]], sideUvs, Vector3I.Back);
                    }
                    
                    // 下面
                    otherPosition = blockPos + Vector3I.Down;
                    otherId = 获取相邻方块ID(otherPosition, chunk);
                    if (blockId != otherId && 是透明方块(otherId))
                    {
                        添加方块面(verts, uvs, indices, normals, ref indexOffset, 
                            [blockVerts[4], blockVerts[5], blockVerts[0], blockVerts[1]], bottomUvs, Vector3I.Down);
                    }
                    
                    // 上面
                    otherPosition = blockPos + Vector3I.Up;
                    otherId = 获取相邻方块ID(otherPosition, chunk);
                    if (blockId != otherId && 是透明方块(otherId))
                    {
                        添加方块面(verts, uvs, indices, normals, ref indexOffset, 
                            [blockVerts[2], blockVerts[3], blockVerts[6], blockVerts[7]], topUvs, Vector3I.Up);
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

    // 绘制单个方块的网格
    private void 绘制方块网格(SurfaceTool surfaceTool, Vector3I blockPosition, int blockId, Chunk chunk)
    {
        // 计算方块的8个顶点
        var verts = 计算方块顶点(blockPosition);
        
        // 计算方块的UV坐标
        var uvs = 计算方块UV(blockId);
        var topUvs = uvs;    // 顶部UV
        var bottomUvs = uvs; // 底部UV
        
        // 处理花草方块的特殊绘制方式
        if (blockId == 27 || blockId == 28)
        {
            绘制方块面(surfaceTool, new[] { verts[2], verts[0], verts[7], verts[5] }, uvs);
            绘制方块面(surfaceTool, new[] { verts[7], verts[5], verts[2], verts[0] }, uvs);
            绘制方块面(surfaceTool, new[] { verts[3], verts[1], verts[6], verts[4] }, uvs);
            绘制方块面(surfaceTool, new[] { verts[6], verts[4], verts[3], verts[1] }, uvs);
            return;
        }
        
        // 处理需要不同顶部/底部纹理的方块
        switch (blockId)
        {
            case 3:  // 草地：顶部用草(0)，底部用泥土(2)
                topUvs = 计算方块UV(0);
                bottomUvs = 计算方块UV(2);
                break;
            case 5:  // 熔炉：顶部和底部用特殊纹理(31)
                topUvs = 计算方块UV(31);
                bottomUvs = topUvs;
                break;
            case 12: // 木头：顶部和底部用年轮纹理(30)
                topUvs = 计算方块UV(30);
                bottomUvs = topUvs;
                break;
            case 19: // 书架：顶部和底部用木板纹理(4)
                topUvs = 计算方块UV(4);
                bottomUvs = topUvs;
                break;
        }
        
        // 检查六个方向的面是否需要渲染
        // 左面
        var otherPosition = blockPosition + Vector3I.Left;
        var otherId = 获取相邻方块ID(otherPosition, chunk);
        if (blockId != otherId && 是透明方块(otherId))
        {
            绘制方块面(surfaceTool, new[] { verts[2], verts[0], verts[3], verts[1] }, uvs);
        }
        
        // 右面
        otherPosition = blockPosition + Vector3I.Right;
        otherId = 获取相邻方块ID(otherPosition, chunk);
        if (blockId != otherId && 是透明方块(otherId))
        {
            绘制方块面(surfaceTool, new[] { verts[7], verts[5], verts[6], verts[4] }, uvs);
        }
        
        // 前面
        otherPosition = blockPosition + Vector3I.Forward;
        otherId = 获取相邻方块ID(otherPosition, chunk);
        if (blockId != otherId && 是透明方块(otherId))
        {
            绘制方块面(surfaceTool, new[] { verts[6], verts[4], verts[2], verts[0] }, uvs);
        }
        
        // 后面
        otherPosition = blockPosition + Vector3I.Back;
        otherId = 获取相邻方块ID(otherPosition, chunk);
        if (blockId != otherId && 是透明方块(otherId))
        {
            绘制方块面(surfaceTool, new[] { verts[3], verts[1], verts[7], verts[5] }, uvs);
        }
        
        // 下面
        otherPosition = blockPosition + Vector3I.Down;
        otherId = 获取相邻方块ID(otherPosition, chunk);
        if (blockId != otherId && 是透明方块(otherId))
        {
            绘制方块面(surfaceTool, new[] { verts[4], verts[5], verts[0], verts[1] }, bottomUvs);
        }
        
        // 上面
        otherPosition = blockPosition + Vector3I.Up;
        otherId = 获取相邻方块ID(otherPosition, chunk);
        if (blockId != otherId && 是透明方块(otherId))
        {
            绘制方块面(surfaceTool, new[] { verts[2], verts[3], verts[6], verts[7] }, topUvs);
        }
    }

    // 计算方块UV坐标的核心方法
    private Vector2[] 计算方块UV(int blockId)
    {
        const int TEXTURE_SHEET_WIDTH = 8;  // 纹理图集每行8个纹理
        const float TEXTURE_TILE_SIZE = 1.0f / TEXTURE_SHEET_WIDTH;  // 每个纹理的大小
        
        // 计算纹理在图集中的行列位置
        int row = blockId / TEXTURE_SHEET_WIDTH;  // 行
        int col = blockId % TEXTURE_SHEET_WIDTH;  // 列
        
        // 添加微小偏移避免纹理接缝
        const float margin = 0.2f;
        
        // 返回四个UV坐标（对应一个面的四个角）
        return new[]
        {
            // 左上角UV坐标
            new Vector2(
                (col + margin) * TEXTURE_TILE_SIZE,
                (row + margin) * TEXTURE_TILE_SIZE
            ),
            // 左下角UV坐标
            new Vector2(
                (col + margin) * TEXTURE_TILE_SIZE,
                (row + 1 - margin) * TEXTURE_TILE_SIZE
            ),
            // 右上角UV坐标
            new Vector2(
                (col + 1 - margin) * TEXTURE_TILE_SIZE,
                (row + margin) * TEXTURE_TILE_SIZE
            ),
            // 右下角UV坐标
            new Vector2(
                (col + 1 - margin) * TEXTURE_TILE_SIZE,
                (row + 1 - margin) * TEXTURE_TILE_SIZE
            )
        };
    }

    // 计算方块的8个顶点
    private Vector3[] 计算方块顶点(Vector3I blockPosition)
    {
        return new[]
        {
            new Vector3(blockPosition.X,     blockPosition.Y,     blockPosition.Z),     // 0: 左-下-前
            new Vector3(blockPosition.X,     blockPosition.Y,     blockPosition.Z + 1), // 1: 左-下-后
            new Vector3(blockPosition.X,     blockPosition.Y + 1, blockPosition.Z),     // 2: 左-上-前
            new Vector3(blockPosition.X,     blockPosition.Y + 1, blockPosition.Z + 1), // 3: 左-上-后
            new Vector3(blockPosition.X + 1, blockPosition.Y,     blockPosition.Z),     // 4: 右-下-前
            new Vector3(blockPosition.X + 1, blockPosition.Y,     blockPosition.Z + 1), // 5: 右-下-后
            new Vector3(blockPosition.X + 1, blockPosition.Y + 1, blockPosition.Z),     // 6: 右-上-前
            new Vector3(blockPosition.X + 1, blockPosition.Y + 1, blockPosition.Z + 1)  // 7: 右-上-后
        };
    }

    // 绘制方块的一个面（由两个三角形组成）
    private void 绘制方块面(SurfaceTool surfaceTool, Vector3[] vertices, Vector2[] uvs)
    {
        // 第一个三角形（逆时针顺序）
        surfaceTool.SetUV(uvs[1]); surfaceTool.AddVertex(vertices[1]); // 左下
        surfaceTool.SetUV(uvs[2]); surfaceTool.AddVertex(vertices[2]); // 右上
        surfaceTool.SetUV(uvs[3]); surfaceTool.AddVertex(vertices[3]); // 右下
        
        // 第二个三角形
        surfaceTool.SetUV(uvs[2]); surfaceTool.AddVertex(vertices[2]); // 右上
        surfaceTool.SetUV(uvs[1]); surfaceTool.AddVertex(vertices[1]); // 左下
        surfaceTool.SetUV(uvs[0]); surfaceTool.AddVertex(vertices[0]); // 左上
        
    }

    // 获取相邻方块的ID
    private int 获取相邻方块ID(Vector3I localPosition, Chunk chunk)
    {
        // 如果位置在当前区块内
        if (localPosition.X >= 0 && localPosition.X < VoxelConst.CHUNK_SIZE_X &&
            localPosition.Y >= 0 && localPosition.Y < VoxelConst.CHUNK_SIZE_Y &&
            localPosition.Z >= 0 && localPosition.Z < VoxelConst.CHUNK_SIZE_Z)
        {

            return chunk.GetVoxel(localPosition.X, localPosition.Y, localPosition.Z);
        }
        
        // 否则需要从相邻区块获取
        // 这里需要实现从VoxelWorld获取相邻区块的逻辑
        // 简化实现：假设相邻方块是空气
        return 0;
    }

    // 判断方块是否透明
    private bool 是透明方块(int blockId)
    {
        // 0是空气，26-29是花草等透明方块
        return blockId == 0 || (blockId > 25 && blockId < 30);
    }


// 添加方块的一个面到网格数据中
    private void 添加方块面(List<Vector3> verts, List<Vector2> uvs, List<int> indices, 
        List<Vector3> normals, ref int indexOffset, Vector3[] vertices, Vector2[] faceUvs, Vector3I normal)
    {
        // 添加顶点
        int startIndex = indexOffset;
        for (int i = 0; i < 4; i++)
        {
            verts.Add(vertices[i]);
            uvs.Add(faceUvs[i]);
            normals.Add(normal);
        }
        
        // 添加三角形索引（两个三角形）
        // 第一个三角形：1,2,3
        indices.Add(startIndex + 1);
        indices.Add(startIndex + 2);
        indices.Add(startIndex + 3);
        
        // 第二个三角形：2,1,0
        indices.Add(startIndex + 2);
        indices.Add(startIndex + 1);
        indices.Add(startIndex + 0);
        
        indexOffset += 4;
    }

	public void 清空网格()
    {
        var arrMesh = new ArrayMesh();
        Mesh = arrMesh;
    }
    

}
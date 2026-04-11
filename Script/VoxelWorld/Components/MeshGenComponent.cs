using Godot;
using System;
using GodotVoxelGame.VoxleData;
using System.Collections.Generic;
using GodotVoxelGame.Components;

public partial class MeshGenComponent : MeshInstance3D
{
    public int DefaultBlockID { get; set; } = 1;
    private Material _blockMaterial = new Material();
    BlockAtlasConfig atlasConfig = new BlockAtlasConfig();
    public CollisionComponent collisionComponent { get; set; }
    
    // 方块面配置
    public enum BlockFace
    {
        Top,
        Bottom,
        Left,
        Right,
        Front,
        Back
    }
    
    // 方块纹理配置结构
    public struct BlockTextureConfig
    {
        public int Top; // 顶部纹理
        public int Bottom; // 底部纹理
        public int Left; // 左侧纹理
        public int Right; // 右侧纹理
        public int Front; // 前面纹理
        public int Back; // 后面纹理
        
        /// <summary>
        /// 构造函数：所有面使用相同纹理
        /// </summary>
        public BlockTextureConfig(int allSides)
        {
            Top = Bottom = Left = Right = Front = Back = allSides;
        }
        
        /// <summary>
        /// 构造函数：顶部和底部使用相同纹理，四周使用相同纹理
        /// </summary>
        public BlockTextureConfig(int topBottom, int sides)
        {
            Top = Bottom = topBottom;
            Left = Right = Front = Back = sides;
        }
        
        /// <summary>
        /// 构造函数：顶部和底部使用不同纹理，四周使用相同纹理
        /// </summary>
        /// <param name="top">顶部纹理</param>
        /// <param name="bottom">底部纹理</param>
        /// <param name="sides">四周面（左、右、前、后）纹理</param>
        public BlockTextureConfig(int top, int bottom, int sides)
        {
            Top = top;
            Bottom = bottom;
            Left = Right = Front = Back = sides;
        }
        
        /// <summary>
        /// 构造函数：所有面都不同
        /// </summary>
        public BlockTextureConfig(int top, int bottom, int left, int right, int front, int back)
        {
            Top = top;
            Bottom = bottom;
            Left = left;
            Right = right;
            Front = front;
            Back = back;
        }
    }
    
    // 方块纹理配置字典
    private Dictionary<int, BlockTextureConfig> blockTextureConfigs = new Dictionary<int, BlockTextureConfig>()
    {
        { 0, new BlockTextureConfig(0) },  // 空气
        { 1, new BlockTextureConfig(15) },  // 石头
        { 2, new BlockTextureConfig(2) },  // 泥土
        { 3, new BlockTextureConfig(0, 2, 1) },  // 草地：顶部草(0)，底部泥土(2)，侧面草(1)
        { 4, new BlockTextureConfig(4) },  // 木板
        { 5, new BlockTextureConfig(10, 5) },  // 熔炉：顶部底部(31)，侧面(5)
        { 6, new BlockTextureConfig(6) },  // 树苗
        { 7, new BlockTextureConfig(7) },  // 基岩
        { 8, new BlockTextureConfig(8) },  // 水
        { 9, new BlockTextureConfig(9) },  // 静止的水
        { 10, new BlockTextureConfig(10) }, // 熔岩
        { 11, new BlockTextureConfig(11) }, // 静止的熔岩
        { 12, new BlockTextureConfig(30, 12) }, // 木头：顶部底部(30)，侧面(12)
        { 13, new BlockTextureConfig(13) }, // 树叶
        { 14, new BlockTextureConfig(14) }, // 玻璃
        { 15, new BlockTextureConfig(15) }, // 红矿石
        { 16, new BlockTextureConfig(16) }, // 钻石矿石
        { 17, new BlockTextureConfig(17) }, // 工作台
        { 18, new BlockTextureConfig(18) }, // 仙人掌
        { 19, new BlockTextureConfig(4, 19) }, // 书架：顶部底部(4)，侧面(19)
        { 20, new BlockTextureConfig(20) }, // 南瓜
        { 21, new BlockTextureConfig(21) }, // 南瓜灯
        { 22, new BlockTextureConfig(22) }, // 蛋糕
        { 23, new BlockTextureConfig(23) }, // 锁链
        { 24, new BlockTextureConfig(24) }, // 萤石
        { 25, new BlockTextureConfig(25) }, // 铁轨
        { 26, new BlockTextureConfig(26) }, // 花1
        { 27, new BlockTextureConfig(27) }, // 花2
        { 28, new BlockTextureConfig(28) }, // 草
        { 29, new BlockTextureConfig(29) }, // 蘑菇
    };

    public override void _Ready()
    {
        atlasConfig = G.Instance.AtlasConfig;
        _blockMaterial = G.Instance.AtlasConfig.AtlasTexture;

        collisionComponent = new CollisionComponent();
        AddChild(collisionComponent);
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
                    
                    // 处理花草方块的特殊绘制方式
                    if (blockId == 27 || blockId == 28)
                    {
                        var uvConfig = 获取方块UV配置(blockId, BlockFace.Front);
                        // 第一个交叉面
                        添加方块面(verts, uvs, indices, normals, ref indexOffset, 
                            [blockVerts[2], blockVerts[0], blockVerts[7], blockVerts[5]], uvConfig, new Vector3I(0, 0, 0));
                        // 第二个交叉面
                        添加方块面(verts, uvs, indices, normals, ref indexOffset, 
                            [blockVerts[3], blockVerts[1], blockVerts[6], blockVerts[4]], uvConfig, new Vector3I(0, 0, 0));
                        continue;
                    }
                    
                    // 检查六个方向的面是否需要渲染
                    var blockPos = (Vector3I)worldPos;
                    
                    bool 碰撞体 = false;
                    // 左面
                    var otherPosition = blockPos + Vector3I.Left;
                    var otherId = 获取相邻方块ID(otherPosition, chunk);
                    if (是透明方块(otherId))
                    {
                        var uvConfig = 获取方块UV配置(blockId, BlockFace.Left);
                        添加方块面(verts, uvs, indices, normals, ref indexOffset, 
                            [blockVerts[2], blockVerts[0], blockVerts[3], blockVerts[1]], uvConfig, Vector3I.Left);

                    }
                    
                    // 右面
                    otherPosition = blockPos + Vector3I.Right;
                    otherId = 获取相邻方块ID(otherPosition, chunk);
                    if (是透明方块(otherId))
                    {
                        var uvConfig = 获取方块UV配置(blockId, BlockFace.Right);
                        添加方块面(verts, uvs, indices, normals, ref indexOffset, 
                            [blockVerts[7], blockVerts[5], blockVerts[6], blockVerts[4]], uvConfig, Vector3I.Right);
                        碰撞体 = true;
                    }
                    
                    // 前面
                    otherPosition = blockPos + Vector3I.Forward;
                    otherId = 获取相邻方块ID(otherPosition, chunk);
                    if (是透明方块(otherId))
                    {
                        var uvConfig = 获取方块UV配置(blockId, BlockFace.Front);
                        添加方块面(verts, uvs, indices, normals, ref indexOffset, 
                            [blockVerts[6], blockVerts[4], blockVerts[2], blockVerts[0]], uvConfig, Vector3I.Forward);
                        碰撞体 = true;
                    }
                    
                    // 后面
                    otherPosition = blockPos + Vector3I.Back;
                    otherId = 获取相邻方块ID(otherPosition, chunk);
                    if (是透明方块(otherId))
                    {
                        var uvConfig = 获取方块UV配置(blockId, BlockFace.Back);
                        添加方块面(verts, uvs, indices, normals, ref indexOffset, 
                            [blockVerts[3], blockVerts[1], blockVerts[7], blockVerts[5]], uvConfig, Vector3I.Back);
                        碰撞体 = true;
                    }
                    
                    // 下面
                    otherPosition = blockPos + Vector3I.Down;
                    otherId = 获取相邻方块ID(otherPosition, chunk);
                    if (是透明方块(otherId))
                    {
                        var uvConfig = 获取方块UV配置(blockId, BlockFace.Bottom);
                        添加方块面(verts, uvs, indices, normals, ref indexOffset, 
                            [blockVerts[4], blockVerts[5], blockVerts[0], blockVerts[1]], uvConfig, Vector3I.Down);
                        碰撞体 = true;
                    }
                    
                    // 上面
                    otherPosition = blockPos + Vector3I.Up;
                    otherId = 获取相邻方块ID(otherPosition, chunk);
                    if (是透明方块(otherId))
                    {
                        var uvConfig = 获取方块UV配置(blockId, BlockFace.Top);
                        添加方块面(verts, uvs, indices, normals, ref indexOffset, 
                            [blockVerts[2], blockVerts[3], blockVerts[6], blockVerts[7]], uvConfig, Vector3I.Up);
                        碰撞体 = true;
                    }

                    //添加方块碰撞体(blockPos);
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

    // 根据方块ID和面获取对应的UV配置
    private Vector2[] 获取方块UV配置(int blockId, BlockFace face)
    {
        if (!blockTextureConfigs.ContainsKey(blockId))
        {
            // 如果方块没有配置，使用默认方块ID
            blockId = DefaultBlockID;
        }
        
        var config = blockTextureConfigs[blockId];
        int textureId = face switch
        {
            BlockFace.Top => config.Top,
            BlockFace.Bottom => config.Bottom,
            BlockFace.Left => config.Left,
            BlockFace.Right => config.Right,
            BlockFace.Front => config.Front,
            BlockFace.Back => config.Back,
            _ => blockId
        };
        
        return 计算方块UV(textureId);
    }
    
    // 计算方块UV坐标的核心方法
    private Vector2[] 计算方块UV(int textureId)
    {
        const int TEXTURE_SHEET_WIDTH = 8;  // 纹理图集每行8个纹理
        const float TEXTURE_TILE_SIZE = 1.0f / TEXTURE_SHEET_WIDTH;  // 每个纹理的大小
        
        // 计算纹理在图集中的行列位置
        int row = textureId / TEXTURE_SHEET_WIDTH;  // 行
        int col = textureId % TEXTURE_SHEET_WIDTH;  // 列
        
        // 添加微小偏移避免纹理接缝
        const float margin = 0.01f;
        
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

    private void 添加方块碰撞体(Vector3I blockPos)
    {
        var staticBody = new StaticBody3D
        {
            Name = $"Block_{blockPos.X}_{blockPos.Y}_{blockPos.Z}",
            Position = blockPos
        };
        
        var collisionShape = new CollisionShape3D
        {
            Shape = new BoxShape3D
            {
                Size = new Vector3(1, 1, 1)
            }
        };
        
        staticBody.AddChild(collisionShape);
        AddChild(staticBody);
    }

    private bool 需要碰撞体(int blockId)
    {
        return true;
    }


    public void 清空网格()
    {
        var arrMesh = new ArrayMesh();
        Mesh = arrMesh;
    }
}
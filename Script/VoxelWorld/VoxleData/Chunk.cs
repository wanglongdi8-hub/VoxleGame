using System.Collections.Generic;
using Godot;

namespace GodotVoxelGame.VoxleData;
public class Chunk
{
    public Vector3I ChunkPosition { get; set; }
    public 区块顶点数据 顶点数据 { get; set; } = new();
    public ConcavePolygonShape3D 碰撞数据 {get; set;} = new();
    private ushort[] _voxels = new ushort[VoxelConst.CHUNK_VOLUME];
    

    public Chunk(Vector3I 区块坐标)
    {
        ChunkPosition = 区块坐标;
    }

    public void 计算数据()
    {
        计算顶点数据();
    }

    public void 重新计算顶点数据()
    {
        顶点数据 = new();
        计算顶点数据();
    }
    
    // 方块数据 *****************************************************************************
    public ushort GetVoxel(int x, int y, int z)
    {
        int index = Utils.ToFlatIndex(x, y, z, VoxelConst.CHUNK_SIZE_Y, VoxelConst.CHUNK_SIZE_X);
        return _voxels[index];
    }
    
    public void SetVoxel(int x, int y, int z, ushort id)
    {
        if (!IsInBounds(x, y, z))
            return;
        
        int index = Utils.ToFlatIndex(x, y, z, VoxelConst.CHUNK_SIZE_Y, VoxelConst.CHUNK_SIZE_X);
        _voxels[index] = id;
    }
    
    private static bool IsInBounds(int x, int y, int z)
    {
        return x >= 0 && x < VoxelConst.CHUNK_SIZE_X &&
               y >= 0 && y < VoxelConst.CHUNK_SIZE_Y &&
               z >= 0 && z < VoxelConst.CHUNK_SIZE_Z;
    }

    public void ClearVoxels()
    {
        System.Array.Clear(_voxels, 0, _voxels.Length);
    }

    // 顶点数据 *****************************************************************************

    // 方块面配置
    private enum BlockFace
    {
        Top,
        Bottom,
        Left,
        Right,
        Front,
        Back
    }

    // 方块纹理配置结构
    private struct BlockTextureConfig
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
        { 1, new BlockTextureConfig(1) },  // 石头
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


    private void 计算顶点数据()
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
                    var blockId = GetVoxel(x, y, z);
                    if (blockId == 0) continue;

                    // 获取方块的世界坐标
                    var worldPos = new Vector3(x, y, z) + (Vector3)ChunkPosition * VoxelConst.CHUNK_SIZE_X;
                    
                    // 计算方块的8个顶点
                    var blockVerts = 计算方块顶点((Vector3I)worldPos);
                    
                    // 检查六个方向的面是否需要渲染
                    var blockPos = (Vector3I)worldPos;
                    
                    // 左面
                    var otherPosition = blockPos + Vector3I.Left;
                    var otherId = 获取相邻方块ID(otherPosition);

                    if (是透明方块(otherId))
                    {
                        var uvConfig = 获取方块UV配置(blockId, BlockFace.Left);
                        添加方块面(verts, uvs, indices, normals, ref indexOffset, 
                            [blockVerts[2], blockVerts[0], blockVerts[3], blockVerts[1]], uvConfig, Vector3I.Left);

                    }
                    
                    // 右面
                    otherPosition = blockPos + Vector3I.Right;
                    otherId = 获取相邻方块ID(otherPosition);
                    if (是透明方块(otherId))
                    {
                        var uvConfig = 获取方块UV配置(blockId, BlockFace.Right);
                        添加方块面(verts, uvs, indices, normals, ref indexOffset, 
                            [blockVerts[7], blockVerts[5], blockVerts[6], blockVerts[4]], uvConfig, Vector3I.Right);
                    }
                    
                    // 前面
                    otherPosition = blockPos + Vector3I.Forward;
                    otherId = 获取相邻方块ID(otherPosition);
                    if (是透明方块(otherId))
                    {
                        var uvConfig = 获取方块UV配置(blockId, BlockFace.Front);
                        添加方块面(verts, uvs, indices, normals, ref indexOffset, 
                            [blockVerts[6], blockVerts[4], blockVerts[2], blockVerts[0]], uvConfig, Vector3I.Forward);
                    }
                    
                    // 后面
                    otherPosition = blockPos + Vector3I.Back;
                    otherId = 获取相邻方块ID(otherPosition);
                    if (是透明方块(otherId))
                    {
                        var uvConfig = 获取方块UV配置(blockId, BlockFace.Back);
                        添加方块面(verts, uvs, indices, normals, ref indexOffset, 
                            [blockVerts[3], blockVerts[1], blockVerts[7], blockVerts[5]], uvConfig, Vector3I.Back);
                    }
                    
                    // 下面
                    otherPosition = blockPos + Vector3I.Down;
                    otherId = 获取相邻方块ID(otherPosition);
                    if (是透明方块(otherId))
                    {
                        var uvConfig = 获取方块UV配置(blockId, BlockFace.Bottom);
                        添加方块面(verts, uvs, indices, normals, ref indexOffset, 
                            [blockVerts[4], blockVerts[5], blockVerts[0], blockVerts[1]], uvConfig, Vector3I.Down);
                    }
                    
                    // 上面
                    otherPosition = blockPos + Vector3I.Up;
                    otherId = 获取相邻方块ID(otherPosition);
                    if (是透明方块(otherId))
                    {
                        var uvConfig = 获取方块UV配置(blockId, BlockFace.Top);
                        添加方块面(verts, uvs, indices, normals, ref indexOffset, 
                            [blockVerts[2], blockVerts[3], blockVerts[6], blockVerts[7]], uvConfig, Vector3I.Up);
                    }
                }
            }
        }

        顶点数据.vert = verts;
        顶点数据.uv = uvs;
        顶点数据.indice = indices;
        顶点数据.normal = normals;
        
        // 生成碰撞数据
        生成碰撞数据();
    }

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

    private int 获取相邻方块ID(Vector3I localPosition)
    {
        // 如果位置在当前区块内
        if (localPosition.X >= 0 && localPosition.X < VoxelConst.CHUNK_SIZE_X &&
            localPosition.Y >= 0 && localPosition.Y < VoxelConst.CHUNK_SIZE_Y &&
            localPosition.Z >= 0 && localPosition.Z < VoxelConst.CHUNK_SIZE_Z)
        {
            return GetVoxel(localPosition.X, localPosition.Y, localPosition.Z);
        }
        
        // 否则需要从相邻区块获取
        // 这里需要实现从VoxelWorld获取相邻区块的逻辑
        // 简化实现：假设相邻方块是空气
        return 0;
    }

    private bool 是透明方块(int blockId)
    {
        // 0是空气，5000-9999是花草等透明方块
        return blockId == 0 || (blockId > 5000 && blockId < 10000);
    }

    // 根据方块ID和面获取对应的UV配置
    private Vector2[] 获取方块UV配置(int blockId, BlockFace face)
    {
        if (!blockTextureConfigs.ContainsKey(blockId))
        {
            // 如果方块没有配置，使用默认方块ID
            // GD.Print($"方块ID {blockId} 没有配置UV");
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
        const float margin = 0.5f;
        
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

    // 碰撞数据 *****************************************************************************
    public void 生成碰撞数据()
    {
        if (顶点数据.indice.Count == 0)
        {
            碰撞数据.Data = new Vector3[0];
            return;
        }
        
        List<Vector3> collisionFaces = new List<Vector3>();
        
        for (int i = 0; i < 顶点数据.indice.Count; i += 3)
        {
            int idx1 = 顶点数据.indice[i];
            int idx2 = 顶点数据.indice[i + 1];
            int idx3 = 顶点数据.indice[i + 2];
            
            if (idx1 < 顶点数据.vert.Count && 
                idx2 < 顶点数据.vert.Count && 
                idx3 < 顶点数据.vert.Count)
            {
                Vector3 v1 = 顶点数据.vert[idx1];
                Vector3 v2 = 顶点数据.vert[idx2];
                Vector3 v3 = 顶点数据.vert[idx3];

                collisionFaces.Add(v1);
                collisionFaces.Add(v2);
                collisionFaces.Add(v3);
            }
        }
        
        碰撞数据.Data = collisionFaces.ToArray();
    }
    
    private void 生成碰撞面数组(区块顶点数据 meshData, out int 三角形数量)
    {
        三角形数量 = 0;
        List<Vector3> collisionFaces = new List<Vector3>();
        
        // 使用三角形索引生成碰撞面
        for (int i = 0; i < meshData.indice.Count; i += 3)
        {
            if (i + 2 < meshData.indice.Count)
            {
                int idx1 = meshData.indice[i];
                int idx2 = meshData.indice[i + 1];
                int idx3 = meshData.indice[i + 2];
                
                if (idx1 < meshData.vert.Count && 
                    idx2 < meshData.vert.Count && 
                    idx3 < meshData.vert.Count)
                {
                    Vector3 v1 = meshData.vert[idx1];
                    Vector3 v2 = meshData.vert[idx2];
                    Vector3 v3 = meshData.vert[idx3];     

                    collisionFaces.Add(v1);
                    collisionFaces.Add(v2);
                    collisionFaces.Add(v3);
                    三角形数量++;
                    
                }
            }
        }
        
        碰撞数据.Data = collisionFaces.ToArray();
    }
}
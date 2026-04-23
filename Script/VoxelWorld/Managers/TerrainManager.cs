using Godot;
using GodotVoxelGame.VoxleData;
using System;
using System.Collections.Generic;

public partial class TerrainManager : Node
{
    private VoxelWorld _voxelWorld;
    
    [ExportGroup("噪声设置")]
    //控制噪声的频率。较小的缩放值会产生更大范围的地形特征（如山脉），较大的缩放值会产生更细致的地形。
    [Export] public float 地形缩放 = 0.001f; // 增加缩放系数，使其更合理
    //控制噪声的振幅，即地形的高度变化。增加幅度会使地形起伏更大。
    [Export] public float 地形幅度 = 1.0f;
    //整个地形的基准高度。如果想要更多的海洋，可以降低基础高度；如果想要更多的高山，可以提高基础高度。
    [Export] public int 基础高度 = 100; // 降低基础高度
    [Export] public int 高度变化 = 512; //控制噪声值乘以多少倍来影响高度。增加高度变化会使地形更加崎岖。
    
    [ExportGroup("FBM设置")]
    //控制FBM的层数。增加倍频数量会增加地形的细节，但也会增加计算量。通常6-8个倍频已经足够
    [Export] public int 倍频数量 = 6;
    //控制每一层噪声的振幅衰减。通常设置为0.5，但可以调整以获得不同的地形特征。降低增益会使更高频的噪声对地形的影响变小，从而产生更平滑的地形。
    [Export] public float 倍频增益 = 0.5f; 
    // 控制每一层噪声的频率倍增。通常设置为2.0，但也可以调整。增加频率会使更高频的噪声更密集，从而产生更复杂的地形。
    [Export] public float 倍频频率 = 2.0f; 
    
    [Export] public int 世界高度上限 = 4096; // 降低世界高度上限
    [Export] public int 海平面高度 = 70;
    
    // 随机数种子
    [Export] public int 随机种子 = 12345;
    
    // 用于存储随机值的字典
    private Dictionary<Vector3I, float> _randomCache = new Dictionary<Vector3I, float>();
    private RandomNumberGenerator _rng = new RandomNumberGenerator();

    /*****************        地形生成核心算法         **********************/

    /// <summary>
    /// 哈希函数，为每个整数坐标生成伪随机值
    /// 基于 Inigo Quilez 文章中的 myRandomMagic
    /// </summary>
    private float Hash(Vector3I p)
    {
        // 使用缓存以提高性能
        if (_randomCache.TryGetValue(p, out float value))
            return value;
        
        // 使用Godot内置的随机数生成器
        _rng.Seed = (ulong)(p.X * 198491317 + p.Y * 6542989 + p.Z * 357239 + 随机种子);
        value = _rng.Randf(); // 返回 [0, 1) 范围的值
        
        _randomCache[p] = value;
        return value;
    }

    /// <summary>
    /// 3D Value Noise 及其三个导数的计算
    /// 返回 Vector4 (噪声值, x导数, y导数, z导数)
    /// 使用五次多项式以获得更好的连续性
    /// </summary>
    private Godot.Vector4 Noised(Godot.Vector3 x)
    {
        Vector3I p = new Vector3I(
            Mathf.FloorToInt(x.X),
            Mathf.FloorToInt(x.Y),
            Mathf.FloorToInt(x.Z)
        );
        
        Godot.Vector3 w = new Vector3(x.X - p.X, x.Y - p.Y, x.Z - p.Z);
        
        // 五次多项式: u(w) = 6w⁵ - 15w⁴ + 10w³
        Vector3 w2 = w * w;
        Vector3 w3 = w2 * w;
        Vector3 w4 = w3 * w;
        Vector3 w5 = w4 * w;
        
        Vector3 u = 6.0f * w5 - 15.0f * w4 + 10.0f * w3;
        Vector3 du = 30.0f * w4 - 60.0f * w3 + 30.0f * w2; // 导数
        
        // 获取立方体8个角点的随机值
        float a = Hash(p + new Vector3I(0, 0, 0));
        float b = Hash(p + new Vector3I(1, 0, 0));
        float c = Hash(p + new Vector3I(0, 1, 0));
        float d = Hash(p + new Vector3I(1, 1, 0));
        float e = Hash(p + new Vector3I(0, 0, 1));
        float f = Hash(p + new Vector3I(1, 0, 1));
        float g = Hash(p + new Vector3I(0, 1, 1));
        float h = Hash(p + new Vector3I(1, 1, 1));
        
        // 计算插值系数
        float k0 = a;
        float k1 = b - a;
        float k2 = c - a;
        float k3 = e - a;
        float k4 = a - b - c + d;
        float k5 = a - c - e + g;
        float k6 = a - b - e + f;
        float k7 = -a + b + c - d + e - f - g + h;
        
        // 计算噪声值 (映射到 [-1, 1] 范围)
        float n = k0 + k1 * u.X + k2 * u.Y + k3 * u.Z
                 + k4 * u.X * u.Y + k5 * u.Y * u.Z + k6 * u.Z * u.X
                 + k7 * u.X * u.Y * u.Z;
        float noiseValue = -1.0f + 2.0f * n;
        
        // 计算导数
        float dx = du.X * (k1 + k4 * u.Y + k6 * u.Z + k7 * u.Y * u.Z);
        float dy = du.Y * (k2 + k5 * u.Z + k4 * u.X + k7 * u.Z * u.X);
        float dz = du.Z * (k3 + k6 * u.X + k5 * u.Y + k7 * u.X * u.Y);
        Vector3 derivatives = 2.0f * new Vector3(dx, dy, dz);
        
        return new Vector4(noiseValue, derivatives.X, derivatives.Y, derivatives.Z);
    }

    /// <summary>
    /// 2D 地形高度函数（基于 Inigo Quilez 文章的 terrain 函数）
    /// 使用导数调制每个倍频的贡献，产生更自然的地形
    /// </summary>
    public float TerrainHeight(Vector2 p, int octaves = 8)
    {
        float totalHeight = 0.0f;
        float amplitude = 地形幅度;
        Vector2 derivatives = Vector2.Zero;
        Vector2 currentP = p * 地形缩放;
        
        // 旋转矩阵，用于打破各向同性
        float angle = 0.8f; // 对应 mat2(0.8, -0.6, 0.6, 0.8)
        float sinA = Mathf.Sin(angle);
        float cosA = Mathf.Cos(angle);
        
        for (int i = 0; i < octaves; i++)
        {
            // 计算噪声和导数
            Vector4 n = Noised(new Vector3(currentP.X, currentP.Y, 0));
            
            // 累计导数
            derivatives += new Vector2(n.Y, n.Z);
            
            // 使用导数调制：平坦区域贡献大，陡峭区域贡献小
            totalHeight += amplitude * n.X / (1.0f + derivatives.LengthSquared());
            
            // 更新参数
            amplitude *= 倍频增益;
            
            // 应用旋转和频率倍增
            float newX = cosA * currentP.X - sinA * currentP.Y;
            float newY = sinA * currentP.X + cosA * currentP.Y;
            currentP = new Vector2(newX, newY) * 倍频频率;
        }
        
        return 基础高度 + totalHeight * 高度变化;
    }

    /// <summary>
    /// 改进的地形生成方法
    /// 支持多层地形（地表、地下、洞穴）
    /// </summary>
    public Dictionary<Vector3I, Chunk> 生成地形数据(Vector2I chunkPos)
    {
        Dictionary<Vector3I, Chunk> chunkDic = new Dictionary<Vector3I, Chunk>();
        
        int worldStartX = chunkPos.X * VoxelConst.CHUNK_SIZE_X;
        int worldStartZ = chunkPos.Y * VoxelConst.CHUNK_SIZE_Z;
        
        // 预计算区块内所有位置的地形高度
        float[,] heights = new float[VoxelConst.CHUNK_SIZE_X, VoxelConst.CHUNK_SIZE_Z];
        
        for (int x = 0; x < VoxelConst.CHUNK_SIZE_X; x++)
        {
            for (int z = 0; z < VoxelConst.CHUNK_SIZE_Z; z++)
            {
                int worldX = worldStartX + x;
                int worldZ = worldStartZ + z;
                
                // 计算地形高度
                float height = TerrainHeight(new Vector2(worldX, worldZ), 倍频数量);
                
                // 应用平滑和钳制
                height = Mathf.Clamp(height, 0, 世界高度上限 - 1);
                heights[x, z] = height;
                
                // 计算区块位置
                int chunkY = (int)height / VoxelConst.CHUNK_SIZE_Y;
                int localY = (int)height % VoxelConst.CHUNK_SIZE_Y;
                
                Vector3I newChunkPos = new Vector3I(chunkPos.X, chunkY, chunkPos.Y);
                
                if (!chunkDic.ContainsKey(newChunkPos))
                {
                    Chunk newChunk = new Chunk(newChunkPos);
                    //newChunk.SetVoxel(x, localY, z, 获取体素类型(height, worldX, worldZ, (int)height));
                    newChunk.SetVoxel(x, localY, z, 1);
                    chunkDic.Add(newChunkPos, newChunk);
                }
                else
                {
                    //chunkDic[newChunkPos].SetVoxel(x, localY, z, 获取体素类型(height, worldX, worldZ, (int)height));
                    chunkDic[newChunkPos].SetVoxel(x, localY, z, 1);
                }
                
                // 填充地表以下的体素
                //填充地下体素(x, z, (int)height, chunkPos, ref chunkDic, worldX, worldZ);
            }
        }
        
        // 添加洞穴和结构
        //生成洞穴和结构(ref chunkDic, heights, chunkPos);
        
        return chunkDic;
    }

    /// <summary>
    /// 填充地表以下的体素
    /// </summary>
    private void 填充地下体素(int x, int z, int surfaceHeight, Vector2I chunkPos, 
                            ref Dictionary<Vector3I, Chunk> chunkDic, int worldX, int worldZ)
    {
        int startChunkY = surfaceHeight / VoxelConst.CHUNK_SIZE_Y;
        
        for (int y = 0; y < surfaceHeight; y++)
        {
            int chunkY = y / VoxelConst.CHUNK_SIZE_Y;
            int localY = y % VoxelConst.CHUNK_SIZE_Y;
            
            Vector3I chunkKey = new Vector3I(chunkPos.X, chunkY, chunkPos.Y);
            
            if (!chunkDic.ContainsKey(chunkKey))
            {
                Chunk newChunk = new Chunk(chunkKey);
                newChunk.SetVoxel(x, localY, z, 获取地下体素类型(y, surfaceHeight, worldX, worldZ));
                chunkDic.Add(chunkKey, newChunk);
            }
            else
            {
                chunkDic[chunkKey].SetVoxel(x, localY, z, 获取地下体素类型(y, surfaceHeight, worldX, worldZ));
            }
        }
    }

    /// <summary>
    /// 获取地表体素类型
    /// </summary>
    private ushort 获取体素类型(float height, int worldX, int worldZ, int y)
    {
        // 根据高度、噪声等因素决定体素类型
        
        // 海洋/水体
        if (height <= 海平面高度)
        {
            if (y == (int)height) // 水面
                return VoxelConst.水;
            else if (y < (int)height && y > (int)height - 3) // 水下
                return VoxelConst.沙子;
        }
        
        // 雪地
        if (height > 海平面高度 + 100)
        {
            return VoxelConst.雪块;
        }
        
        // 沙滩
        if (height <= 海平面高度 + 3)
        {
            return VoxelConst.沙子;
        }
        
        // 根据噪声添加草地变化
        float noise = Hash(new Vector3I(worldX / 4, y, worldZ / 4));
        if (y == (int)height)
        {
            if (noise > 0.7f)
                return VoxelConst.草地;
            else
                return VoxelConst.泥土;
        }
        
        return VoxelConst.石头; // 默认
    }

    /// <summary>
    /// 获取地下体素类型
    /// </summary>
    private ushort 获取地下体素类型(int depth, int surfaceHeight, int worldX, int worldZ)
    {
        // 深度越深，生成矿石的概率越高
        
        // 顶层土壤
        if (depth >= surfaceHeight - 3)
        {
            return VoxelConst.泥土;
        }
        
        // 添加矿石层
        if (depth < surfaceHeight - 10)
        {
            float oreNoise = Hash(new Vector3I(worldX / 8, depth, worldZ / 8));
            
            // 煤矿
            if (depth < 128 && oreNoise > 0.9f)
                return VoxelConst.煤矿石;
            
            // 铁矿
            if (depth < 96 && oreNoise > 0.92f)
                return VoxelConst.铁矿石;
            
            // 金矿
            if (depth < 32 && oreNoise > 0.95f)
                return VoxelConst.金矿石;
        }
        
        return VoxelConst.石头; // 默认岩石
    }

    /// <summary>
    /// 生成洞穴和地下结构
    /// </summary>
    private void 生成洞穴和结构(ref Dictionary<Vector3I, Chunk> chunkDic, float[,] heights, Vector2I chunkPos)
    {
        int worldStartX = chunkPos.X * VoxelConst.CHUNK_SIZE_X;
        int worldStartZ = chunkPos.Y * VoxelConst.CHUNK_SIZE_Z;
        
        for (int x = 0; x < VoxelConst.CHUNK_SIZE_X; x++)
        {
            for (int z = 0; z < VoxelConst.CHUNK_SIZE_Z; z++)
            {
                int worldX = worldStartX + x;
                int worldZ = worldStartZ + z;
                int surfaceHeight = (int)heights[x, z];
                
                // 在表面高度以下生成洞穴
                for (int y = 5; y < surfaceHeight - 5; y++)
                {
                    // 使用3D噪声决定洞穴位置
                    float caveNoise = Hash(new Vector3I(
                        worldX / 10, 
                        y / 10, 
                        worldZ / 10
                    ));
                    
                    // 洞穴噪声
                    Vector4 caveNoise3D = Noised(new Vector3(worldX * 0.05f, y * 0.05f, worldZ * 0.05f));
                    
                    // 洞穴的条件：噪声值接近0.5，并且深度足够
                    if (Mathf.Abs(caveNoise3D.X) < 0.1f && y < surfaceHeight - 10)
                    {
                        int chunkY = y / VoxelConst.CHUNK_SIZE_Y;
                        int localY = y % VoxelConst.CHUNK_SIZE_Y;
                        Vector3I chunkKey = new Vector3I(chunkPos.X, chunkY, chunkPos.Y);
                        
                        if (chunkDic.ContainsKey(chunkKey))
                        {
                            // 创建洞穴（空气）
                            chunkDic[chunkKey].SetVoxel(x, localY, z, VoxelConst.空气);
                        }
                    }
                }
                
                // 在地表生成树木
                尝试生成树木(x, z, surfaceHeight, worldX, worldZ, chunkPos, ref chunkDic);
            }
        }
    }

    /// <summary>
    /// 在地表生成树木
    /// </summary>
    private void 尝试生成树木(int x, int z, int surfaceHeight, int worldX, int worldZ, 
                          Vector2I chunkPos, ref Dictionary<Vector3I, Chunk> chunkDic)
    {
        // 只在陆地且足够高的地方生成树木
        if (surfaceHeight > 海平面高度 + 5)
        {
            float treeNoise = Hash(new Vector3I(worldX / 5, 0, worldZ / 5));
            
            if (treeNoise > 0.85f) // 15%的概率生成树木
            {
                int treeHeight = 4 + (int)(Hash(new Vector3I(worldX, 0, worldZ)) * 3);
                
                for (int y = 1; y <= treeHeight; y++)
                {
                    int chunkY = (surfaceHeight + y) / VoxelConst.CHUNK_SIZE_Y;
                    int localY = (surfaceHeight + y) % VoxelConst.CHUNK_SIZE_Y;
                    Vector3I chunkKey = new Vector3I(chunkPos.X, chunkY, chunkPos.Y);
                    
                    if (!chunkDic.ContainsKey(chunkKey))
                    {
                        Chunk newChunk = new Chunk(chunkKey);
                        newChunk.SetVoxel(x, localY, z, VoxelConst.木头);
                        chunkDic.Add(chunkKey, newChunk);
                    }
                    else
                    {
                        chunkDic[chunkKey].SetVoxel(x, localY, z, VoxelConst.木头);
                    }
                }
                
                // 生成树叶
                if (surfaceHeight + treeHeight + 1 < 世界高度上限)
                {
                    int leavesY = surfaceHeight + treeHeight;
                    生成树叶层(x, z, leavesY, worldX, worldZ, chunkPos, ref chunkDic);
                }
            }
        }
    }

    /// <summary>
    /// 生成树叶层
    /// </summary>
    private void 生成树叶层(int x, int z, int centerY, int worldX, int worldZ, 
                         Vector2I chunkPos, ref Dictionary<Vector3I, Chunk> chunkDic)
    {
        int radius = 2;
        
        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dz = -radius; dz <= radius; dz++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    // 创建球形树叶
                    float distance = Mathf.Sqrt(dx * dx + dz * dz + dy * dy);
                    
                    if (distance <= radius)
                    {
                        int targetX = x + dx;
                        int targetZ = z + dz;
                        int targetY = centerY + dy;
                        
                        // 检查是否在区块范围内
                        if (targetX >= 0 && targetX < VoxelConst.CHUNK_SIZE_X && 
                            targetZ >= 0 && targetZ < VoxelConst.CHUNK_SIZE_Z)
                        {
                            int chunkY = targetY / VoxelConst.CHUNK_SIZE_Y;
                            int localY = targetY % VoxelConst.CHUNK_SIZE_Y;
                            Vector3I chunkKey = new Vector3I(chunkPos.X, chunkY, chunkPos.Y);
                            
                            if (chunkDic.ContainsKey(chunkKey))
                            {
                                ushort existingVoxel = chunkDic[chunkKey].GetVoxel(targetX, localY, targetZ);
                                
                                // 只在空气或树叶上放置树叶
                                if (existingVoxel == VoxelConst.空气 || existingVoxel == VoxelConst.树叶)
                                {
                                    chunkDic[chunkKey].SetVoxel(targetX, localY, targetZ, VoxelConst.树叶);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    /*****************        初始化         **********************/
    public override void _Ready()
    {
        _voxelWorld = GetParent<VoxelWorld>();
        初始化随机数生成器();
    }

    private void 初始化随机数生成器()
    {
        _rng = new RandomNumberGenerator();
        _rng.Seed = (ulong)随机种子;
        _randomCache.Clear();
    }

    /*****************        测试功能         **********************/
    public Chunk 生成测试地形(Vector3I chunkPos)
    {
        Chunk newChunk = new Chunk(chunkPos);
        
        // 生成简单的地形用于测试
        for (int x = 0; x < VoxelConst.CHUNK_SIZE_X; x++)
        {
            for (int z = 0; z < VoxelConst.CHUNK_SIZE_Z; z++)
            {
                int worldX = chunkPos.X * VoxelConst.CHUNK_SIZE_X + x;
                int worldZ = chunkPos.Z * VoxelConst.CHUNK_SIZE_Z + z;
                
                // 使用改进的地形函数
                float height = TerrainHeight(new Vector2(worldX, worldZ), 3);
                int surfaceY = Mathf.Clamp((int)height, 0, VoxelConst.CHUNK_SIZE_Y - 1);
                
                for (int y = 0; y < VoxelConst.CHUNK_SIZE_Y; y++)
                {
                    ushort voxelType = VoxelConst.空气;
                    
                    if (y == surfaceY)
                    {
                        voxelType = VoxelConst.草地;
                    }
                    else if (y < surfaceY)
                    {
                        if (y > surfaceY - 3)
                            voxelType = VoxelConst.泥土;
                        else
                            voxelType = VoxelConst.石头;
                    }
                    
                    newChunk.SetVoxel(x, y, z, voxelType);
                }
            }
        }
        
        return newChunk;
    }
}
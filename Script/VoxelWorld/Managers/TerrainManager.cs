using Godot;
using GodotVoxelGame.VoxleData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[Tool]public partial class TerrainManager : Node
{
	private VoxelWorld _voxelWorld;

    private FastNoiseLite _地形噪声 = new FastNoiseLite();
    private FastNoiseLite _细节噪声 = new FastNoiseLite();

    [Export] public float 地形缩放 = 0.01f;
    [Export] public float 地形幅度 = 32.0f;
    [Export] public float 基础高度 = 64.0f;
    
    [Export] public float 细节缩放 = 0.05f;
    [Export] public float 细节幅度 = 8.0f;

    [Export] public int 世界高度上限 = 2048;
    [Export] public int 海平面高度 = 65;

    //预计算噪声
    private Dictionary<Vector2I, float[,]> _预计算的地形块 = new Dictionary<Vector2I, float[,]>();
    private float[,] _预计算的地形 = null;
    
    // 地形块相关计算
    private int _地形块包含区块数;
    private int _地形块大小;
    private Vector2I _上一次中心地形块坐标 = Vector2I.Zero;

    public override void _Ready()
	{
		_voxelWorld = GetParent<VoxelWorld>();
		_voxelWorld.ChunkManager.新地形被添加 += 添加地形;
		初始化地形块计算();
	}
    public void InitNoise()
    {
        // 主地形噪声
        _地形噪声.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
        _地形噪声.Seed = GD.RandRange(0, 99999);
        _地形噪声.Frequency = 地形缩放;
        _地形噪声.FractalOctaves = 6;
        _地形噪声.FractalGain = 0.5f;
        _地形噪声.FractalLacunarity = 2.0f;
        _地形噪声.FractalType = FastNoiseLite.FractalTypeEnum.Fbm;
        
        // 细节噪声
        _细节噪声.NoiseType = FastNoiseLite.NoiseTypeEnum.SimplexSmooth;
        _细节噪声.Seed = GD.RandRange(0, 99999);
        _细节噪声.Frequency = 细节缩放;
        _细节噪声.FractalOctaves = 2;
    }
    
    private void 初始化地形块计算()
    {
        // 根据注释"每个地形块包含16x16个区块底层方块"计算地形块大小
        // 每个区块有32x32个底层方块，所以地形块大小 = 16 * 32 = 512
        _地形块包含区块数 = 16; // 每个地形块包含16x16个区块
        _地形块大小 = _地形块包含区块数 * VoxelConst.CHUNK_SIZE_X;
        
        GD.Print($"地形块计算初始化完成：每个地形块包含 {_地形块包含区块数}x{_地形块包含区块数} 个区块，地形块大小 {_地形块大小}x{_地形块大小}");
    }
    
    public void 预计算地形块(Vector2I 中心地形块坐标)
    {
        // 完全禁用预计算，只更新坐标
        if (中心地形块坐标 == _上一次中心地形块坐标)
        {
            return;
        }
        
        _上一次中心地形块坐标 = 中心地形块坐标;
        
        // 不打印日志，减少控制台输出
    }
    
    // 移除旧的异步预计算方法，使用新的分帧预计算方法
    
    // 移除预计算相关方法，直接使用实时计算
    
    public float 获取地形块噪声(float worldX, float worldZ)
    {
        // 优化实时计算：简化噪声合并
        return _地形噪声.GetNoise2D(worldX, worldZ) * 地形幅度 + 
               _细节噪声.GetNoise2D(worldX, worldZ) * 细节幅度;
    }
    
    private Vector2I 计算地形块坐标(Vector2I 地形坐标)
    {
        // 将地形坐标转换为地形块坐标
        return new Vector2I(
            Mathf.FloorToInt((float)地形坐标.X / _地形块包含区块数),
            Mathf.FloorToInt((float)地形坐标.Y / _地形块包含区块数)
        );
    }
    
    private Vector3I 计算区块坐标(Vector2I 地形坐标)
    {
        // 计算地形坐标在世界中的中心位置
        float 世界X = 地形坐标.X * VoxelConst.CHUNK_SIZE_X + VoxelConst.CHUNK_SIZE_X / 2f;
        float 世界Z = 地形坐标.Y * VoxelConst.CHUNK_SIZE_Z + VoxelConst.CHUNK_SIZE_Z / 2f;
        
        // 获取该位置的地形高度
        float 噪声值 = 获取地形块噪声(世界X, 世界Z);
        int 地形高度 = Mathf.RoundToInt(基础高度 + 噪声值);
        
        // 计算该高度对应的区块Y坐标
        int 区块Y = Mathf.FloorToInt((float)地形高度 / VoxelConst.CHUNK_SIZE_Y);
        
        return new Vector3I(地形坐标.X, 区块Y, 地形坐标.Y);
    }
    
    private async Task 添加地形(object arg1, Vector2I 地形坐标)
    {
        // 使用简化计算，避免复杂逻辑
        Vector3I 区块坐标 = 计算区块坐标(地形坐标);
        var chunk = new Chunk(区块坐标);
        
        // 直接生成地形，不进行预计算
        生成地形高度(chunk, 地形坐标);

        _voxelWorld.ChunkManager.添加地形到区块总数据映射(地形坐标, chunk.ChunkPosition);
		_voxelWorld.ChunkManager.AddChunk(chunk);
    }
    
    private void 生成地形高度(Chunk chunk, Vector2I 地形坐标)
    {
        // 计算区块在世界中的起始位置
        int 区块起始X = 地形坐标.X * VoxelConst.CHUNK_SIZE_X;
        int 区块起始Z = 地形坐标.Y * VoxelConst.CHUNK_SIZE_Z;
        
        // 为区块中的每个方块计算高度
        for (int x = 0; x < VoxelConst.CHUNK_SIZE_X; x++)
        {
            for (int z = 0; z < VoxelConst.CHUNK_SIZE_Z; z++)
            {
                // 计算世界坐标
                float worldX = 区块起始X + x;
                float worldZ = 区块起始Z + z;
                
                // 使用预计算的地形块获取噪声值
                float 噪声值 = 获取地形块噪声(worldX, worldZ);
                
                // 计算地形高度
                int 地形高度 = Mathf.RoundToInt(基础高度 + 噪声值);
                
                // 限制高度在世界高度上限内
                地形高度 = Mathf.Clamp(地形高度, 0, 世界高度上限);
                
                // 填充从底部到地形高度的方块
                for (int y = 0; y < VoxelConst.CHUNK_SIZE_Y; y++)
                {
                    int 世界Y = (int)chunk.ChunkPosition.Y * VoxelConst.CHUNK_SIZE_Y + y;
                    
                    if (世界Y <= 地形高度)
                    {
                        // 根据高度选择方块类型
                        ushort 方块ID = 获取方块类型(世界Y, 地形高度);
                        chunk.SetVoxel(x, y, z, 方块ID);
                    }
                    else
                    {
                        // 高于地形高度的位置设为空气
                        chunk.SetVoxel(x, y, z, 0);
                    }
                }
            }
        }
    }
    
    private ushort 获取方块类型(int 世界Y, int 地形高度)
    {
        // 简单的方块类型分配逻辑
        if (世界Y <= 海平面高度 - 5)
        {
            return 3; // 石头
        }
        else if (世界Y <= 海平面高度)
        {
            return 2; // 沙子
        }
        else if (世界Y == 地形高度)
        {
            return 1; // 草方块
        }
        else
        {
            return 4; // 泥土
        }
    }


    public void 预计算噪声()
    {
        _预计算的地形 = new float[VoxelConst.预计算大小, VoxelConst.预计算大小];
        
        Parallel.For(0, VoxelConst.预计算大小, x =>
        {
            for (int z = 0; z < VoxelConst.预计算大小; z++)
            {
                _预计算的地形[x, z] = _地形噪声.GetNoise2D(x, z);
            }
        });
    }

    public float 获取预计算噪声(float x, float z)
    {
        int ix = Mathf.RoundToInt(x) % VoxelConst.预计算大小;
        int iz = Mathf.RoundToInt(z) % VoxelConst.预计算大小;
        return _预计算的地形[ix, iz];
    }
    
    // 使用 SIMD 优化的批量获取
    public float[] 批量获取噪声(Vector2[] 位置)
    {
        float[] 结果 = new float[位置.Length];
        var noise = new FastNoiseLite();
        
        for (int i = 0; i < 位置.Length; i++)
        {
            结果[i] = noise.GetNoise2D(位置[i].X, 位置[i].Y);
        }
        
        return 结果;
    }

    public override void _Process(double delta)
	{
		
	}

}
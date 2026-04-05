using Godot;
using GodotVoxelGame.VoxleData;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public partial class TerrainManager : Node
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
    private HashSet<Vector2I> _预计算的地形块坐标 = new HashSet<Vector2I>();

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
        // 根据视距计算要预计算的地形块范围
        int 视距 = _voxelWorld.视距;
        
        // 计算地形块视距（将区块视距转换为地形块视距）
        int 地形块视距 = Mathf.CeilToInt((float)视距 / _地形块包含区块数) + 1;
        
        // 计算需要预计算的地形块坐标
        var 需要预计算的地形块 = new HashSet<Vector2I>();
        
        for (int dx = -地形块视距; dx <= 地形块视距; dx++)
        {
            for (int dz = -地形块视距; dz <= 地形块视距; dz++)
            {
                Vector2I 地形块坐标 = new Vector2I(中心地形块坐标.X + dx, 中心地形块坐标.Y + dz);
                需要预计算的地形块.Add(地形块坐标);
            }
        }
        
        // 计算需要添加的新地形块和需要移除的旧地形块
        var 要添加的地形块 = 需要预计算的地形块.Except(_预计算的地形块坐标);
        var 要移除的地形块 = _预计算的地形块坐标.Except(需要预计算的地形块);
        
        // 预计算新地形块
        foreach (var 地形块坐标 in 要添加的地形块)
        {
            预计算单个地形块(地形块坐标);
        }
        
        // 移除不需要的地形块
        foreach (var 地形块坐标 in 要移除的地形块)
        {
            _预计算的地形块.Remove(地形块坐标);
        }
        
        // 更新预计算的地形块坐标集合
        _预计算的地形块坐标 = 需要预计算的地形块;
        
        GD.Print($"预计算地形块完成：中心坐标 {中心地形块坐标}, 视距 {地形块视距}, 预计算 {_预计算的地形块坐标.Count} 个地形块");
    }
    
    private void 预计算单个地形块(Vector2I 地形块坐标)
    {
        // 计算地形块在世界中的起始位置
        int 起始X = 地形块坐标.X * _地形块大小;
        int 起始Z = 地形块坐标.Y * _地形块大小;
        
        // 创建地形块噪声数据
        float[,] 地形块噪声 = new float[_地形块大小, _地形块大小];
        
        // 并行预计算地形块噪声
        Parallel.For(0, _地形块大小, x =>
        {
            for (int z = 0; z < _地形块大小; z++)
            {
                float worldX = 起始X + x;
                float worldZ = 起始Z + z;
                
                // 获取主地形噪声
                float 主噪声 = _地形噪声.GetNoise2D(worldX, worldZ);
                
                // 获取细节噪声
                float 细节噪声 = _细节噪声.GetNoise2D(worldX, worldZ);
                
                // 合并噪声
                地形块噪声[x, z] = 主噪声 * 地形幅度 + 细节噪声 * 细节幅度;
            }
        });
        
        // 存储预计算的地形块
        _预计算的地形块[地形块坐标] = 地形块噪声;
    }
    
    public float 获取地形块噪声(float worldX, float worldZ)
    {
        // 计算地形块坐标
        Vector2I 地形块坐标 = new Vector2I(
            Mathf.FloorToInt(worldX / _地形块大小),
            Mathf.FloorToInt(worldZ / _地形块大小)
        );
        
        // 检查是否已预计算
        if (!_预计算的地形块.ContainsKey(地形块坐标))
        {
            // 如果未预计算，实时计算
            float 主噪声 = _地形噪声.GetNoise2D(worldX, worldZ);
            float 细节噪声 = _细节噪声.GetNoise2D(worldX, worldZ);
            return 主噪声 * 地形幅度 + 细节噪声 * 细节幅度;
        }
        
        // 从预计算的地形块中获取噪声
        float[,] 地形块噪声 = _预计算的地形块[地形块坐标];
        
        // 计算在地形块内的相对位置
        int localX = Mathf.PosMod(Mathf.RoundToInt(worldX), _地形块大小);
        int localZ = Mathf.PosMod(Mathf.RoundToInt(worldZ), _地形块大小);
        
        return 地形块噪声[localX, localZ];
    }
    private async Task 添加地形(object arg1, Vector2I 地形坐标)
    {
        // 更新预计算地形块
        Vector2I 中心地形块坐标 = new Vector2I(
            Mathf.FloorToInt(地形坐标.X / (float)_地形块包含区块数),
            Mathf.FloorToInt(地形坐标.Y / (float)_地形块包含区块数)
        );
        预计算地形块(中心地形块坐标);
        
        // 创建区块
        Chunk chunk = new Chunk();
        chunk.ChunkPosition = new Vector3I(地形坐标.X, 0, 地形坐标.Y);
        
        // 使用预计算的地形块生成地形
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



	void 填充区块底层方块(Chunk chunk)
    {
        // 只填充 Y=0 层（最底层）
        int groundY = 0;

        for (int x = 0; x < VoxelConst.CHUNK_SIZE_X; x++)
        {
            for (int z = 0; z < VoxelConst.CHUNK_SIZE_Z; z++)
            {
                chunk.SetVoxel(x, groundY, z, 1);
            }
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
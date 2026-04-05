using Godot;
using GodotVoxelGame.VoxleData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading.Tasks;

[Tool]public partial class TerrainManager : Node
{
	private VoxelWorld _voxelWorld;
    [ExportGroup("噪声设置")]
    [Export]private FastNoiseLite _地形噪声 = new FastNoiseLite();
    [Export]private FastNoiseLite _细节噪声 = new FastNoiseLite();

    [ExportGroup("地形设置")]
    [Export] public float 地形缩放 = 0.01f;
    [Export] public float 地形幅度 = 32.0f;
    [Export] public float 基础高度 = 64.0f;
    
    [Export] public float 细节缩放 = 0.05f;
    [Export] public float 细节幅度 = 8.0f;

    [Export] public int 世界高度上限 = 2048;
    [Export] public int 海平面高度 = 65;

    [ExportGroup("地形块设置")]
    [Export]public int 地形块视距 = 1; // 以地形块为单位的视距，如果为1，则玩家周围3x3的地形块会被生成
    [Export]public Vector2I 地形块大小 = new(16, 16); //每个地形块包含多少个区块， (16, 16)表示每个地形块包含16x16个区块

    private Dictionary<Vector2I, float[,]> _预计算的地形块 = [];
    private HashSet<Vector2I> 需要生成的地形块 = [];

    /*****************        事件         **********************/
	public event Func<Vector3I, Task> 地形数据被生成;

    /*****************        事件         **********************/
    

    public override void _Ready()
	{
		_voxelWorld = GetParent<VoxelWorld>();
        _voxelWorld.玩家移动到新区块 += 判断是否需要生成地形数据;
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

    public async Task 判断是否需要生成地形数据(Vector3I PlayerChunkPos)
    {
        // 如果预计算的地形块为空，需要生成初始地形数据
        if(_预计算的地形块.Count == 0)
        {
            GD.Print("预计算地形块为空，开始生成初始地形数据");
            await 生成初始地形数据(PlayerChunkPos);
            return;
        }
        
        // 检查当前已有的地形块是否足够覆盖玩家周围的视距范围
        if (根据视距计算已有的地形块是否足够(_voxelWorld.视距, PlayerChunkPos))
        {
            GD.Print("当前地形块数据足够，无需重新生成");
            return;
        }
        
        // 需要生成新的地形数据
        GD.Print("需要生成新的地形数据");
        await 生成视距内地形数据(PlayerChunkPos);
    }

    private bool 根据视距计算已有的地形块是否足够(int 视距, Vector3I 玩家区块位置)
    {
        // 计算玩家周围视距范围内需要的地形块数量
        // 每个地形块包含16x16个区块，所以视距为1时，需要覆盖3x3个地形块
        int 地形块视距 = Mathf.CeilToInt((float)视距 / _地形块包含区块数);
        
        // 计算玩家所在的地形块坐标
        Vector2I 玩家地形块坐标 = 计算地形块坐标(new Vector2I(玩家区块位置.X, 玩家区块位置.Z));
        
        // 检查所有需要的地形块是否都已预计算
        for (int dx = -地形块视距; dx <= 地形块视距; dx++)
        {
            for (int dz = -地形块视距; dz <= 地形块视距; dz++)
            {
                Vector2I 需要的地形块坐标 = new Vector2I(玩家地形块坐标.X + dx, 玩家地形块坐标.Y + dz);
                
                if (!_预计算的地形块.ContainsKey(需要的地形块坐标))
                {
                    GD.Print($"缺少地形块数据: {需要的地形块坐标}");
                    return false;
                }
            }
        }
        
        GD.Print($"地形块数据足够: 视距{视距}, 地形块视距{地形块视距}, 需要{(2*地形块视距+1)*(2*地形块视距+1)}个地形块");
        return true;
    }
    
    private async Task 生成初始地形数据(Vector3I 玩家区块位置)
    {
        // 生成玩家当前位置周围的地形块数据
        Vector2I 玩家地形块坐标 = 计算地形块坐标(new Vector2I(玩家区块位置.X, 玩家区块位置.Z));
        
        // 生成3x3的地形块区域
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dz = -1; dz <= 1; dz++)
            {
                Vector2I 地形块坐标 = new Vector2I(玩家地形块坐标.X + dx, 玩家地形块坐标.Y + dz);
                Vector3I 地形块位置 = new Vector3I(地形块坐标.X, 0, 地形块坐标.Y);
                
                await 生成地形块数据(地形块位置);
            }
        }
        
        GD.Print($"初始地形数据生成完成，共生成9个地形块");
    }
    
    private async Task 生成视距内地形数据(Vector3I 玩家区块位置)
    {
        // 计算需要的地形块范围
        int 地形块视距 = Mathf.CeilToInt((float)_voxelWorld.视距 / _地形块包含区块数);
        Vector2I 玩家地形块坐标 = 计算地形块坐标(new Vector2I(玩家区块位置.X, 玩家区块位置.Z));
        
        var 生成任务列表 = new List<Task>();
        
        // 生成视距内所有需要的地形块
        for (int dx = -地形块视距; dx <= 地形块视距; dx++)
        {
            for (int dz = -地形块视距; dz <= 地形块视距; dz++)
            {
                Vector2I 地形块坐标 = new Vector2I(玩家地形块坐标.X + dx, 玩家地形块坐标.Y + dz);
                
                // 如果这个地形块还没有生成，就生成它
                if (!_预计算的地形块.ContainsKey(地形块坐标))
                {
                    Vector3I 地形块位置 = new Vector3I(地形块坐标.X, 0, 地形块坐标.Y);
                    var 生成任务 = 生成地形块数据(地形块位置);
                    生成任务列表.Add(生成任务);
                }
            }
        }
        
        // 等待所有地形块生成完成
        if (生成任务列表.Count > 0)
        {
            await Task.WhenAll(生成任务列表);
            GD.Print($"视距内地形数据生成完成，新增{生成任务列表.Count}个地形块");
        }
        else
        {
            GD.Print("视距内地形数据已完整，无需新增");
        }
    }

    

    public async Task 生成地形块数据(Vector3I 地形块位置)
    {
        // 计算地形块数据
        // 地形块包含 16x16 个区块，每个区块有 32x32 个方块
        // 所以地形块总大小为 16*32 = 512 个方块
        int 地形块宽度 = 地形块大小.X * VoxelConst.CHUNK_SIZE_X;
        int 地形块高度 = 地形块大小.Y * VoxelConst.CHUNK_SIZE_Y;
        
        // 创建地形块高度图
        float[,] 地形块高度图 = new float[地形块宽度, 地形块高度];
        
        // 计算地形块在世界中的起始位置
        int 世界起始X = 地形块位置.X * 地形块宽度;
        int 世界起始Z = 地形块位置.Z * 地形块高度;
        
        // 使用并行处理加速地形生成
        var 并行任务 = new List<Task>();
        
        // 将地形块分成多个部分进行并行处理
        int 并行度 = Math.Min(System.Environment.ProcessorCount, 地形块宽度);
        int 每部分宽度 = 地形块宽度 / 并行度;
        
        for (int 部分索引 = 0; 部分索引 < 并行度; 部分索引++)
        {
            int 起始X = 部分索引 * 每部分宽度;
            int 结束X = (部分索引 == 并行度 - 1) ? 地形块宽度 : 起始X + 每部分宽度;
            
            var 任务 = Task.Run(() =>
            {
                // 处理当前部分的地形数据
                for (int x = 起始X; x < 结束X; x++)
                {
                    for (int z = 0; z < 地形块高度; z++)
                    {
                        // 计算当前点的世界坐标
                        float worldX = 世界起始X + x;
                        float worldZ = 世界起始Z + z;
                        
                        // 使用噪声生成地形高度
                        float 主噪声 = _地形噪声.GetNoise2D(worldX, worldZ) * 地形幅度;
                        float 细节噪声 = _细节噪声.GetNoise2D(worldX, worldZ) * 细节幅度;
                        float 总噪声 = 主噪声 + 细节噪声;
                        
                        // 计算最终高度（基础高度 + 噪声值）
                        float 高度 = 基础高度 + 总噪声;
                        
                        // 限制高度在世界高度上限内
                        高度 = Mathf.Clamp(高度, 0, 世界高度上限);
                        
                        // 存储到高度图（需要线程安全访问）
                        lock (地形块高度图)
                        {
                            地形块高度图[x, z] = 高度;
                        }
                    }
                }
            });
            
            并行任务.Add(任务);
        }
        
        // 等待所有并行任务完成
        await Task.WhenAll(并行任务);
        
        // 存储到预计算字典
        _预计算的地形块[new (地形块位置.X, 地形块位置.Z)] = 地形块高度图;
        
        GD.Print($"地形块数据生成完成: 位置({地形块位置.X}, {地形块位置.Z}), 大小({地形块宽度}x{地形块高度}), 并行度: {并行度}");
    }
    
    public async Task 地形块数据添加到区块管理器总数据和地形数据中(Vector3I 地形块位置)
    {
        
    }
    
}
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
    [Export]public Vector2I 地形块大小 = new(16, 16); //每个地形块包含多少个区块， (16, 16)表示每个地形块包含16x16个区块

    private Dictionary<Vector2I, float[,]> _预计算的地形块 = [];
    

    /*****************        事件         **********************/
	public event Func<Vector3I, Task> 地形数据被生成;

    /*****************        事件         **********************/
    

    public override void _Ready()
	{
		_voxelWorld = GetParent<VoxelWorld>();
        _voxelWorld.ChunkManager.新地形需求被添加 += 检查地形区块是否存在并生成;
        
	}

    

    private async Task 检查地形区块是否存在并生成(Vector2I 区块坐标)
    {
        var 地形块坐标 = 根据区块坐标计算地形块坐标(区块坐标);
        if (_预计算的地形块.ContainsKey(地形块坐标)) return;
            
        // 生成地形块数据
        await 生成地形块数据(地形块坐标);  

        获取区块数据并添加到区块管理器(地形块坐标, 区块坐标);

    }

    private Vector2I 根据区块坐标计算地形块坐标(Vector2I 区块坐标)
    {
        int 地形块X = Mathf.FloorToInt(区块坐标.X / 地形块大小.X);
        int 地形块Y = Mathf.FloorToInt(区块坐标.Y / 地形块大小.Y);
    
    return new Vector2I(地形块X, 地形块Y);
    }

    private void 获取区块数据并添加到区块管理器(Vector2I 地形块坐标,Vector2I 区块坐标)
    {
        _预计算的地形块.TryGetValue(地形块坐标, out var  地形块高度图);

         // 计算区块在地形块内的局部坐标
        int 区块在X方向索引 = 区块坐标.X - 地形块坐标.X * 地形块大小.X;
        int 区块在Z方向索引 = 区块坐标.Y - 地形块坐标.Y * 地形块大小.Y;

        // 获取这个区块对应的高度图区域
    float[,] 区块高度图 = 地形块.获取区块高度图区域(区块在X方向索引, 区块在Z方向索引);

        Chunk chunkData = new Chunk(new Vector3I(区块坐标.X, 0, 区块坐标.Y));


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
    

    public async Task 生成地形块数据(Vector2I 地形块位置)
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
        int 世界起始Y = 地形块位置.Y * 地形块高度;
        
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
                        float worldY = 世界起始Y + z;
                        
                        // 使用噪声生成地形高度
                        float 主噪声 = _地形噪声.GetNoise2D(worldX, worldY) * 地形幅度;
                        float 细节噪声 = _细节噪声.GetNoise2D(worldX, worldY) * 细节幅度;
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
        _预计算的地形块[new (地形块位置.X, 地形块位置.Y)] = 地形块高度图;
        
        GD.Print($"地形块数据生成完成: 位置({地形块位置.X}, {地形块位置.Y}), 大小({地形块宽度}x{地形块高度}), 并行度: {并行度}");
    }
    
}
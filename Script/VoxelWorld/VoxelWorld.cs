using Godot;
using System;
using System.Threading.Tasks;

[Tool]public partial class VoxelWorld : Node
{
	[ExportGroup("组件引用")]
    [Export]public Node3D PlayerPivot{get; set;}
    
    // 运行时获取的组件引用
    [Export]public ChunkManager ChunkManager{get; private set;}
    [Export]public TerrainManager TerrainManager{get; private set;}
    [Export]public MeshGenManager MeshGenManager{get; private set;}
    
    [ExportGroup("区块管理器")] 
    [Export]public Vector3I 区块加载范围{get; set;} = new (1, 1, 1);
    [Export]public int 视距{get; set;} = 1;
    
    [ExportGroup("地形生成器")] 
    [Export]public FastNoiseLite.NoiseTypeEnum 噪声类型 { get; set; } = FastNoiseLite.NoiseTypeEnum.Simplex;
    [Export]public FastNoiseLite.FractalTypeEnum 地形类型 { get; set; } = FastNoiseLite.FractalTypeEnum.Fbm;
    [Export]public int 地形种子 { get; set; } = 123456789;
    [Export]public float 地形大小 { get; set; } = 0.015f;
    [Export]public int 细节层数 { get; set; } = 4;
    [Export]public float 细节间隔 { get; set; } = 2.0f;
    [Export]public float 细节强度 { get; set; } = 0.5f;
    [Export]public bool 是否生成调试地形 { get; set; } = false;
    
    [ExportGroup("网格渲染器")] 
    [Export] public bool 启用遮挡剔除 { get; set; } = true;
    [Export] public bool 重新生成网格 {get; set;} = true;
    [Export] public bool 清除现有网格 {get; set;} = false;
    
    [ExportGroup("调试选项")] 
    [Export]public Vector3I 玩家所在区块{get; set;}
	[Export]public Vector3I 上一次玩家所在区块{get;set;}

    private bool 是否为初次加载 = true;
    
	/*****************        事件         **********************/
	public event Func<Vector3I, Task> 玩家移动到新区块;

    /*****************        事件         **********************/

    public override void _Ready()
    {
        // 在运行时获取组件引用
        ChunkManager = GetNode<ChunkManager>("ChunkManager");
        TerrainManager = GetNode<TerrainManager>("TerrainManager");
        MeshGenManager = GetNode<MeshGenManager>("MeshGenManager");
       
    }

    public override void _Process(double delta)
    {
        更新玩家所在区块();
    }

    private void 更新玩家所在区块()
    {
		if(PlayerPivot != null)
		{
			玩家所在区块 = new Vector3I(
			Mathf.FloorToInt(PlayerPivot.GlobalPosition.X / VoxelConst.CHUNK_SIZE_X),
			Mathf.FloorToInt(PlayerPivot.GlobalPosition.Y / VoxelConst.CHUNK_SIZE_Y),
			Mathf.FloorToInt(PlayerPivot.GlobalPosition.Z / VoxelConst.CHUNK_SIZE_Z)
			);
			if(上一次玩家所在区块 != 玩家所在区块)
			{
				玩家移动到新区块?.Invoke(玩家所在区块);
			}
            if(是否为初次加载)
            {
                玩家移动到新区块?.Invoke(玩家所在区块);
                是否为初次加载 = false;
            }
			上一次玩家所在区块 = 玩家所在区块;

		}
        else
		{
			玩家所在区块 = new(0,0,0);
			上一次玩家所在区块 = 玩家所在区块;
			玩家移动到新区块?.Invoke(玩家所在区块);
		}
    }
}
using Godot;
using System;
using System.Threading.Tasks;

public partial class VoxelWorld : Node
{
    public ChunkManager ChunkManager{get; private set;}
    public TerrainManager TerrainManager{get; private set;}
    public MeshManager MeshManager{get; private set;}
    public ColliderManager ColliderManager{get; private set;}

    [Export]public int 视距{get; set;} = 3;
    public int 旧视距{get; set;} = 0;

    [Export]public int 需要实例化的视距{get; set;} = 2;
    public int 旧需要实例化的视距{get; set;} = 0;
    
    [Export]public int 每帧加载地形数{get; set;} = 1;
    public Player 玩家引用{get; set;}
    public Vector3I 玩家所在区块{get; set;}
	public Vector3I 上一次玩家所在区块{get;set;}
    
    
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
    [Export] public BlockAtlasConfig AtlasConfig { get; set; } = new();
    [Export] public bool 启用面剔除 { get; set; } = true;
    [Export] public bool 重新生成网格 {get; set;} = true;
    [Export] public bool 清除现有网格 {get; set;} = false;

    public override void _Ready()
    {
        // 在运行时获取组件引用
        ChunkManager = GetNode<ChunkManager>("ChunkManager");
        TerrainManager = GetNode<TerrainManager>("TerrainManager");

        初始化管理器();
       
    }

    public override void _Process(double delta)
    {
        更新玩家所在区块();
        视距是否变化();
    }

    private void 初始化管理器()
    {
        // 区块管理器
        var ChunkPackedScene = GD.Load<PackedScene>("uid://m06qjfw6w7jh");
        if(ChunkPackedScene != null)
		{
			ChunkManager = ChunkPackedScene.Instantiate<ChunkManager>();
            
            ChunkManager._voxelWorld = this;
	
			AddChild(ChunkManager);
		}

        // 地形管理器
        var TerrainPackedScene = GD.Load<PackedScene>("uid://drvxjs0iw4jpf");
        if(TerrainPackedScene != null)
		{
			TerrainManager = TerrainPackedScene.Instantiate<TerrainManager>();

            TerrainManager.voxelWorld = this;
	
			AddChild(TerrainManager);
		}

        // 网格管理器   
        var MeshPackedScene = GD.Load<PackedScene>("uid://dqsbs2iwaq5lv");
        if(MeshPackedScene != null)
		{
			MeshManager = MeshPackedScene.Instantiate<MeshManager>();

            MeshManager.voxelWorld = this;
	
			AddChild(MeshManager);
		}

        // 碰撞体管理器
        var ColliderPackedScene = GD.Load<PackedScene>("uid://colliderpackedscene");
        if(ColliderPackedScene != null)
        {
            ColliderManager = ColliderPackedScene.Instantiate<ColliderManager>();

            ColliderManager.voxelWorld = this;

            AddChild(ColliderManager);
        }
    }

    private void 更新玩家所在区块()
    {

		玩家所在区块 = new Vector3I(
		Mathf.FloorToInt(玩家引用.GlobalPosition.X / VoxelConst.CHUNK_SIZE_X),
		Mathf.FloorToInt(玩家引用.GlobalPosition.Y / VoxelConst.CHUNK_SIZE_Y),
		Mathf.FloorToInt(玩家引用.GlobalPosition.Z / VoxelConst.CHUNK_SIZE_Z)
		);
		if(上一次玩家所在区块 != 玩家所在区块)
		{
			ChunkManager.是否需要重新计算需要加载的地形 = true;
		}
            
		上一次玩家所在区块 = 玩家所在区块;

    }

    private void 视距是否变化()
    {
        if(视距 != 旧视距)
        {
            ChunkManager.是否需要重新计算需要加载的地形 = true;
            旧视距 = 视距;
        }
    }
}
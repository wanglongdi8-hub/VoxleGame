using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

public partial class ChunkManager : Node
{
	private HashSet<Vector2I> 视距内地形 = [];
	private HashSet<Vector2I> 新视距内地形 = [];
	HashSet<Vector2I> 要添加;
	HashSet<Vector2I> 要移除;
	private Vector2I 当前中心区块 = Vector2I.Zero;
	private int 当前视距 = 1;
	private bool 是否更新视距内地形 = false;
	private Dictionary<Vector3I, Chunk> _chunkDict = [];
	private VoxelWorld _voxelWorld;

	public override void _Ready()
	{
		_voxelWorld = GetParent<VoxelWorld>();
		_voxelWorld.玩家移动到新区块 += 响应更新视距内区块;
	}

	public override async void _Process(double delta)
	{
        if(是否更新视距内地形)
		{
			var sw = Stopwatch.StartNew();

            await Task.Run(() => Parallel.For(0, 1000, var i =更新视距内区块()));

			
			sw.Stop();
			GD.Print(sw.ElapsedMilliseconds);
		}
    }

	private  void 响应更新视距内区块(Vector3I inPos)
	{
		是否更新视距内地形 = true;
		当前中心区块 = new Vector2I(inPos.X,inPos.Z);
	}

	private async Task 更新视距内区块()
	{

		var 新视距内区块 = 计算正方形视距内区块(当前中心区块,_voxelWorld.视距);

		var 要添加的区块 = 新视距内区块.Except(视距内地形).ToHashSet();
        var 要移除的区块 = 视距内地形.Except(新视距内区块).ToHashSet();
        
        // 更新内部状态
        当前视距 = _voxelWorld.视距;
        视距内地形 = 新视距内区块;
		是否更新视距内地形 = true;
    
	}

	private HashSet<Vector2I> 计算正方形视距内区块(Vector2I 中心, int 视距)
    {
        var 结果 = new HashSet<Vector2I>();
        
        for (int dx = -视距; dx <= 视距; dx++)
        {
            for (int dz = -视距; dz <= 视距; dz++)
            {
                结果.Add(new Vector2I(中心.X + dx, 中心.Y + dz));
            }
        }
        
        return 结果;
    }
	private void 打印状态()
    {
        GD.Print($"当前中心区块: {_voxelWorld.玩家所在区块}");
        GD.Print($"当前视距: {当前视距}");
        GD.Print($"已生成区块数: {视距内地形.Count}");
        GD.Print($"当前视距内区块数: {新视距内地形.Count}");
    }
}

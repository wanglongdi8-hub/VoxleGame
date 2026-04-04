using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

public partial class ChunkManager : Node
{
	public HashSet<Vector2I> 视距内地形 = [];
	private HashSet<Vector2I> 新视距内地形 = [];
	private Vector2I 当前中心区块 = Vector2I.Zero;
	private int 当前视距 = 1;
	private Dictionary<Vector3I, Chunk> _chunkDict = [];
	private VoxelWorld _voxelWorld;

	/*****************        事件         **********************/
	public event Func<object, Vector2I, Task> 新地形被添加;
	public event Func<object, Vector2I, Task> 地形被删除;

	/*****************        事件         **********************/

	public override void _Ready()
	{
		_voxelWorld = GetParent<VoxelWorld>();
		_voxelWorld.玩家移动到新区块 += 响应更新视距内区块;
	}


    public override  void _Process(double delta)
	{
        
    }
	private async Task 响应更新视距内区块(object arg1, Vector3I inPos)
    {
        
		var sw = Stopwatch.StartNew();

		await 更新视距内区块();

		sw.Stop();
		GD.Print(sw.ElapsedMilliseconds);
    }


	private async Task 更新视距内区块()
	{

        var 新视距内区块 = 计算正方形视距内区块(当前中心区块, _voxelWorld.视距);

        var 要添加的区块 = 新视距内区块.Except(视距内地形).ToHashSet();
        var 要移除的区块 = 视距内地形.Except(新视距内区块).ToHashSet();

		// 触发移除事件
        foreach (var 区块 in 要移除的区块)
        {
            try
            {
                await 地形被删除?.Invoke(this, 区块);
            }
            catch (Exception ex)
            {
                GD.PrintErr($"移除区块时发生错误 {区块}: {ex.Message}");
            }
        }

        // 触发添加事件
        foreach (var 区块 in 要添加的区块)
        {
            try
            {
                await 新地形被添加?.Invoke(this, 区块);
            }
            catch (Exception ex)
            {
                GD.PrintErr($"添加区块时发生错误 {区块}: {ex.Message}");
            }
        }
        // 更新内部状态
        当前视距 = _voxelWorld.视距;
        视距内地形 = 新视距内区块;
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

	public void AddChunk(Chunk chunk)
    {
        _chunkDict.TryAdd(chunk.ChunkPosition, chunk);
        GD.Print($"Addchunk: {chunk.ChunkPosition}");
    }

	private void RemoveChunk(Vector3I chunkPos)
    {
        _chunkDict.Remove(chunkPos);
		GD.Print($"Removechunk: {chunkPos}");
    }

}

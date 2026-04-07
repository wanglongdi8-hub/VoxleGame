using Godot;
using GodotVoxelGame.VoxleData;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

[Tool]public partial class ChunkManager : Node
{
    
    private Vector2I 当前中心区块 = Vector2I.Zero;
    private int 当前视距 = 1;  
    
    private VoxelWorld _voxelWorld;
    /*****************        区块实例         ***********************/
    public Dictionary<Vector3I, ChunkInstance> ChunkInstanceDict {get; private set;} = [];
    public Dictionary<Vector3I,Vector3I> 地形到区块总数据映射 {get; private set;} = []; //区分总数据中那些是地形

    private Vector2I[]  上次加载的地形坐标 = [];

    public event Func<ChunkInstance, Task> 区块实例被添加;
    public event Func<Vector3I, Task> 区块实例被移除;

    public event Func<Vector2I, Task> 新地形需求被添加;
    public event Func<Vector2I, Task> 地形被删除;

    /*****************        区块实例          **********************/



    public override void _Ready()
    {
        _voxelWorld = GetParent<VoxelWorld>();
        _voxelWorld.玩家移动到新区块 += 更新区块内需要加载的地形;
    }

    private async Task 更新区块内需要加载的地形(Vector3I PlayerChunkPos)
    {
        await 计算需要加载的地形坐标(PlayerChunkPos);
        //DOTO: 检查地形总数据中是否存在这个地形区块，如果不存在则生成
    }

    private async Task 计算需要加载的地形坐标(Vector3I PlayerChunkPos)
    {
        // 根据玩家位置和视距计算当前需要加载的地形坐标 Vector2I
        
        // 预计算地形数量，避免动态扩容
        int 地形数量 = (2 * _voxelWorld.视距 + 1) * (2 * _voxelWorld.视距 + 1);
        var 当前需要加载的地形坐标 = new Vector2I[地形数量];
        int 索引 = 0;
        
        // 计算以玩家为中心，视距范围内的所有地形坐标
        for (int dx = -_voxelWorld.视距; dx <= _voxelWorld.视距; dx++)
        {
            for (int dz = -_voxelWorld.视距; dz <= _voxelWorld.视距; dz++)  
            {
                当前需要加载的地形坐标[索引++] = new Vector2I(PlayerChunkPos.X + dx, PlayerChunkPos.Z + dz);
            }
        }
        
        // 计算需要添加和移除的地形坐标
        var 需要添加的地形 = 当前需要加载的地形坐标.Except(上次加载的地形坐标).ToArray();
        var 需要移除的地形 = 上次加载的地形坐标.Except(当前需要加载的地形坐标).ToArray();
        
        // 触发添加事件
        foreach (var 地形坐标 in 需要添加的地形)
        {
            try
            {
                if (新地形需求被添加 != null)
                {
                    await 新地形需求被添加.Invoke(地形坐标);
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"添加地形时发生错误 {地形坐标}: {ex.Message}");
            }
        }
        
        // 触发移除事件
        foreach (var 地形坐标 in 需要移除的地形)
        {
            try
            {
                if (地形被删除 != null)
                {
                    await 地形被删除.Invoke(地形坐标);
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"移除地形时发生错误 {地形坐标}: {ex.Message}");
            }
        }
        
        // 更新上次加载的地形坐标
        上次加载的地形坐标 = 当前需要加载的地形坐标;
    }


    public void 添加区块实例(ChunkInstance chunkInstance)
    {
        AddChild(chunkInstance);
        ChunkInstanceDict[chunkInstance.chunkData.ChunkPosition] = chunkInstance;
        区块实例被添加?.Invoke(chunkInstance);
    }

    public void 添加地形到区块总数据映射(Vector3I 地形坐标, Vector3I 区块坐标)
    {
        地形到区块总数据映射.TryAdd(地形坐标, 区块坐标);
    }

    public void 从区块总数据映射移除地形(Vector3I 地形坐标)
    {
        地形到区块总数据映射.Remove(地形坐标);
    }  

    
}
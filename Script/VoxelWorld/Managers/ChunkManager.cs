using Godot;
using GodotVoxelGame.VoxleData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[Tool]public partial class ChunkManager : Node
{
    
    private Vector2I 当前中心区块 = Vector2I.Zero;
    private int 当前视距 = 1;  
    
    private VoxelWorld _voxelWorld;
    /*****************        区块实例         ***********************/
    private Dictionary<Vector3I, ChunkInstance> ChunkInstanceDict  = [];
    private HashSet<Vector3> 已实例化的区块 = new HashSet<Vector3>();
    private Dictionary<Vector2I,Vector3I[]> 一个地形柱中的区块索引 = [];
    private Vector2I[]  上次加载的地形坐标 = [];
    // 队列
    private Queue<Vector2I> 待生成地形区块 = [];
    private Queue<Vector3I> 已生成待实例化区块 = [];
    private Queue<Vector2I> 待删除地形区块 = [];

    /*****************        区块实例          **********************/
    public override void _Ready()
    {
        _voxelWorld = GetParent<VoxelWorld>();
        _voxelWorld.玩家移动到新区块 += 更新区块内需要加载的地形;
        _voxelWorld.视距变化 += 清空队列;
    }

    


    public override void _Process(double delta)
    {
        生成地形并保存();
        
        实例化区块();

        //取消渲染不在视距内的区块();

    }

    private async Task 清空队列(Vector3I i)
    {
        待生成地形区块.Clear();
        已生成待实例化区块.Clear();
        待删除地形区块.Clear();
    }

    private void 生成地形并保存()
    {
        if(待生成地形区块.Count > 0)
        {
            var result = 待生成地形区块.Dequeue();
            var chunks = _voxelWorld.TerrainManager.生成地形数据(result);

            Vector3I[] chunkArr = new Vector3I[chunks.Count];
            int 索引 = 0;

            foreach(var (chunkPosition, chunkData) in chunks)
            {
                ChunkInstanceDict[chunkPosition] = new ChunkInstance(chunkData);
                已生成待实例化区块.Enqueue(chunkPosition);
                chunkArr[索引] = chunkPosition;
                索引++;
            }
            if(!一个地形柱中的区块索引.ContainsKey(result))
            {
                一个地形柱中的区块索引.Add(result, chunkArr);
            }
        }
    }

    private void 实例化区块()
    {
        if(已生成待实例化区块.Count > 0 )
        {
            var chunkPos = 已生成待实例化区块.Dequeue();
            if(已实例化的区块.Contains(chunkPos)) return;
            AddChild(ChunkInstanceDict[chunkPos]);
            已实例化的区块.Add(chunkPos);
            ChunkInstanceDict[chunkPos].渲染网格();
        }   
    }

    private void 取消渲染不在视距内的区块()
    {
        if(待删除地形区块.Count > 0)
        {
            var 地形坐标 = 待删除地形区块.Dequeue();

            if(一个地形柱中的区块索引.TryGetValue(地形坐标, out Vector3I[] chunks))
            {
                foreach(var 区块坐标 in chunks)
                {
                    if (ChunkInstanceDict.TryGetValue(区块坐标, out ChunkInstance chunkInstance))
                    {
                        try
                        {
                            // 添加更多安全检查
                            if (chunkInstance != null && GodotObject.IsInstanceValid(chunkInstance))
                            {
                                // 让 ChunkInstance 自己处理清空逻辑
                                chunkInstance.清空网格();
                            }
                            
                            // 从父节点移除
                            if (chunkInstance != null && chunkInstance.GetParent() != null)
                            {
                                chunkInstance.GetParent().RemoveChild(chunkInstance);
                            }
                            
                            // 如果 ChunkInstance 是 Godot 节点，也需要释放
                            if (chunkInstance != null && chunkInstance is GodotObject godotObj)
                            {
                                if (GodotObject.IsInstanceValid(godotObj) && !godotObj.IsQueuedForDeletion())
                                {
                                    chunkInstance.QueueFree();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            GD.PrintErr($"取消渲染区块时出错: {ex.Message}, 区块位置: {区块坐标}");
                        }
                    }
                    
                    // 从字典中移除引用
                    ChunkInstanceDict.Remove(区块坐标);
                    
                    // 从已实例化集合中移除
                    已实例化的区块.Remove(区块坐标);
                }
                
                // 从区块索引中移除
                一个地形柱中的区块索引.Remove(地形坐标);
            }
        }
    }

    private async Task 更新区块内需要加载的地形(Vector3I PlayerChunkPos)
    {
        await 计算需要加载的地形坐标(PlayerChunkPos);
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
        
        // 添加到待生成地形区块
        foreach (var 地形坐标 in 需要添加的地形)
        {   
            if(!一个地形柱中的区块索引.ContainsKey(地形坐标))
            {
                待生成地形区块.Enqueue(地形坐标);
            }
        }
        
        // 添加到
        foreach (var 地形坐标 in 需要移除的地形)
        {
            if(一个地形柱中的区块索引.ContainsKey(地形坐标))
            {
                待删除地形区块.Enqueue(地形坐标);
            }
        }
        
        // 更新上次加载的地形坐标
        上次加载的地形坐标 = 当前需要加载的地形坐标;
    }


    public void 添加区块实例(ChunkInstance chunkInstance)
    {
        AddChild(chunkInstance);
        ChunkInstanceDict[chunkInstance.chunkData.ChunkPosition] = chunkInstance;
    }


    
}
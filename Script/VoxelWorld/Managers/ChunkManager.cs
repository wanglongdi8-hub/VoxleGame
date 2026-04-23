using Godot;
using GodotVoxelGame.VoxleData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public partial class ChunkManager : Node
{
    public bool 是否需要重新计算需要加载的地形 {get; set;} = false;
    private Vector2I 上一次玩家所在地形块 = Vector2I.Zero;
    public VoxelWorld _voxelWorld {get; set;}

    private Dictionary<Vector3I, ChunkInstance> ChunkInstanceDict = [];
    private Dictionary<Vector3I, Chunk> ChunkDict  = [];

    // 渲染
    private HashSet<Vector3> 已实渲染的区块 = [];
    private HashSet<Vector2I> 已实渲染的地形柱 = [];
    private Dictionary<Vector2I,Vector3I[]> 一个地形柱中的区块索引 = [];
    private Vector2I[]  上次加载的地形坐标 = [];
    // 队列
    private Queue<Vector2I> 待生成地形区块 = [];
    private Queue<Vector3I> 已生成待渲染区块 = [];
    private Queue<Vector2I> 待删除地形区块 = [];

    // 实例化
    private HashSet<Vector3I> 已实例化的区块 = [];
    private Queue<Vector3I> 待实例化的区块 = [];
    private Queue<Vector3I> 待取消实例化的区块 = [];

    
    public override void _Ready()
    {
        _voxelWorld = GetParent<VoxelWorld>();
    }


    public override async void _Process(double delta)
    {
        if(是否需要重新计算需要加载的地形)
        {
            计算需要加载的地形坐标(_voxelWorld.玩家所在区块);
            await 计算需要实例化的区块();
            是否需要重新计算需要加载的地形 = false;
        }
        
        //TODO: 添加并行处理
        for(int i = 0; i < _voxelWorld.每帧加载地形数; i++)
        {
            await 生成地形并保存();
            await 渲染所有区块();
            await 实例化区块();
            await 取消实例化区块();
            await 取消渲染不在视距内的区块();
        }

    }


    private async Task 渲染所有区块()
    {
        if(已生成待渲染区块.Count > 0 )
        {
            var chunkPos = 已生成待渲染区块.Dequeue();
            if(已实渲染的区块.Contains(chunkPos)) return;
            已实渲染的区块.Add(chunkPos);
            已实渲染的地形柱.Add(new Vector2I(chunkPos.X, chunkPos.Z));
            _voxelWorld.MeshManager.渲染网格(ChunkDict[chunkPos]);
        } 
    }

    private async Task 计算需要实例化的区块()
    {
        int 需要实例化的区块数 = (2 * _voxelWorld.视距 + 1) * (2 * _voxelWorld.视距 + 1) * (2 * _voxelWorld.视距 + 1);
        var 当前需要实例化的区块 = new Vector3I[需要实例化的区块数];
        int 索引 = 0;
        
        // 计算以玩家为中心，视距范围内的所有区块坐标
        var 玩家区块坐标 = _voxelWorld.玩家所在区块;
        for(int x = -_voxelWorld.视距; x <= _voxelWorld.视距; x++)
        {
            for(int y = -_voxelWorld.视距; y <= _voxelWorld.视距; y++)
            {
                for(int z = -_voxelWorld.视距; z <= _voxelWorld.视距; z++)
                {
                    当前需要实例化的区块[索引] = new Vector3I(玩家区块坐标.X + x, 玩家区块坐标.Y + y, 玩家区块坐标.Z + z);
                    索引++;
                }
            }
        }

        // 计算需要实例化的区块（当前需要但尚未实例化）
        var 需要实例化的区块 = 当前需要实例化的区块.Except(已实例化的区块).ToArray();
        
        // 计算需要取消实例化的区块（已实例化但不再需要）
        var 需要取消实例化的区块 = 已实例化的区块.Except(当前需要实例化的区块).ToArray();

        // 将需要实例化的区块添加到队列
        foreach (var 区块坐标 in 需要实例化的区块)
        {
            // 只有当区块数据已存在时才加入实例化队列
            if (ChunkDict.ContainsKey(区块坐标) && !已实例化的区块.Contains(区块坐标))
            {
                待实例化的区块.Enqueue(区块坐标);
            }
        }
        // 将需要取消实例化的区块添加到待删除队列
        foreach (var 区块坐标 in 需要取消实例化的区块)
        {
            if (已实例化的区块.Contains(区块坐标))
            {
                待取消实例化的区块.Enqueue(区块坐标);
            }
        }
    }

    private async Task 实例化区块()
    {
        if(待实例化的区块.Count == 0) return;
        var 区块坐标 = 待实例化的区块.Dequeue();
        if(ChunkDict.TryGetValue(区块坐标, out Chunk chunk))
        {
            var chunkInstance = new ChunkInstance(chunk);
            AddChild(chunkInstance);
            
            // 先将实例添加到字典，但暂时不标记为已实例化
            // 等待一帧确保ChunkInstance的_Ready方法执行完成
            await ToSignal(chunkInstance, "ready");
            
            ChunkInstanceDict[区块坐标] = chunkInstance;
            已实例化的区块.Add(区块坐标);
            
            // 通过 ColliderManager 添加碰撞体
            _voxelWorld.ColliderManager?.添加区块碰撞体(chunk);
        }
    }

    private async Task 取消实例化区块()
    {
        if(待取消实例化的区块.Count == 0) return;
        var 区块坐标 = 待取消实例化的区块.Dequeue();
        if(ChunkInstanceDict.TryGetValue(区块坐标, out ChunkInstance chunkInstance))
        {
            chunkInstance.QueueFree();
            ChunkInstanceDict.Remove(区块坐标);
            已实例化的区块.Remove(区块坐标);
            
            // 通过 ColliderManager 移除碰撞体
            _voxelWorld.ColliderManager?.移除区块碰撞体(区块坐标);
        }
    }


    private async Task 生成地形并保存()
    {
        if(待生成地形区块.Count > 0)
        {
            var result = 待生成地形区块.Dequeue();
            var chunks = _voxelWorld.TerrainManager.生成地形数据(result);

            Vector3I[] chunkArr = new Vector3I[chunks.Count];
            int 索引 = 0;

            foreach(var (chunkPosition, chunkData) in chunks)
            {
                chunkData.计算数据();
                ChunkDict[chunkPosition] = chunkData;
                已生成待渲染区块.Enqueue(chunkPosition);
                chunkArr[索引] = chunkPosition;
                索引++;
            }
            if(!一个地形柱中的区块索引.ContainsKey(result))
            {
                一个地形柱中的区块索引.Add(result, chunkArr);
            }
        }
    }

    private async Task 取消渲染不在视距内的区块()
    {
        if(待删除地形区块.Count == 0) return;
        var 地形坐标 = 待删除地形区块.Dequeue();
        var 已渲染的区块队列 = 获取已渲染的区块(地形坐标);

        if(已渲染的区块队列.Count <= 0) return;
        foreach(var 区块坐标 in 已渲染的区块队列)
        {
            if(ChunkDict.TryGetValue(区块坐标, out Chunk chunk))
            {
                _voxelWorld.MeshManager.清除网格(chunk);
                // 从已实例化集合中移除
                已实渲染的区块.Remove(区块坐标);
                已实渲染的地形柱.Remove(new Vector2I(区块坐标.X, 区块坐标.Z));
                // 从区块索引中移除
                一个地形柱中的区块索引.Remove(地形坐标);
                
            }
        }
    }

    private Queue<Vector3I> 获取已渲染的区块(Vector2I 地形块坐标)
    {
        Queue<Vector3I> 已渲染的区块队列 = [];

        // 检查地形坐标是否存在于字典中
        if (!一个地形柱中的区块索引.TryGetValue(地形块坐标, out Vector3I[] 地形坐标))
        {
            return 已渲染的区块队列; // 如果不存在，返回空队列
        }

        foreach(var chunks in 地形坐标)
        {
            if(已实渲染的区块.Contains(chunks))
            {
                已渲染的区块队列.Enqueue(chunks);
            }
        }

        return 已渲染的区块队列;
    }

    private bool 控制重新计算要加载的地形(Vector3I PlayerChunkPos)
    {
        if(是否需要重新计算需要加载的地形)
        {
            var 当前玩家所在地形块 = new Vector2I(PlayerChunkPos.X, PlayerChunkPos.Z);
            if(当前玩家所在地形块 != 上一次玩家所在地形块)
            {
                上一次玩家所在地形块 = 当前玩家所在地形块;
                return true;
            }
            return false;
        }
        return false;
    }

    private void 计算需要加载的地形坐标(Vector3I PlayerChunkPos)
    {
        // 根据玩家位置和视距计算当前需要加载的地形坐标 Vector2I
        
        // 预计算地形数量，避免动态扩容
        int 地形数量 = (2 * _voxelWorld.视距 + 1) * (2 * _voxelWorld.视距 + 1);
        var 当前需要加载的地形坐标 = new Vector2I[地形数量];
        int 索引 = 0;
        
        Vector2I 玩家地形坐标 = new Vector2I(PlayerChunkPos.X, PlayerChunkPos.Z);
        
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

        // 应该和已实例化的区块进行对比
        var 地形柱 = 已实渲染的地形柱.ToArray();
        var 需要移除的地形 = 地形柱.Except(当前需要加载的地形坐标).ToArray();
        
        // 按距离排序，距离近的优先加载
        var 排序后的需要添加地形 = 需要添加的地形
            .Select(坐标 => new { 坐标, 距离 = 坐标.DistanceTo(玩家地形坐标) })
            .OrderBy(x => x.距离)
            .Select(x => x.坐标)
            .ToArray();
        
        // 添加到待生成地形区块（按距离排序顺序）
        foreach (var 地形坐标 in 排序后的需要添加地形)
        {   
            if(!一个地形柱中的区块索引.ContainsKey(地形坐标) && !待删除地形区块.Contains(地形坐标))
            {
                待生成地形区块.Enqueue(地形坐标);
            }
        }
        
        // 添加到需要移除的地形区块
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

}
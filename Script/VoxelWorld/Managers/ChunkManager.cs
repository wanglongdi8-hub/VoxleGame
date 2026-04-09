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
    private bool 是否需要重新计算需要加载的地形 = false;
    
    private VoxelWorld _voxelWorld;
    /*****************        区块实例         ***********************/
    private Dictionary<Vector3I, ChunkInstance> ChunkInstanceDict  = [];
    private HashSet<Vector3> 已实例化的区块 = [];
    private HashSet<Vector2I> 已实例化的地形柱 = [];
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
        _voxelWorld.视距变化 += 更新区块内需要加载的地形;
    }

    public override void _Process(double delta)
    {
        计算需要加载的地形坐标(_voxelWorld.玩家所在区块);

        //TODO: 添加并行处理
        for(int i = 0; i < _voxelWorld.每帧加载地形数; i++)
        {
            生成地形并保存();
            实例化区块();
            取消渲染不在视距内的区块();
        }

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
            已实例化的地形柱.Add(new Vector2I(chunkPos.X, chunkPos.Z));
            ChunkInstanceDict[chunkPos].渲染网格();
        }   
    }

    private void 取消渲染不在视距内的区块()
    {
        if(待删除地形区块.Count <= 0) return;
        var 地形坐标 = 待删除地形区块.Dequeue();
        var 已实例化的区块队列 = 获取已实例化的区块(地形坐标);

        if(已实例化的区块队列.Count <= 0) return;
        foreach(var 区块坐标 in 已实例化的区块队列)
        {
            if(ChunkInstanceDict.TryGetValue(区块坐标, out ChunkInstance chunkInstance))
            {
                chunkInstance.清空网格();
                // 从父节点移除
                //chunkInstance.GetParent().RemoveChild(chunkInstance);
                // 如果 ChunkInstance 是 Godot 节点，也需要释放
                chunkInstance.QueueFree();
                // 从已实例化集合中移除
                已实例化的区块.Remove(区块坐标);
                已实例化的地形柱.Remove(new Vector2I(区块坐标.X, 区块坐标.Z));
                // 从区块索引中移除
                一个地形柱中的区块索引.Remove(地形坐标);
                // 从总数据中移除
                ChunkInstanceDict.Remove(区块坐标);
            }
        }
    }

    private Queue<Vector3I> 获取已实例化的区块(Vector2I 地形块坐标)
    {
        Queue<Vector3I> 已实例化的区块队列 = [];

        // 检查地形坐标是否存在于字典中
        if (!一个地形柱中的区块索引.TryGetValue(地形块坐标, out Vector3I[] 地形坐标))
        {
            return 已实例化的区块队列; // 如果不存在，返回空队列
        }

        foreach(var chunks in 地形坐标)
        {
            if(已实例化的区块.Contains(chunks))
            {
                已实例化的区块队列.Enqueue(chunks);
            }
        }

        return 已实例化的区块队列;
    }

    private async Task 更新区块内需要加载的地形(Vector3I PlayerChunkPos)
    {
        //await 计算需要加载的地形坐标(PlayerChunkPos);
        是否需要重新计算需要加载的地形 = true;
    }

    private void 计算需要加载的地形坐标(Vector3I PlayerChunkPos)
    {
        if(!是否需要重新计算需要加载的地形) return;
        是否需要重新计算需要加载的地形 = false;

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

        // 应该和已实例化的区块进行对比
        var 地形柱 = 已实例化的地形柱.ToArray();
        var 需要移除的地形 = 地形柱.Except(当前需要加载的地形坐标).ToArray();
        
        // 添加到待生成地形区块
        foreach (var 地形坐标 in 需要添加的地形)
        {   
            if(!一个地形柱中的区块索引.ContainsKey(地形坐标))
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
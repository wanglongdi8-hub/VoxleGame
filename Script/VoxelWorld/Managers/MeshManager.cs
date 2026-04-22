using System;
using Godot;

public partial class MeshManager : Node
{
    public VoxelWorld voxelWorld {get; set;}


    public override void _Ready()
    {
        
    }

    public void 渲染网格(Vector3I chunkPos)
    {
        
    }

    private async Task 取消实例化不在视距内的区块()
    {
        
    }


    private async Task 渲染区块()
    {
        if(待渲染区块.Count <= 0) return;
        var chunkPos = 待渲染区块.Dequeue();
        if(已渲染的地形柱.Contains(new Vector2I(chunkPos.X, chunkPos.Z))) return;
        已渲染的地形柱.Add(new Vector2I(chunkPos.X, chunkPos.Z));

        MeshManager.渲染网格(chunkPos);
    }


    private async Task 清空队列(Vector3I i)
    {
        待生成地形柱.Clear();
        已生成待实例化区块.Clear();
        //待删除地形柱.Clear();
    }

    private async Task 生成地形并保存()
    {
        if(待生成地形柱.Count > 0)
        {
            var result = 待生成地形柱.Dequeue();
            var chunks = voxelWorld.TerrainManager.生成地形数据(result);

            Vector3I[] chunkArr = new Vector3I[chunks.Count];
            int 索引 = 0;

            foreach(var (chunkPosition, chunkData) in chunks)
            {
                ChunkInstanceDict[chunkPosition] = new ChunkInstance(chunkData);
                chunkData.计算顶点数据();
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

    private async Task 实例化区块()
    {
        if(已生成待实例化区块.Count > 0 )
        {
            var chunkPos = 已生成待实例化区块.Dequeue();
            if(已实例化的区块.Contains(chunkPos)) return;
            AddChild(ChunkInstanceDict[chunkPos]);
            已实例化的区块.Add(chunkPos);
            已实例化的地形柱.Add(new Vector2I(chunkPos.X, chunkPos.Z));
            //ChunkInstanceDict[chunkPos].渲染网格();
        }   
    }

    private async Task 取消渲染不在视距内的区块()
    {
        if(待删除地形柱.Count <= 0) return;
        var 地形坐标 = 待删除地形柱.Dequeue();
        var 已实例化的区块队列 = 获取已实例化的区块(地形坐标);

        if(已实例化的区块队列.Count <= 0) return;
        foreach(var 区块坐标 in 已实例化的区块队列)
        {
            if(ChunkInstanceDict.TryGetValue(区块坐标, out ChunkInstance chunkInstance))
            {

                chunkInstance.QueueFree();
                // 从已实例化集合中移除
                已实例化的区块.Remove(区块坐标);
                已实例化的地形柱.Remove(new Vector2I(区块坐标.X, 区块坐标.Z));
                // 从区块索引中移除
                一个地形柱中的区块索引.Remove(地形坐标);

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

    private async Task 控制重新计算要加载的地形(Vector3I PlayerChunkPos)
    {
        var 当前玩家所在地形块 = new Vector2I(PlayerChunkPos.X, PlayerChunkPos.Z);
        if(当前玩家所在地形块 != 上一次玩家所在地形块)
        {
            上一次玩家所在地形块 = 当前玩家所在地形块;
            是否需要重新计算需要加载的地形 = true;
            return;
        }
    }

    private async Task 视距变化计算地形加载(Vector3I i)
    {
        是否需要重新计算需要加载的地形 = true;
    }

    private void 计算需要渲染的地形坐标(Vector3I PlayerChunkPos)
    {
        int 渲染视距 = voxelWorld.视距; 

        // 预计算地形数量
        int 地形数量 = (2 * voxelWorld.视距 + 1) * (2 * voxelWorld.视距 + 1);
        var 当前需要加载的地形坐标 = new Vector2I[地形数量];
        int 索引 = 0;
        
        // 计算渲染范围内的所有区块坐标
        for (int dx = -渲染视距; dx <= 渲染视距; dx++)
        {
            for (int dz = -渲染视距; dz <= 渲染视距; dz++)
            {
                // 跳过实例化距离内的区块（这些区块已经实例化）
                if (Math.Abs(dx) <= voxelWorld.需要实例化的视距 && Math.Abs(dz) <= voxelWorld.需要实例化的视距)
                    continue;
                    
                当前需要加载的地形坐标[索引++] = new Vector2I(PlayerChunkPos.X + dx, PlayerChunkPos.Z + dz); 
            }
        }

        // 计算需要添加的地形坐标
        var 需要添加的地形 = 当前需要加载的地形坐标.Except(上次只渲染的地形坐标).ToArray();

        // 和已渲染的地形坐标进行对比，移除已渲染的地形坐标
        var 地形柱 = 已渲染的地形柱.ToArray();
        var 需要移除的地形 = 地形柱.Except(当前需要加载的地形坐标).ToArray();

        // 按距离排序需要添加的地形（从近到远）
        var 排序后的添加地形 = 需要添加的地形
            .Select(coord => new 
            { 
                坐标 = coord, 
                距离平方 = Mathf.Pow(coord.X - PlayerChunkPos.X, 2) + 
                        Mathf.Pow(coord.Y - PlayerChunkPos.Z, 2) 
            })
            .OrderBy(item => item.距离平方)
            .Select(item => item.坐标)
            .ToArray();

        // 添加到待生成地形区块（按距离从近到远）
        foreach (var 地形坐标 in 排序后的添加地形)
        {   
            if(!一个地形柱中的区块索引.ContainsKey(地形坐标) && !待删除地形柱.Contains(地形坐标))
            {
                待生成地形柱.Enqueue(地形坐标);
            }
        }

        // 按距离排序需要移除的地形（从远到近）
        var 排序后的移除地形 = 需要移除的地形
            .Select(coord => new 
            { 
                坐标 = coord, 
                距离平方 = Mathf.Pow(coord.X - PlayerChunkPos.X, 2) + 
                        Mathf.Pow(coord.Y - PlayerChunkPos.Z, 2) 
            })
            .OrderByDescending(item => item.距离平方)
            .Select(item => item.坐标)
            .ToArray();

        // 添加到需要取消渲染的地形区块（从远到近）
        foreach (var 地形坐标 in 排序后的移除地形)
        {
            if(一个地形柱中的区块索引.ContainsKey(地形坐标))
            {
                待取消渲染地形柱.Enqueue(地形坐标);
            }
        }

        // 更新上次只渲染的地形坐标
        上次只渲染的地形坐标 = 当前需要加载的地形坐标;
        
    }

    private void 计算需要实例化的地形坐标(Vector3I PlayerChunkPos)
    {
        
        // 预计算地形数量
        int 地形数量 = (2 * voxelWorld.需要实例化的视距 + 1) * (2 * voxelWorld.需要实例化的视距 + 1);
        var 当前需要加载的地形坐标 = new Vector2I[地形数量];
        int 索引 = 0;
        
        // 计算以玩家为中心，视距范围内的所有地形坐标
        for (int dx = -voxelWorld.需要实例化的视距; dx <= voxelWorld.需要实例化的视距; dx++)
        {
            for (int dz = -voxelWorld.需要实例化的视距; dz <= voxelWorld.需要实例化的视距; dz++)  
            {
                当前需要加载的地形坐标[索引++] = new Vector2I(PlayerChunkPos.X + dx, PlayerChunkPos.Z + dz);
            }
        }
        
        // 计算需要添加的地形坐标
        var 需要添加的地形 = 当前需要加载的地形坐标.Except(上次加载的地形坐标).ToArray();

        // 应该和已实例化的区块进行对比，移除已实例化的区块
        var 地形柱 = 已实例化的地形柱.ToArray();
        var 需要移除的地形 = 地形柱.Except(当前需要加载的地形坐标).ToArray();
        
        // 按距离排序需要添加的地形（从近到远）
        var 排序后的添加地形 = 需要添加的地形
            .Select(coord => new 
            { 
                坐标 = coord, 
                距离平方 = Mathf.Pow(coord.X - PlayerChunkPos.X, 2) + 
                        Mathf.Pow(coord.Y - PlayerChunkPos.Z, 2) 
            })
            .OrderBy(item => item.距离平方)
            .Select(item => item.坐标)
            .ToArray();
        
        // 添加到待生成地形区块（按距离从近到远）
        foreach (var 地形坐标 in 排序后的添加地形)
        {   
            if(!一个地形柱中的区块索引.ContainsKey(地形坐标) && !待删除地形柱.Contains(地形坐标))
            {
                待生成地形柱.Enqueue(地形坐标);
            }
        }
        
        // 按距离排序需要移除的地形（从远到近）
        var 排序后的移除地形 = 需要移除的地形
            .Select(coord => new 
            { 
                坐标 = coord, 
                距离平方 = Mathf.Pow(coord.X - PlayerChunkPos.X, 2) + 
                        Mathf.Pow(coord.Y - PlayerChunkPos.Z, 2) 
            })
            .OrderByDescending(item => item.距离平方)
            .Select(item => item.坐标)
            .ToArray();
        
        // 添加到需要移除的地形区块（从远到近）
        foreach (var 地形坐标 in 排序后的移除地形)
        {
            if(一个地形柱中的区块索引.ContainsKey(地形坐标))
            {
                待删除地形柱.Enqueue(地形坐标);
            }
        }
        
        // 更新上次加载的地形坐标
        上次加载的地形坐标 = 当前需要加载的地形坐标;

        计算需要渲染的地形坐标(PlayerChunkPos);
    }
}
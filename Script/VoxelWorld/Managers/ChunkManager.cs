using Godot;
using GodotVoxelGame.VoxleData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CGodotGameGodotVoxelGame.Script.VoxelWorld.Managers;

public partial class ChunkManager : Node
{
    [Export]public Material Material {get; set;}
    private Vector2I 当前中心区块 = Vector2I.Zero;
    private int 当前视距 = 1;
    private bool 是否需要重新计算需要加载的地形 = false;
    private Vector2I 上一次玩家所在地形块 = Vector2I.Zero;
    
    private VoxelWorld _voxelWorld;
    private MeshManager MeshManager = new();

    /*****************        区块实例         ***********************/
    private Dictionary<Vector3I, Chunk> ChunkDict  = [];
    private HashSet<Vector3> 已渲染的区块 = [];
    private HashSet<Vector2I> 已渲染的地形柱 = [];
    private Dictionary<Vector2I,Vector3I[]> 一个地形柱中的区块索引 = [];
    private Vector2I[]  上次加载的地形柱 = [];
    // 队列
    private Queue<Vector2I> 待生成地形柱 = [];
    private Queue<Vector3I> 已生成待渲染区块 = [];
    private Queue<Vector2I> 待取消渲染地形柱 = [];

    /*****************        区块实例          **********************/

    
    public override void _Ready()
    {
        _voxelWorld = GetParent<VoxelWorld>();
        _voxelWorld.玩家移动到新区块 += 控制重新计算要加载的地形;
        _voxelWorld.视距变化 += 视距变化计算地形加载;
        
        AddChild(MeshManager);
        MeshManager.Material = Material;
    }


    public override async void _Process(double delta)
    {
        if(是否需要重新计算需要加载的地形)
        {
            计算需要加载的地形坐标(_voxelWorld.玩家所在区块);
            是否需要重新计算需要加载的地形 = false;
        }
        
        for(int i = 0; i < _voxelWorld.每帧加载地形数; i++)
        {
            await 生成地形并保存();
            await 渲染区块();
            await 取消渲染不在视距内的区块();
        }

    }
    
    private async Task 生成地形并保存()
    {
        if(待生成地形柱.Count > 0)
        {
            var result = 待生成地形柱.Dequeue();
            var chunks = _voxelWorld.TerrainManager.生成地形数据(result);

            Vector3I[] chunkArr = new Vector3I[chunks.Count];
            int 索引 = 0;

            foreach(var (chunkPosition, chunkData) in chunks)
            {
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

    private async Task 渲染区块()
    {
        if(已生成待渲染区块.Count > 0 )
        {
            var chunkPos = 已生成待渲染区块.Dequeue();
            var chunk = ChunkDict[chunkPos];
            if(已渲染的区块.Contains(chunkPos)) return;
            已渲染的区块.Add(chunkPos);
            已渲染的地形柱.Add(new Vector2I(chunkPos.X, chunkPos.Z));
            
            chunk.计算数据();
            MeshManager.渲染网格(chunk);
        }   
    }

    private async Task 取消渲染不在视距内的区块()
    {
        if(待取消渲染地形柱.Count <= 0) return;
        var 地形坐标 = 待取消渲染地形柱.Dequeue();
        var 已实例化的区块队列 = 获取已渲染的区块(地形坐标);

        if(已实例化的区块队列.Count <= 0) return;
        foreach(var 区块坐标 in 已实例化的区块队列)
        {
            if(ChunkDict.TryGetValue(区块坐标, out var chunk))
            {
                MeshManager.清除网格(chunk);
                // 从已实例化集合中移除
                已渲染的区块.Remove(区块坐标);
                已渲染的地形柱.Remove(new Vector2I(区块坐标.X, 区块坐标.Z));
                // 从区块索引中移除
                一个地形柱中的区块索引.Remove(地形坐标);
            }
        }
    }

    private Queue<Vector3I> 获取已渲染的区块(Vector2I 地形块坐标)
    {
        Queue<Vector3I> 已渲染区块队列 = [];

        // 检查地形坐标是否存在于字典中
        if (!一个地形柱中的区块索引.TryGetValue(地形块坐标, out Vector3I[] 地形坐标))
        {
            return 已渲染区块队列; // 如果不存在，返回空队列
        }

        foreach(var chunks in 地形坐标)
        {
            if(已渲染的区块.Contains(chunks))
            {
                已渲染区块队列.Enqueue(chunks);
            }
        }

        return 已渲染区块队列;
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
        var 需要添加的地形 = 当前需要加载的地形坐标.Except(上次加载的地形柱).ToArray();

        // 应该和已实例化的区块进行对比
        var 地形柱 = 已渲染的地形柱.ToArray();
        var 需要移除的地形 = 地形柱.Except(当前需要加载的地形坐标).ToArray();
        
        // 添加到待生成地形区块
        foreach (var 地形坐标 in 需要添加的地形)
        {   
            if(!一个地形柱中的区块索引.ContainsKey(地形坐标) && !待取消渲染地形柱.Contains(地形坐标))
            {
                待生成地形柱.Enqueue(地形坐标);
            }
        }
        
        // 添加到需要移除的地形区块
        foreach (var 地形坐标 in 需要移除的地形)
        {
            if(一个地形柱中的区块索引.ContainsKey(地形坐标))
            {
                待取消渲染地形柱.Enqueue(地形坐标);
            }
        }
        
        // 更新上次加载的地形坐标
        上次加载的地形柱 = 当前需要加载的地形坐标;
    }

}
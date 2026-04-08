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
    private Dictionary<Vector2I,Vector3I[]> 一个地形柱中的区块索引;
    private Vector2I[]  上次加载的地形坐标 = [];
    // 队列
    private Queue<Vector2I> 待生成地形区块 = [];
    private Queue<Vector2I> 待删除地形区块 = [];

    private Queue<Vector3I> 已生成待实例化区块 = [];



    /*****************        区块实例          **********************/
    public override void _Ready()
    {
        _voxelWorld = GetParent<VoxelWorld>();
        _voxelWorld.玩家移动到新区块或视距变化 += 更新区块内需要加载的地形;
    }
    public override void _Process(double delta)
    {
        // 测试
        if(待生成地形区块.Count > 0)
        {
           var result = 待生成地形区块.Dequeue();
           var chunk = _voxelWorld.TerrainManager.生成地形数据(result);

           foreach(var T in chunk.Values)
            {
                _voxelWorld.MeshGenManager.添加区块(T);
            }
        }

        //生成地形

        // 生成区块实例
 
        // 渲染

        
    }

    private async Task 生成地形()
    {
        
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
        
        // 添加到待生成地形区块
        foreach (var 地形坐标 in 需要添加的地形)
        {
            待生成地形区块.Enqueue(地形坐标);
            GD.Print("添加到待生成地形区块",地形坐标);
        }
        
        // 添加到
        foreach (var 地形坐标 in 需要移除的地形)
        {
            待删除地形区块.Enqueue(地形坐标);
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
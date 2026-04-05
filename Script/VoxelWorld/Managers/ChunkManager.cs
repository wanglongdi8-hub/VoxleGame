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
    public Dictionary<Vector2I,Vector3I> 地形到区块总数据映射 {get; private set;} = [];

    public event Func<ChunkInstance, Task> 区块实例被添加;
    public event Func<Vector3I, Task> 区块实例被移除;
    public event Func<Vector2I, Task> 新地形被添加;
    public event Func<Vector2I, Task> 地形被删除;

    /*****************        区块实例          **********************/



    public override void _Ready()
    {
        _voxelWorld = GetParent<VoxelWorld>();
    }

    public void 添加区块实例(ChunkInstance chunkInstance)
    {
        AddChild(chunkInstance);
        ChunkInstanceDict[chunkInstance.chunkData.ChunkPosition] = chunkInstance;
        区块实例被添加?.Invoke(chunkInstance);
    }

    public void 添加地形到区块总数据映射(Vector2I 地形坐标, Vector3I 区块坐标)
    {
        地形到区块总数据映射.TryAdd(地形坐标, 区块坐标);
    }

    public void 从区块总数据映射移除地形(Vector2I 地形坐标)
    {
        地形到区块总数据映射.Remove(地形坐标);
    }  

    
}
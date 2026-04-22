using Godot;
using GodotVoxelGame.VoxelOctree;
using GodotVoxelGame.VoxleData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public partial class ChunkManager : Node
{
    public VoxelWorld voxelWorld {get; set;}
    public MeshManager MeshManager{get; private set;}
    private bool 是否需要重新计算需要加载的地形 = false;
    
    // 区块实例存储
    private Dictionary<Vector3I, ChunkInstance> ChunkInstanceDict  = [];
    // 区块数据存储
    private Dictionary<Vector3I, Chunk> ChunkDataDict = [];
    // 多个八叉树，每个八叉树负责32x32x32个区块的地形
    private Dictionary<Vector3I, ChunkOctree> 八叉树 = [];

    private HashSet<Vector2I> 上次需要生成的地形柱 = [];
    private HashSet<Vector2I> 已生成的地形柱 = [];
    private HashSet<Vector2I> 已渲染的地形柱 = [];


    private Queue<Vector2I> 要生成的地形柱 = [];
    private Queue<Vector2I> 要移除的地形柱 = [];


    public override void _Ready()
    {
        voxelWorld.玩家移动到新区块 += 重新计算要加载的地形;
        voxelWorld.视距变化 += 重新计算要加载的地形;

    }

    public override async void _Process(double delta)
    {
        if(是否需要重新计算需要加载的地形)
        {
            await 计算需要生成的地形();
            await 生成地形();
            await 计算需要加载的地形(voxelWorld.玩家所在区块);
            await 加载地形();

            await 计算需要卸载的地形();
            await 卸载地形();
        }
    }

    private async Task 卸载地形()
    {
        
    }


    private async Task 计算需要卸载的地形()
    {
        
    }


    private async Task 加载地形()
    {
        
    }


    private async Task 生成地形()
    {
        
    }


    private async Task 计算需要生成的地形()
    {
        
    }


    private async Task 计算需要加载的地形(Vector3I 玩家所在区块)
    {
        
    }

    private void 计算需要生成的地形柱(Vector3I 玩家所在区块, int 渲染视距)
    {
        int 预期数量 = (2 * 渲染视距 + 1) * (2 * 渲染视距 + 1);    
        var 当前需要生成的地形柱 = new HashSet<Vector2I>(预期数量);

        // 计算以玩家为中心，视距范围内的所有区块坐标（立方体范围）
        for (int dx = -渲染视距; dx <= 渲染视距; dx++)
        {
            for (int dz = -渲染视距; dz <= 渲染视距; dz++)
            {
                当前需要生成的地形柱.Add(new Vector2I(玩家所在区块.X + dx, 玩家所在区块.Z + dz));
            }
        }

        var 需要添加地形柱 = 当前需要生成的地形柱.Except(已渲染的地形柱).ToHashSet();
        var 需要移除的地形柱 = 已渲染的地形柱.Except(当前需要生成的地形柱);

        // 按距离排序需要添加的地形（从近到远）
        var 排序后的添加地形 = 需要添加地形柱
            .Select(coord => new 
            { 
                坐标 = coord, 
                距离平方 = Mathf.Pow(coord.X - 玩家所在区块.X, 2) + 
                        Mathf.Pow(coord.Y - 玩家所在区块.Z, 2) 
            })
            .OrderBy(item => item.距离平方)
            .Select(item => item.坐标)
            .ToArray();

        // 按距离排序需要移除的地形（从远到近）
        var 排序后的移除地形 = 需要移除的地形柱
            .Select(coord => new 
            { 
                坐标 = coord, 
                距离平方 = Mathf.Pow(coord.X - 玩家所在区块.X, 2) + 
                        Mathf.Pow(coord.Y - 玩家所在区块.Z, 2) 
            })
            .OrderByDescending(item => item.距离平方)
            .Select(item => item.坐标)
            .ToArray();

        foreach (var 地形坐标 in 排序后的添加地形)
        {
            if(已生成的地形柱.Contains(地形坐标))
            {
                要移除的地形柱.Enqueue(地形坐标);
            }
        }

        // 添加到需要生成的地形柱中
        foreach (var 地形坐标 in 排序后的移除地形)
        {
            if(!已生成的地形柱.Contains(地形坐标))
            {
                要生成的地形柱.Enqueue(地形坐标);
            }
        }

        上次需要生成的地形柱 = 需要添加地形柱;
    }


    private async Task 重新计算要加载的地形(Vector3I i)
    {
        是否需要重新计算需要加载的地形 = true;
    }

    

}
using Godot;
using GodotVoxelGame.VoxleData;
using System.Collections.Generic;

public partial class ColliderManager : Node
{
    [Export] public uint CollisionLayer = 1;
    [Export] public uint CollisionMask = 1;
    
    public VoxelWorld voxelWorld { get; set; }
    
    // 存储所有区块的碰撞体
    private Dictionary<Vector3I, StaticBody3D> _chunkColliders = new Dictionary<Vector3I, StaticBody3D>();
    
    // 碰撞形状缓存
    private Dictionary<Vector3I, CollisionShape3D> _collisionShapes = new Dictionary<Vector3I, CollisionShape3D>();

    public override void _Ready()
    {
        voxelWorld = GetParent<VoxelWorld>();
    }

    /// <summary>
    /// 为区块添加碰撞体
    /// </summary>
    public void 添加区块碰撞体(Chunk chunk)
    {
        var chunkPos = chunk.ChunkPosition;
        
        // 如果已经存在碰撞体，先移除
        if (_chunkColliders.ContainsKey(chunkPos))
        {
            移除区块碰撞体(chunkPos);
        }
        
        // 创建静态刚体
        var staticBody = new StaticBody3D
        {
            Name = $"Collider_{chunkPos.X}_{chunkPos.Y}_{chunkPos.Z}",
            Position = new Vector3(chunkPos.X * VoxelConst.CHUNK_SIZE_X, 
                                  chunkPos.Y * VoxelConst.CHUNK_SIZE_Y, 
                                  chunkPos.Z * VoxelConst.CHUNK_SIZE_Z),
            CollisionLayer = CollisionLayer,
            CollisionMask = CollisionMask
        };
        
        // 创建碰撞形状
        var collisionShape = new CollisionShape3D();
        
        // 使用简化的碰撞形状（方块边界框）
        var boxShape = new BoxShape3D
        {
            Size = new Vector3(VoxelConst.CHUNK_SIZE_X, VoxelConst.CHUNK_SIZE_Y, VoxelConst.CHUNK_SIZE_Z)
        };
        
        collisionShape.Shape = boxShape;
        
        // 添加子节点
        staticBody.AddChild(collisionShape);
        AddChild(staticBody);
        
        // 保存引用
        _chunkColliders[chunkPos] = staticBody;
        _collisionShapes[chunkPos] = collisionShape;
    }

    /// <summary>
    /// 移除区块碰撞体
    /// </summary>
    public void 移除区块碰撞体(Vector3I chunkPos)
    {
        if (_chunkColliders.TryGetValue(chunkPos, out var staticBody))
        {
            staticBody.QueueFree();
            _chunkColliders.Remove(chunkPos);
            _collisionShapes.Remove(chunkPos);
        }
    }

    /// <summary>
    /// 更新区块碰撞体
    /// </summary>
    public void 更新区块碰撞体(Chunk chunk)
    {
        移除区块碰撞体(chunk.ChunkPosition);
        添加区块碰撞体(chunk);
    }

    /// <summary>
    /// 检查区块是否有碰撞体
    /// </summary>
    public bool 区块是否有碰撞体(Vector3I chunkPos)
    {
        return _chunkColliders.ContainsKey(chunkPos);
    }

    /// <summary>
    /// 清除所有碰撞体
    /// </summary>
    public void 清除所有碰撞体()
    {
        foreach (var collider in _chunkColliders.Values)
        {
            collider.QueueFree();
        }
        _chunkColliders.Clear();
        _collisionShapes.Clear();
    }

    /// <summary>
    /// 获取碰撞体数量
    /// </summary>
    public int 碰撞体数量()
    {
        return _chunkColliders.Count;
    }
}
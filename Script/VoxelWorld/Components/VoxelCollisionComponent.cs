// VoxelCollisionComponent.cs
using Godot;
using System.Collections.Generic;
using GodotVoxelGame.VoxleData;

public partial class VoxelCollisionComponent : Node
{
    [Export] public bool 启用碰撞 = true;
    [Export] public uint CollisionLayer = 1;
    [Export] public uint CollisionMask = 1;
    [Export] public float CollisionMargin = 0.04f;
    
    // 碰撞优化选项
    [Export] public bool 简化碰撞 = true;
    [Export] public float 简化阈值 = 0.001f;
    [Export] public bool 使用凸包简化 = false;
    [Export] public int 凸包简化等级 = 6; // 0-8，越高越精确
    
    private StaticBody3D _staticBody;
    private CollisionShape3D _collisionShape;
    private ConcavePolygonShape3D _collisionPolygonShape;
    private ConvexPolygonShape3D _convexCollisionShape;
    private bool _isInitialized = false;
    private bool _useConcave = true; // 是否使用凹多边形碰撞
    
    // 用于性能监控
    private int _lastFaceCount = 0;
    private ulong _lastUpdateTime = 0;
    
    public delegate void 碰撞更新完毕事件(bool 成功, int 三角形数量, ulong 耗时, string 碰撞类型);
    public event 碰撞更新完毕事件 碰撞更新完毕;
    
    public override void _Ready()
    {
        初始化碰撞体();
    }

    internal bool 更新碰撞形状(区块顶点数据 meshData)
    {
        清除碰撞形状();


        return true;
    }

    public void 清除碰撞形状()
    {
        if (_collisionPolygonShape != null)
        {
            _collisionPolygonShape.Data = new Vector3[0];
        }
        
        if (_convexCollisionShape != null)
        {
            _convexCollisionShape.Points = new Vector3[0];
        }
        
        if (_collisionShape != null)
        {
            _collisionShape.Disabled = true;
        }
        
        _lastFaceCount = 0;
    }
    
    public void 初始化碰撞体()
    {
        if (_isInitialized) return;
        
        // 创建静态刚体节点
        _staticBody = new StaticBody3D();
        _staticBody.Name = "StaticBody3D";
        _staticBody.CollisionLayer = CollisionLayer;
        _staticBody.CollisionMask = CollisionMask;
        
        // 创建碰撞形状节点
        _collisionShape = new CollisionShape3D();
        _collisionShape.Name = "CollisionShape3D";
        
        // 根据设置选择合适的碰撞形状
        if (使用凸包简化)
        {
            _useConcave = false;
            _convexCollisionShape = new ConvexPolygonShape3D();
            _collisionShape.Shape = _convexCollisionShape;
        }
        else
        {
            _useConcave = true;
            _collisionPolygonShape = new ConcavePolygonShape3D();
            _collisionPolygonShape.Margin = CollisionMargin;
            _collisionShape.Shape = _collisionPolygonShape;
        }
        
        // 将碰撞形状添加到静态刚体
        _staticBody.AddChild(_collisionShape);
        
        // 将静态刚体添加为当前节点的子节点
        AddChild(_staticBody);
        
        _isInitialized = true;
        
        //GD.Print($"[VoxelCollisionComponent] 碰撞组件初始化完成，使用 {(使用凸包简化 ? "凸多边形" : "凹多边形")} 形状");
    }
    
    
}
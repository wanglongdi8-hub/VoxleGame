// VoxelCollisionComponent.cs
using Godot;
using System.Collections.Generic;

[Tool]public partial class VoxelCollisionComponent : Node
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
    
    /// <summary>
    /// 切换碰撞形状类型
    /// </summary>
    public void 切换碰撞类型(bool 使用凹多边形 = true)
    {
        if (_staticBody != null && _collisionShape != null)
        {
            _useConcave = 使用凹多边形;
            使用凸包简化 = !使用凹多边形;
            
            if (_useConcave)
            {
                _collisionPolygonShape = new ConcavePolygonShape3D();
                _collisionPolygonShape.Margin = CollisionMargin;
                _collisionShape.Shape = _collisionPolygonShape;
            }
            else
            {
                _convexCollisionShape = new ConvexPolygonShape3D();
                _collisionShape.Shape = _convexCollisionShape;
            }
            
            //GD.Print($"[VoxelCollisionComponent] 切换到 {(使用凹多边形 ? "凹多边形" : "凸多边形")} 碰撞形状");
        }
    }
    
    /// <summary>
    /// 更新碰撞形状
    /// </summary>
    /// <param name="meshData">网格数据</param>
    /// <returns>是否更新成功</returns>
    internal bool 更新碰撞形状(区块顶点数据 meshData)
    {
        if (!启用碰撞 || !_isInitialized) return false;
        
        if (meshData == null || meshData.vert.Count == 0 || meshData.indice.Count == 0)
        {
            清除碰撞形状();
            return false;
        }
        
        ulong startTime = Time.GetTicksMsec();
        
        try
        {
            bool 成功 = false;
            int 三角形数量 = 0;
            
            if (_useConcave)
            {
                成功 = 更新凹多边形碰撞(meshData, out 三角形数量);
            }
            else
            {
                成功 = 更新凸多边形碰撞(meshData, out 三角形数量);
            }
            
            ulong endTime = Time.GetTicksMsec();
            ulong duration = endTime - startTime;
            _lastUpdateTime = duration;
            
            string 碰撞类型 = _useConcave ? "凹多边形" : "凸多边形";
            碰撞更新完毕?.Invoke(成功, 三角形数量, duration, 碰撞类型);
            
            if (成功)
            {
                //GD.Print($"[VoxelCollisionComponent] {碰撞类型}碰撞更新完成: {三角形数量} 个三角形, 耗时 {duration}ms");
            }
            
            return 成功;
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"[VoxelCollisionComponent] 更新碰撞形状失败: {ex.Message}");
            碰撞更新完毕?.Invoke(false, 0, 0, "错误");
            return false;
        }
    }
    
    /// <summary>
    /// 更新凹多边形碰撞形状
    /// </summary>
    private bool 更新凹多边形碰撞(区块顶点数据 meshData, out int 三角形数量)
    {
        三角形数量 = 0;
        
        if (_collisionPolygonShape == null)
        {
            _collisionPolygonShape = new ConcavePolygonShape3D();
            _collisionShape.Shape = _collisionPolygonShape;
        }
        
        // 生成碰撞面数组
        Vector3[] faces = 生成碰撞面数组(meshData, out 三角形数量);
        
        if (faces.Length > 0)
        {
            // 在 Godot 4 中，ConcavePolygonShape3D 使用 Data 属性
            _collisionPolygonShape.Data = faces;
            _lastFaceCount = 三角形数量;
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// 更新凸多边形碰撞形状
    /// </summary>
    private bool 更新凸多边形碰撞(区块顶点数据 meshData, out int 三角形数量)
    {
        三角形数量 = 0;
        
        if (_convexCollisionShape == null)
        {
            _convexCollisionShape = new ConvexPolygonShape3D();
            _collisionShape.Shape = _convexCollisionShape;
        }
        
        // 生成点集
        List<Vector3> points = 生成碰撞点集(meshData, out 三角形数量);
        
        if (points.Count > 0)
        {
            // 简化点集（如果需要）
            if (points.Count > 1000 && 凸包简化等级 < 8)
            {
                points = 简化点集(points);
            }
            
            _convexCollisionShape.Points = points.ToArray();
            _lastFaceCount = 三角形数量;
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// 生成碰撞面数组（用于凹多边形）
    /// </summary>
    private Vector3[] 生成碰撞面数组(区块顶点数据 meshData, out int 三角形数量)
    {
        三角形数量 = 0;
        List<Vector3> collisionFaces = new List<Vector3>();
        
        // 使用三角形索引生成碰撞面
        for (int i = 0; i < meshData.indice.Count; i += 3)
        {
            if (i + 2 < meshData.indice.Count)
            {
                int idx1 = meshData.indice[i];
                int idx2 = meshData.indice[i + 1];
                int idx3 = meshData.indice[i + 2];
                
                if (idx1 < meshData.vert.Count && 
                    idx2 < meshData.vert.Count && 
                    idx3 < meshData.vert.Count)
                {
                    Vector3 v1 = meshData.vert[idx1];
                    Vector3 v2 = meshData.vert[idx2];
                    Vector3 v3 = meshData.vert[idx3];
                    
                    if (简化碰撞)
                    {
                        // 简化：移除退化三角形（面积过小）
                        if (!是退化三角形(v1, v2, v3))
                        {
                            collisionFaces.Add(v1);
                            collisionFaces.Add(v2);
                            collisionFaces.Add(v3);
                            三角形数量++;
                        }
                    }
                    else
                    {
                        collisionFaces.Add(v1);
                        collisionFaces.Add(v2);
                        collisionFaces.Add(v3);
                        三角形数量++;
                    }
                }
            }
        }
        
        return collisionFaces.ToArray();
    }
    
    /// <summary>
    /// 生成碰撞点集（用于凸多边形）
    /// </summary>
    private List<Vector3> 生成碰撞点集(区块顶点数据 meshData, out int 三角形数量)
    {
        三角形数量 = 0;
        HashSet<Vector3> uniquePoints = new HashSet<Vector3>();
        
        // 收集所有唯一的顶点
        for (int i = 0; i < meshData.indice.Count; i += 3)
        {
            if (i + 2 < meshData.indice.Count)
            {
                int idx1 = meshData.indice[i];
                int idx2 = meshData.indice[i + 1];
                int idx3 = meshData.indice[i + 2];
                
                if (idx1 < meshData.vert.Count && 
                    idx2 < meshData.vert.Count && 
                    idx3 < meshData.vert.Count)
                {
                    Vector3 v1 = meshData.vert[idx1];
                    Vector3 v2 = meshData.vert[idx2];
                    Vector3 v3 = meshData.vert[idx3];
                    
                    if (简化碰撞)
                    {
                        // 简化：移除退化三角形（面积过小）
                        if (!是退化三角形(v1, v2, v3))
                        {
                            uniquePoints.Add(v1);
                            uniquePoints.Add(v2);
                            uniquePoints.Add(v3);
                            三角形数量++;
                        }
                    }
                    else
                    {
                        uniquePoints.Add(v1);
                        uniquePoints.Add(v2);
                        uniquePoints.Add(v3);
                        三角形数量++;
                    }
                }
            }
        }
        
        return new List<Vector3>(uniquePoints);
    }
    
    /// <summary>
    /// 简化点集（用于凸多边形碰撞）
    /// </summary>
    private List<Vector3> 简化点集(List<Vector3> points)
    {
        if (points.Count <= 1000) return points;
        
        // 简单的简化：均匀采样
        int targetCount = Mathf.Max(100, 1000 - 凸包简化等级 * 100);
        if (points.Count <= targetCount) return points;
        
        List<Vector3> simplified = new List<Vector3>();
        float step = (float)points.Count / targetCount;
        
        for (int i = 0; i < targetCount; i++)
        {
            int index = Mathf.Min((int)(i * step), points.Count - 1);
            simplified.Add(points[index]);
        }
        
        GD.Print($"[VoxelCollisionComponent] 点集简化: {points.Count} -> {simplified.Count}");
        return simplified;
    }
    
    /// <summary>
    /// 检查是否是退化三角形（面积过小）
    /// </summary>
    private bool 是退化三角形(Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 ab = b - a;
        Vector3 ac = c - a;
        Vector3 cross = ab.Cross(ac);
        float areaSquared = cross.LengthSquared();
        
        return areaSquared < 简化阈值;
    }
    
    /// <summary>
    /// 清除碰撞形状
    /// </summary>
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
    
    /// <summary>
    /// 启用碰撞组件
    /// </summary>
    public void 启用(bool enabled = true)
    {
        启用碰撞 = enabled;
        
        if (_staticBody != null)
        {
            _staticBody.Visible = enabled;
            _staticBody.SetProcess(enabled);
            _staticBody.SetPhysicsProcess(enabled);
            
            if (_collisionShape != null)
            {
                _collisionShape.Disabled = !enabled;
            }
        }
    }
    
    /// <summary>
    /// 禁用碰撞组件
    /// </summary>
    public void 禁用()
    {
        启用(false);
    }
    
    /// <summary>
    /// 设置碰撞层
    /// </summary>
    public void 设置碰撞层(uint layer, uint mask)
    {
        CollisionLayer = layer;
        CollisionMask = mask;
        
        if (_staticBody != null)
        {
            _staticBody.CollisionLayer = layer;
            _staticBody.CollisionMask = mask;
        }
    }
    
    /// <summary>
    /// 设置碰撞边距
    /// </summary>
    public void 设置碰撞边距(float margin)
    {
        CollisionMargin = margin;
        
        if (_collisionPolygonShape != null)
        {
            _collisionPolygonShape.Margin = margin;
        }
    }
    
    /// <summary>
    /// 获取碰撞统计信息
    /// </summary>
    public string 获取统计信息()
    {
        string 类型 = _useConcave ? "凹多边形" : "凸多边形";
        return $"类型: {类型}, 三角形: {_lastFaceCount}, 耗时: {_lastUpdateTime}ms";
    }
    
    /// <summary>
    /// 重新初始化碰撞体
    /// </summary>
    public void 重新初始化()
    {
        if (_staticBody != null)
        {
            RemoveChild(_staticBody);
            _staticBody.QueueFree();
        }
        
        _collisionPolygonShape = null;
        _convexCollisionShape = null;
        _collisionShape = null;
        _staticBody = null;
        
        _isInitialized = false;
        初始化碰撞体();
    }
    
    /// <summary>
    /// 优化性能：批量更新
    /// </summary>
    internal void 批量更新碰撞(List<区块顶点数据> 所有网格数据)
    {
        if (!启用碰撞 || !_isInitialized) return;
        
        ulong startTime = Time.GetTicksMsec();
        
        // 合并所有网格数据
        区块顶点数据 合并数据 = new 区块顶点数据();
        foreach (var 数据 in 所有网格数据)
        {
            合并数据.AddRange(数据);
        }
        
        // 更新碰撞
        bool 成功 = 更新碰撞形状(合并数据);
        
        ulong endTime = Time.GetTicksMsec();
        if (成功)
        {
            GD.Print($"[VoxelCollisionComponent] 批量更新完成，总耗时: {endTime - startTime}ms");
        }
    }
}
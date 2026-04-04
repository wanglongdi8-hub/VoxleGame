using Godot;
using System;

public partial class MoveComponent : Node3D
{
	[Export] public Node3D Character { get; set; }
    [Export] public AnimationPlayer NtcAnimationPlayer { get; set; }
    [Export]public Player OwnerPlayer { get; set; }
    private Vector3 _targetVelocity = Vector3.Zero;

    /// <summary>
    /// 物理移动更新
    /// </summary>
    public void PhysicsUpdate(double delta, Vector2 inputDir, Node3D camera, bool isOnFloor)
    {
        ApplyGravity(delta, isOnFloor);
        ApplyJump(isOnFloor);
        ApplyMove(inputDir, camera, delta);
    }

    /// <summary>
    /// 应用重力
    /// </summary>
    private void ApplyGravity(double delta, bool isOnFloor)
    {
        if (!isOnFloor)
        {
            OwnerPlayer.Velocity += new Vector3(0, -OwnerPlayer.下落速度, 0) * (float)delta;
        }
    }

    /// <summary>
    /// 应用跳跃
    /// </summary>
    private void ApplyJump(bool isOnFloor)
    {
        if (!OwnerPlayer.InputComponent.IsJumpPressed() || !isOnFloor) return;
        Vector3 vel = OwnerPlayer.Velocity;
        vel.Y = OwnerPlayer.跳跃速度;
        OwnerPlayer.Velocity = vel;
    }

    /// <summary>
    /// 应用移动
    /// </summary>
    private void ApplyMove(Vector2 inputDir, Node3D camera, double delta)
{
    // 获取摄像机方向的移动向量
    Vector3 direction = (camera.GlobalTransform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
    direction.Y = 0;
    
    Vector3 vel = OwnerPlayer.Velocity;
    
    if (direction != Vector3.Zero)
    {
        vel.X = direction.X * OwnerPlayer.移动速度;
        vel.Z = direction.Z * OwnerPlayer.移动速度;
        
        // 让角色旋转到面对移动方向
        RotateTowardsDirection(direction, delta);
    }
    else
    {
        vel.X = Mathf.MoveToward(vel.X, 0, OwnerPlayer.移动速度);
        vel.Z = Mathf.MoveToward(vel.Z, 0, OwnerPlayer.移动速度);
    }
    
    OwnerPlayer.Velocity = vel;
    SetAnimation(direction);
    OwnerPlayer.MoveAndSlide();
}

private void RotateTowardsDirection(Vector3 direction, double delta)
{
    // 计算目标朝向角度
    float targetAngle = Mathf.Atan2(direction.X, direction.Z);
    
    // 当前旋转角度
    Vector3 currentRotation = OwnerPlayer.Rotation;
    
    // 平滑旋转到目标角度
    currentRotation.Y = (float)Mathf.LerpAngle(currentRotation.Y, targetAngle, OwnerPlayer.转身速度 * delta);
    
    OwnerPlayer.Rotation = currentRotation;
}

    /// <summary>
    /// 设置动画与转向
    /// </summary>
    private void SetAnimation(Vector3 direction)
    {
        if (direction != Vector3.Zero)
        {
            float targetAngle = Mathf.Atan2(direction.X, direction.Z) - OwnerPlayer.Rotation.Y;
            Character.Rotation = new Vector3(
                Character.Rotation.X,
                Mathf.LerpAngle(Character.Rotation.Y, targetAngle, OwnerPlayer.转身速度),
                Character.Rotation.Z);
        }
    }
}

using Godot;
using System;

public partial class FlyComponent : Node3D
{
    [Export]public Player OwnerPlayer { get; set; }
	public override void _Ready()
	{
	}

    internal void ToggleFlyMode()
    {
        OwnerPlayer.启用飞行 = !OwnerPlayer.启用飞行;
        if (OwnerPlayer.启用飞行)
        {
            // 进入飞行：清除重力影响，重置垂直速度
            OwnerPlayer.Velocity = new Vector3(OwnerPlayer.Velocity.X, 0, OwnerPlayer.Velocity.Z);
        }
        else
        {
            // 退出飞行：恢复地面重力
            OwnerPlayer.Velocity += new Vector3(0, -OwnerPlayer.下落速度, 0) * 0.1f;
        }
    }

    internal void PhysicsUpdate(double delta, Vector2 inputDir, CameraComponent cameraComponent)
    {
        if (!OwnerPlayer.启用飞行) return;

        // 1. 计算飞行方向（相机朝向）
        Vector3 forward = cameraComponent.GlobalTransform.Basis.Z;
        Vector3 right = cameraComponent.GlobalTransform.Basis.X;
        forward.Y = 0;
        right.Y = 0;
        forward = forward.Normalized();
        right = right.Normalized();

        // 2. 水平移动（WASD）
        Vector3 moveDir = (forward * inputDir.Y + right * inputDir.X).Normalized();
        Vector3 targetVel = moveDir * OwnerPlayer.飞行速度;

        // 3. 垂直移动（空格上升 / LeftControl下降）
        float verticalInput = 0f;
        if (Input.IsActionPressed("fly_up")) verticalInput = 1f;
        if (Input.IsActionPressed("fly_down")) verticalInput = -1f;
        targetVel.Y = verticalInput * OwnerPlayer.垂直速度;

        // 4. 平滑过渡速度 ✅ 正确写法
        OwnerPlayer.Velocity = OwnerPlayer.Velocity.Lerp(
            targetVel,
            (float)delta * OwnerPlayer.飞行加速度
        );

        // 5. 空中减速（无输入时）✅ 正确写法
        if (inputDir == Vector2.Zero && verticalInput == 0f)
        {
            OwnerPlayer.Velocity = OwnerPlayer.Velocity.Lerp(
                Vector3.Zero,
                (float)delta * OwnerPlayer.飞行减速
            );
        }

        // 6. 执行移动
        OwnerPlayer.MoveAndSlide();
    }
}

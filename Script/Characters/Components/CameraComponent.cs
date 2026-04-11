using Godot;
using System;

public partial class CameraComponent : Camera3D
{
	[Export] public Node3D CameraPivot { get; set; }
	[Export]public Player OwnerPlayer { get; set; }
	public override void _Ready()
    {
        
    }

	public void CameraControl(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouseMotion)
        {
            Vector3 currentRotation = CameraPivot.Rotation;
            float newRotationY = currentRotation.Y - mouseMotion.Relative.X * (float)OwnerPlayer.摄像机灵敏度;
            float newRotationX = currentRotation.X - mouseMotion.Relative.Y * (float)OwnerPlayer.摄像机灵敏度;

            float limitRadians = Mathf.DegToRad(OwnerPlayer.摄像机角度限制);
            newRotationX = Mathf.Clamp(newRotationX, -limitRadians, limitRadians);

            CameraPivot.Rotation = new Vector3(newRotationX, newRotationY, currentRotation.Z);
        }
    }
}

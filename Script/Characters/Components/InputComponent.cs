using Godot;
using System;

public partial class InputComponent : Node3D
{
    [Export]public Player OwnerPlayer { get; set; }
	public override void _Ready()
	{
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	public void ProcessInput(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_cancel"))
        {
            GetTree().Quit();
        }
        
        if (@event.IsActionPressed("fly_fly"))
        {
            OwnerPlayer.FlyComponent.ToggleFlyMode();
        }

		if (@event.IsActionPressed("game_mous"))
        {
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }    
        
        OwnerPlayer.CameraComponent.CameraControl(@event);
    }

	public Vector2 GetMoveInput()
    {
        return Input.GetVector("move_left", "move_right", "move_forward", "move_back");
    }

    public Vector2 GetFlyInput()
    {
        return Input.GetVector("fly_left", "fly_right", "fly_forward", "fly_back");
    }

	public bool IsJumpPressed()
    {
        return Input.IsActionJustPressed("move_jump");
    }

    

}

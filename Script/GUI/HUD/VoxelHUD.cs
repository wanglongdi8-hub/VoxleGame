using Godot;
using System;

public partial class VoxelHUD : BaseGUIView
{
	[Export] public Label 帧率 { get; set; }
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		帧率.Text = Engine.GetFramesPerSecond().ToString();
	}
}

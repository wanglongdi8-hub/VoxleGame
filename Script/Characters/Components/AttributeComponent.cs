using Godot;
using System;

public partial class AttributeComponent : Node3D
{
	[Export]public Player OwnerPlayer { get; set; }
	public override void _Ready()
	{
	}
	public override void _Process(double delta)
	{
	}
}

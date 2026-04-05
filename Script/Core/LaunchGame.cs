using Godot;
using System;

public partial class LaunchGame : Node
{
	public override void _Ready()
	{
		GetNode<G>("/root/G").GetGUIViewManager().OpenView("StartMenu");
	}

}
using Godot;
using System;

public partial class LaunchGame : Node
{
	public override void _Ready()
	{
		G.Instance.GetGUIViewManager().OpenView("StartMenu");
	}

}
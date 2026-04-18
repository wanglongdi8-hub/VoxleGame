using Godot;
using System;

public partial class MainGameMode : GameModeBase
{
	public override void _Ready()
	{
		GUIManager.OpenView("HUD");
	}

	public override void _Process(double delta)
	{
	}
}

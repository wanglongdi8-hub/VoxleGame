using Godot;
using System;

public partial class StartMenut : BaseGUIView
{

	protected override void Open()
	{
		
	}

	protected override void Close()
	{
		
	}

	public void OnQuitButtonPressed()
	{
		GetTree().Quit();
	}
}

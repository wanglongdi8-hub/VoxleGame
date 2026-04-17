using Godot;
using System;

public partial class GameModeBase : Node
{
	[ExportGroup("组件引用")]
	[Export]public GUIViewManager GUIManager{get; set;}

	public override void _Ready()
	{
		
	}

}

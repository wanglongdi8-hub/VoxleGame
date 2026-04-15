using Godot;
using System;

public partial class G : Node
{
	public static G Instance { get; private set; }


	public BlockAtlasConfig AtlasConfig { get; set; }
	public bool 启用面剔除 { get; set; } = true;

	public override void _Ready()
    {
        Instance = this;
		Engine.MaxFps = 90;
    }
	
	public  Node GetGmaeRoot()
	{
		return GetNode("/root/Game");
	}

	public GUIViewManager GetGUIViewManager()
	{
		return GetGmaeRoot().GetNode<GUIViewManager>("%GUIViewManager");	
	}
}

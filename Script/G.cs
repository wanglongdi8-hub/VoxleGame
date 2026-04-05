using Godot;
using System;

public partial class G : Node
{
	public static G Instance { get; private set; }

	public override void _Ready()
    {
        Instance = this;
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

using Godot;
using System;

public partial class G : Node
{
	public  Node GetGmaeRoot()
	{
		return GetNode("/root/Game");
	}

	public GUIViewManager GetGUIViewManager()
	{
		return GetGmaeRoot().GetNode<GUIViewManager>("%GUIViewManager");	
	}
}

using Godot;
using Godot.Collections;
using System;

public partial class GUIViewManager : Node
{
	[Export] public Godot.Collections.Array<GUIViewConfig> ViewConfigList { get; set; } = [];
	[Export] public Node GuiRoot { get; set; } = null;


}

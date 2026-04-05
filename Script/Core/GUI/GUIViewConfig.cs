using Godot;
using System;

public partial class GUIViewConfig : Resource
{
    [Export] StringName Id { get; set; }
    [Export] PackedScene Prefab { get; set; }

    
}

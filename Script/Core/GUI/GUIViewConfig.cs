using Godot;
using System;

[GlobalClass]
public partial class GUIViewConfig : Resource
{
    [Export] public StringName Id { get; set; }
    [Export] public PackedScene Prefab { get; set; }

    
}

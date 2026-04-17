using Godot;
using System;

[GlobalClass]
public partial class SceneConfig : Resource
{
    [Export] public StringName Id { get; set; }
    [Export] public PackedScene ScenePack { get; set; }
}

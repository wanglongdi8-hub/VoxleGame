using Godot;
using System;

[GlobalClass]
public partial class BlockAtlasConfig : Resource
{
    [Export] public Texture2D AtlasTexture { get; set; }
    [Export] public int AtlasColumns { get; set; } = 16;
    [Export] public int AtlasRows { get; set; } = 16;
    
    // 方块ID到UV坐标的映射
    [Export] public Godot.Collections.Dictionary<int, Vector2I> BlockUVOffsets { get; set; } = new();
}
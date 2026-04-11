using Godot;
using System;

[GlobalClass]
public partial class BlockAtlasConfig : Resource
{
    [Export] public Material AtlasTexture { get; set; }
    [Export] public int 列 { get; set; } = 8;
    [Export] public int 行 { get; set; } = 4;
    
    // 方块ID到UV坐标的映射
    [Export] public Godot.Collections.Dictionary<int, int[]> BlockFaceTextures { get; set; } = new();

    // 获取方块某个面的材质ID
    public int GetFaceTextureID(int blockId, int faceIndex)
    {
        if (BlockFaceTextures.TryGetValue(blockId, out var faceTextures))
        {
            if (faceIndex >= 0 && faceIndex < faceTextures.Length)
            {
                return faceTextures[faceIndex];
            }
        }
        
        // 如果找不到，返回默认材质（通常是方块的第一面材质）
        return 0;
    }
}


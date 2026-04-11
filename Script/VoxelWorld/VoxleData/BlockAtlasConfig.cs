using Godot;
using System;

[GlobalClass]
public partial class BlockAtlasConfig : Resource
{
    [Export] public Material AtlasTexture { get; set; }
    [Export] public int 列 { get; set; } = 8;
    [Export] public int 行 { get; set; } = 4;
    
    // 方块ID到UV坐标的映射
    [Export] public Godot.Collections.Dictionary<int, Godot.Collections.Array<Vector2[]>> BlockFaceUVs { get; set; } = new();

    // 获取方块某个面的UV坐标
    public Vector2[] GetFaceUVs(int blockId, int faceIndex)
    {
        if (BlockFaceUVs.TryGetValue(blockId, out var faceUVsArray))
        {
            if (faceIndex >= 0 && faceIndex < faceUVsArray.Count)
            {
                return faceUVsArray[faceIndex];
            }
        }
        
        // 如果找不到，返回默认UV坐标（整个纹理）
        return new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        };
    }

}
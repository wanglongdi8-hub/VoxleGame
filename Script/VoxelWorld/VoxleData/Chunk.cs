using Godot;

namespace GodotVoxelGame.VoxleData;
public class Chunk
{
    public Vector3I ChunkPosition { get; set; }
    
    private ushort[] _voxels = new ushort[VoxelConst.CHUNK_VOLUME];

    public Chunk(Vector3I 区块坐标)
    {
        ChunkPosition = 区块坐标;
    }

    public ushort GetVoxel(int x, int y, int z)
    {
        int index = Utils.ToFlatIndex(x, y, z, VoxelConst.CHUNK_SIZE_Y, VoxelConst.CHUNK_SIZE_X);
        return _voxels[index];
    }
    
    public void SetVoxel(int x, int y, int z, ushort id)
    {
        if (!IsInBounds(x, y, z))
            return;
        
        int index = Utils.ToFlatIndex(x, y, z, VoxelConst.CHUNK_SIZE_Y, VoxelConst.CHUNK_SIZE_X);
        _voxels[index] = id;
    }
    
    private static bool IsInBounds(int x, int y, int z)
    {
        return x >= 0 && x < VoxelConst.CHUNK_SIZE_X &&
               y >= 0 && y < VoxelConst.CHUNK_SIZE_Y &&
               z >= 0 && z < VoxelConst.CHUNK_SIZE_Z;
    }

    public void ClearVoxels()
    {
        System.Array.Clear(_voxels, 0, _voxels.Length);
    }
}
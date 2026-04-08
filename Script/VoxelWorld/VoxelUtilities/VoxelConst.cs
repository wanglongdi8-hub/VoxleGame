public static class VoxelConst
{
	// ================================
    // 区块相关
    // ================================
    public const ushort CHUNK_SIZE_X = 32;
    public const ushort CHUNK_SIZE_Y = 32;
    public const ushort CHUNK_SIZE_Z = 32;
    public const ushort CHUNK_VOLUME = CHUNK_SIZE_X * CHUNK_SIZE_Z * CHUNK_SIZE_Y;

    // ================================
    // 方块相关
    // ================================
    public const float VOXEL_SIZE = 1.0f;  

    public const ushort 石头 = 1;

    // ================================
    // 世界相关
    // ================================
    public const float 世界高度上限 = 64;
}

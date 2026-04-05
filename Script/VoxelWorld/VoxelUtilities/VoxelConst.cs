public static class VoxelConst
{
	// ================================
    // 区块相关
    // ================================
    public const int CHUNK_SIZE_X = 32;
    public const int CHUNK_SIZE_Y = 32;
    public const int CHUNK_SIZE_Z = 32;
    public const int CHUNK_VOLUME = CHUNK_SIZE_X * CHUNK_SIZE_Z * CHUNK_SIZE_Y;

	// ================================
    // 地形块相关
    // ================================
    public const int 预计算大小 = 128;

    // ================================
    // 方块相关
    // ================================
    public const float VOXEL_SIZE = 1.0f;  
}

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

    public const ushort 空气 = 0;
    public const ushort 石头 = 1;
    public const ushort 沙子 = 2;
    public const ushort 水 = 3;
    public const ushort 雪块 = 4;
    public const ushort 草地 = 5;
    public const ushort 泥土 = 6;
    public const ushort 煤矿石 = 7;
    public const ushort 铁矿石 = 8;
    public const ushort 金矿石 = 9;
    public const ushort 木头 = 10;
    public const ushort 树叶 = 11;

    // ================================

    public const float 世界高度上限 = 64;

}

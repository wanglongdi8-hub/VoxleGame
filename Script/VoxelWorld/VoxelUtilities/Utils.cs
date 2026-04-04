
public static class Utils 
{
	/// <summary>
    /// 三维坐标 转 一维索引
    /// </summary>
    public static int ToFlatIndex(
        int x, int y, int z,
        int height, int depth)
    {
        return x * height * depth + y * depth + z;
    }
    
    /// <summary>
    /// 一维索引 转 三维坐标 (x,y,z)
    /// </summary>
    public static (int x, int y, int z) To3DCoord(
        int flatIndex,
        int width, int height, int depth)
    {
        int x = flatIndex / (height * depth);
        int rem = flatIndex % (height * depth);
        int y = rem / depth;
        int z = rem % depth;

        return (x, y, z);
    }      // 单个方块大小（单位：米）

}

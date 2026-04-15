using Godot;

namespace GodotVoxelGame.VoxelOctree;
/// <summary>
/// 表示一个三维空间中的轴对齐边界框
/// 用于定义八叉树节点的空间范围
/// </summary>
public class BoundingBox
    {
        public Vector3 Min { get; set; }  // 边界框的最小坐标点
        public Vector3 Max { get; set; }  // 边界框的最大坐标点
        public Vector3 Center { get; private set; }  // 中心点，预计算以提高性能

        public BoundingBox(Vector3 min, Vector3 max)
        {
            Min = min;
            Max = max;
            Center = (Min + Max) * 0.5f;  // 计算中心点
        }

        /// <summary>
        /// 检查点是否在边界框内
        /// </summary>
        public bool Contains(Vector3 point)
        {
            return point.X >= Min.X && point.X <= Max.X &&
                   point.Y >= Min.Y && point.Y <= Max.Y &&
                   point.Z >= Min.Z && point.Z <= Max.Z;
        }

        /// <summary>
        /// 检查两个边界框是否相交
        /// </summary>
        public bool Intersects(BoundingBox other)
        {
            return Min.X <= other.Max.X && Max.X >= other.Min.X &&
                   Min.Y <= other.Max.Y && Max.Y >= other.Min.Y &&
                   Min.Z <= other.Max.Z && Max.Z >= other.Min.Z;
        }
    }
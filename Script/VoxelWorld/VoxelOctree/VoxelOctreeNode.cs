using System.Collections.Generic;
using Godot;
using GodotVoxelGame.VoxleData;

namespace GodotVoxelGame.VoxelOctree;

/// <summary>
    /// 八叉树节点类
    /// 这是八叉树的核心数据结构
    /// </summary>
    public class OctreeNode
    {
        // 子节点数组，最多8个子节点
        public OctreeNode[] Children {get;set;}= new OctreeNode[8];
        
        // 当前节点存储的区块
        public Chunk Chunk { get; private set; }
        
        // 节点的空间边界
        public BoundingBox Bounds { get; private set; }
        
        // 节点深度（根节点为0）
        public int Depth { get; private set; }
        
        // 父节点引用
        public OctreeNode Parent { get; private set; }
        
        // 节点是否被细分的标志
        public bool IsSubdivided { get; private set; }
        
        // 最小细分深度，防止无限细分
        public const int MAX_DEPTH = 8;
        public const int MIN_SIZE = 16;  // 最小节点大小（与区块大小匹配）

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="bounds">节点边界</param>
        /// <param name="depth">节点深度</param>
        /// <param name="parent">父节点</param>
        public OctreeNode(BoundingBox bounds, int depth = 0, OctreeNode parent = null)
        {
            Bounds = bounds;
            Depth = depth;
            Parent = parent;
            IsSubdivided = false;
        }

        /// <summary>
        /// 插入区块到八叉树
        /// 如果节点太大，会自动细分
        /// </summary>
        public bool Insert(Chunk chunk)
        {
            // 检查区块是否在节点边界内
            if (!Bounds.Contains(chunk.ChunkPosition))
                return false;

            // 计算节点尺寸
            float nodeSize = Bounds.Max.X - Bounds.Min.X;
            
            // 如果节点足够小，或者达到最大深度，直接存储区块
            if (nodeSize <= MIN_SIZE || Depth >= MAX_DEPTH)
            {
                // 如果节点已有区块，需要处理冲突（这里简单替换）
                if (Chunk != null)
                {
                    // 实际游戏中可能需要合并或选择LOD更高的区块
                    Chunk = chunk;
                }
                else
                {
                    Chunk = chunk;
                }
                return true;
            }
            
            // 如果节点太大，需要细分
            if (!IsSubdivided)
            {
                Subdivide();
            }
            
            // 尝试插入到合适的子节点
            for (int i = 0; i < 8; i++)
            {
                if (Children[i] != null && Children[i].Insert(chunk))
                {
                    return true;
                }
            }
            
            return false;
        }

        /// <summary>
        /// 细分节点为8个子节点
        /// 这是八叉树的核心操作
        /// </summary>
        private void Subdivide()
        {
            if (IsSubdivided) return;
            
            Vector3 center = Bounds.Center;
            Vector3 min = Bounds.Min;
            Vector3 max = Bounds.Max;
            
            // 创建8个子节点，每个对应一个八分圆
            // 顺序遵循惯例：从后左下到前右上
            Children[0] = new OctreeNode(new BoundingBox(
                new Vector3(min.X, min.Y, min.Z),
                new Vector3(center.X, center.Y, center.Z)), Depth + 1, this);
                
            Children[1] = new OctreeNode(new BoundingBox(
                new Vector3(center.X, min.Y, min.Z),
                new Vector3(max.X, center.Y, center.Z)), Depth + 1, this);
                
            Children[2] = new OctreeNode(new BoundingBox(
                new Vector3(min.X, center.Y, min.Z),
                new Vector3(center.X, max.Y, center.Z)), Depth + 1, this);
                
            Children[3] = new OctreeNode(new BoundingBox(
                new Vector3(center.X, center.Y, min.Z),
                new Vector3(max.X, max.Y, center.Z)), Depth + 1, this);
                
            Children[4] = new OctreeNode(new BoundingBox(
                new Vector3(min.X, min.Y, center.Z),
                new Vector3(center.X, center.Y, max.Z)), Depth + 1, this);
                
            Children[5] = new OctreeNode(new BoundingBox(
                new Vector3(center.X, min.Y, center.Z),
                new Vector3(max.X, center.Y, max.Z)), Depth + 1, this);
                
            Children[6] = new OctreeNode(new BoundingBox(
                new Vector3(min.X, center.Y, center.Z),
                new Vector3(center.X, max.Y, max.Z)), Depth + 1, this);
                
            Children[7] = new OctreeNode(new BoundingBox(
                new Vector3(center.X, center.Y, center.Z),
                new Vector3(max.X, max.Y, max.Z)), Depth + 1, this);
            
            IsSubdivided = true;
            
            // 如果当前节点有区块，需要下放到子节点
            if (Chunk != null)
            {
                Chunk chunkToRedistribute = Chunk;
                Chunk = null;
                Insert(chunkToRedistribute);
            }
        }

        /// <summary>
        /// 查询特定位置的区块
        /// 这是八叉树的主要优势：快速空间查询
        /// </summary>
        public Chunk Query(Vector3 position)
        {
            // 检查位置是否在节点边界内
            if (!Bounds.Contains(position))
                return null;
            
            // 如果节点是叶子节点且有区块，返回它
            if (!IsSubdivided && Chunk != null)
            {
                // 检查区块是否包含该位置
                Vector3 chunkEnd = Chunk.ChunkPosition + new Vector3(VoxelConst.CHUNK_SIZE_X, VoxelConst.CHUNK_SIZE_Y, VoxelConst.CHUNK_SIZE_Z);
                BoundingBox chunkBounds = new BoundingBox(Chunk.ChunkPosition, chunkEnd);
                
                if (chunkBounds.Contains(position))
                {
                    return Chunk;
                }
            }
            
            // 如果有子节点，递归查询
            if (IsSubdivided)
            {
                foreach (var child in Children)
                {
                    var result = child?.Query(position);
                    if (result != null)
                        return result;
                }
            }
            
            return null;
        }

        /// <summary>
        /// 获取特定位置的方块
        /// 封装了区块查询和方块获取
        /// </summary>
        public ushort GetBlockAt(Vector3 worldPosition)
        {
            Chunk chunk = Query(worldPosition);
            if (chunk == null)
                return 0;  // 空气方块
            
            // 转换为区块局部坐标
            Vector3 localPos = worldPosition - chunk.ChunkPosition;
            int x = (int)localPos.X;
            int y = (int)localPos.Y;
            int z = (int)localPos.Z;
            
            return chunk.GetVoxel(x, y, z);
        }

        /// <summary>
        /// 获取所有区块（用于保存或遍历）
        /// </summary>
        public List<Chunk> GetAllChunks()
        {
            List<Chunk> chunks = new List<Chunk>();
            CollectChunks(this, chunks);
            return chunks;
        }

        /// <summary>
        /// 递归收集所有区块
        /// </summary>
        private void CollectChunks(OctreeNode node, List<Chunk> chunks)
        {
            if (node == null) return;
            
            if (node.Chunk != null)
            {
                chunks.Add(node.Chunk);
            }
            
            if (node.IsSubdivided)
            {
                foreach (var child in node.Children)
                {
                    if (child != null)
                    {
                        CollectChunks(child, chunks);
                    }
                }
            }
        }

        /// <summary>
        /// 合并节点（如果所有子节点都有相同LOD的区块，可以合并）
        /// 用于动态LOD管理
        /// </summary>
        public bool TryMerge()
        {
            if (!IsSubdivided) return false;
            
            // 检查所有子节点是否都是叶子节点且有区块
            bool canMerge = true;
            foreach (var child in Children)
            {
                if (child == null || child.IsSubdivided || child.Chunk == null)
                {
                    canMerge = false;
                    break;
                }
            }
            
            if (canMerge)
            {
                // 这里可以实现合并逻辑
                // 例如：创建低LOD的合并区块
                // 暂时只清空子节点
                Children = new OctreeNode[8];
                IsSubdivided = false;
                return true;
            }
            
            return false;
        }
    }

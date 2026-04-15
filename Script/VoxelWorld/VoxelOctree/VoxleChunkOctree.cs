using System;
using System.Collections.Generic;
using Godot;
using GodotVoxelGame.VoxleData;

namespace GodotVoxelGame.VoxelOctree;

/// <summary>
    /// 八叉树管理类
    /// 提供高级接口和世界管理功能
    /// </summary>
    public class ChunkOctree
    {
        public OctreeNode Root { get; private set; }
        public int TotalChunks { get; private set; }
        
        // 世界边界
        private BoundingBox worldBounds;
        
        public ChunkOctree(Vector3 worldSize)
        {
            // 创建世界边界（以原点为中心）
            Vector3 halfSize = worldSize * 0.5f;
            worldBounds = new BoundingBox(-halfSize, halfSize);
            
            Root = new OctreeNode(worldBounds);
            TotalChunks = 0;
        }
        
        /// <summary>
        /// 添加区块到世界
        /// </summary>
        public bool AddChunk(Chunk chunk)
        {
            if (Root.Insert(chunk))
            {
                TotalChunks++;
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// 获取位置处的区块
        /// </summary>
        public Chunk GetChunkAt(Vector3 position)
        {
            return Root.Query(position);
        }
        
        /// <summary>
        /// 获取位置处的方块
        /// </summary>
        public ushort GetBlockAt(Vector3 position)
        {
            return Root.GetBlockAt(position);
        }
        
        /// <summary>
        /// 设置位置处的方块
        /// 如果区块不存在，会自动创建
        /// </summary>
        public void SetBlockAt(Vector3 position, byte blockId)
        {
            // 计算区块位置（区块对齐）
            Vector3 chunkPos = new Vector3(
                (float)Math.Floor(position.X / VoxelConst.CHUNK_SIZE_X) * VoxelConst.CHUNK_SIZE_X,
                (float)Math.Floor(position.Y / VoxelConst.CHUNK_SIZE_Y) * VoxelConst.CHUNK_SIZE_Y,
                (float)Math.Floor(position.Z / VoxelConst.CHUNK_SIZE_Z) * VoxelConst.CHUNK_SIZE_Z);
            
            // 获取或创建区块
            Chunk chunk = GetChunkAt(chunkPos);
            if (chunk == null)
            {
                chunk = new Chunk((Vector3I)chunkPos);
                AddChunk(chunk);
            }
            
            // 计算区块内坐标
            Vector3 localPos = position - chunkPos;
            int x = (int)localPos.X;
            int y = (int)localPos.Y;
            int z = (int)localPos.Z;
            
            chunk.SetVoxel(x, y, z, blockId);
        }
        
        /// <summary>
        /// 获取视锥体内的区块（用于渲染）
        /// </summary>
        public List<Chunk> GetChunksInFrustum(BoundingBox frustumBounds)
        {
            List<Chunk> visibleChunks = new List<Chunk>();
            CollectChunksInBounds(Root, frustumBounds, visibleChunks);
            return visibleChunks;
        }
        
        /// <summary>
        /// 递归收集边界内的区块
        /// </summary>
        private void CollectChunksInBounds(OctreeNode node, BoundingBox bounds, List<Chunk> result)
        {
            if (node == null) return;
            
            // 如果节点与边界不相交，跳过
            if (!node.Bounds.Intersects(bounds))
                return;
            
            // 如果节点有区块且在边界内，添加
            if (node.Chunk != null)
            {
                Vector3 chunkEnd = node.Chunk.ChunkPosition + new Vector3(VoxelConst.CHUNK_SIZE_X, VoxelConst.CHUNK_SIZE_Y, VoxelConst.CHUNK_SIZE_Z);
                BoundingBox chunkBounds = new BoundingBox(node.Chunk.ChunkPosition, chunkEnd);
                
                if (chunkBounds.Intersects(bounds))
                {
                    result.Add(node.Chunk);
                }
            }
            
            // 递归检查子节点
            if (node.IsSubdivided)
            {
                foreach (var child in node.Children)
                {
                    CollectChunksInBounds(child, bounds, result);
                }
            }
        }
        
        /// <summary>
        /// 获取所有区块
        /// </summary>
        public List<Chunk> GetAllChunks()
        {
            return Root.GetAllChunks();
        }
    }
using Godot;
using GodotVoxelGame.VoxleData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading.Tasks;

[Tool]
public partial class TerrainManager : Node
{
    private VoxelWorld _voxelWorld;
    [ExportGroup("噪声设置")]
    [Export] private FastNoiseLite _地形噪声 = new FastNoiseLite();
    [Export] private FastNoiseLite _细节噪声 = new FastNoiseLite();

    [ExportGroup("地形设置")]
    [Export] public float 地形缩放 = 0.0000000001f;
    [Export] public float 地形幅度 = 50f;
    [Export] public int 基础高度 = 200;
    [Export]int 高度变化 = 64; // 高度变化范围


    [Export] public float 细节缩放 = 0.005f;
    [Export] public float 细节幅度 = 5f;

    [Export] public int 世界高度上限 = 2048;
    [Export] public int 海平面高度 = 65;

    /*****************        事件         **********************/
    //public event Func<Vector3I, Task> 地形数据被生成;

    /*****************        事件         **********************/



    public override void _Ready()
    {
        _voxelWorld = GetParent<VoxelWorld>();
        InitNoise();

    }


    public Dictionary<Vector3I, Chunk> 生成地形数据(Vector2I ChunkPos)

    {
        Dictionary<Vector3I, Chunk> ChunkDic = [];

        int worldStartX = ChunkPos.X * VoxelConst.CHUNK_SIZE_X;
        int worldStartZ = ChunkPos.Y * VoxelConst.CHUNK_SIZE_Z;

        for (int x = 0; x < VoxelConst.CHUNK_SIZE_X; x++)
        {
            for (int z = 0; z < VoxelConst.CHUNK_SIZE_Z; z++)
            {
                int worldX = worldStartX + x;
                int worldZ = worldStartZ + z;


                float 主噪声 = _地形噪声.GetNoise2D(worldX, worldZ) * 地形幅度;
                float 细节噪声 = _细节噪声.GetNoise2D(worldX, worldZ) * 细节幅度;
                float 总噪声 = 主噪声 + 细节噪声;

                // 使用基础高度+噪声变化

                int 地形高度 = 基础高度 + (int)(总噪声 * 高度变化);

                // 确保地形高度在合理范围内


                地形高度 = Math.Clamp(地形高度, 1, 世界高度上限 - 1); // 避免生成在世界边界

                // 计算chunk位置

                int chunkY = 地形高度 / VoxelConst.CHUNK_SIZE_Y;
                int localY = 地形高度 % VoxelConst.CHUNK_SIZE_Y;


                Vector3I newChunkPos = new(ChunkPos.X, chunkY, ChunkPos.Y);

                if (!ChunkDic.ContainsKey(newChunkPos))
                {
                    Chunk newChunk = new Chunk(newChunkPos);
                    newChunk.SetVoxel(x, localY, z, VoxelConst.石头);
                    ChunkDic.Add(newChunkPos, newChunk);
                }
                else
                {
                    ChunkDic[newChunkPos].SetVoxel(x, localY, z, VoxelConst.石头);
                }

            }
        }

        return ChunkDic;
    }

    public Chunk 生成测试地形(Vector3I ChunkPos)
    {
        Chunk newChunk = new Chunk(ChunkPos);

        // 在区块内随机填充4096个 石头
        Random random = new Random();
        int blocksToPlace = 4096;
        
        for (int i = 0; i < blocksToPlace; i++)
        {
            // 随机生成区块内的坐标
            int x = random.Next(0, VoxelConst.CHUNK_SIZE_X);
            int y = random.Next(0, VoxelConst.CHUNK_SIZE_Y);
            int z = random.Next(0, VoxelConst.CHUNK_SIZE_Z);
            
            // 设置石头方块
            newChunk.SetVoxel(x, y, z, VoxelConst.石头);
        }

        return newChunk;
    }



    public void InitNoise()
    {
        // 主地形噪声
        _地形噪声.NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin;
        _地形噪声.Seed = GD.RandRange(0, 99999);
        _地形噪声.Frequency = 地形缩放;
        _地形噪声.FractalOctaves = 6;
        _地形噪声.FractalGain = 0.5f;
        _地形噪声.FractalLacunarity = 2.0f;
        _地形噪声.FractalType = FastNoiseLite.FractalTypeEnum.Fbm;

        // 细节噪声

        _细节噪声.NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin;
        _细节噪声.Seed = GD.RandRange(0, 99999);
        _细节噪声.Frequency = 细节缩放;
        _细节噪声.FractalOctaves = 2;
    }


}
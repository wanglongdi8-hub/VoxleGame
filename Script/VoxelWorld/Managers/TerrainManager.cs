using Godot;
using System.Threading.Tasks;

public partial class TerrainManager : Node
{
	private VoxelWorld _voxelWorld;
	public override void _Ready()
	{
		_voxelWorld = GetParent<VoxelWorld>();
		_voxelWorld.ChunkManager.新地形被添加 += 添加地形;
	}

    private async Task 添加地形(object arg1, Vector2I i)
    {
        Chunk chunk = new();
		chunk.ChunkPosition = new Vector3I(i.X, 0, i.Y);
		填充区块底层方块(chunk);

		_voxelWorld.ChunkManager.AddChunk(chunk);
    }

	void 填充区块底层方块(Chunk chunk)
    {
        // 只填充 Y=0 层（最底层）
        int groundY = 0;

        for (int x = 0; x < VoxelConst.CHUNK_SIZE_X; x++)
        {
            for (int z = 0; z < VoxelConst.CHUNK_SIZE_Z; z++)
            {
                chunk.SetVoxel(x, groundY, z, 1);
            }
        }
    }

    public override void _Process(double delta)
	{
		
	}

}

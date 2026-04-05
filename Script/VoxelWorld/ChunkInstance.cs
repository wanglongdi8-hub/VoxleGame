using Godot;
using GodotVoxelGame.VoxleData;
using System;

public partial class ChunkInstance : Node
{
	public Chunk chunkData { get;  set; }
	public MeshGenComponent meshComponent { get; set; }
	public ColliderComponent colliderComponent { get; set; }
	
	
}

using Godot;
using GodotVoxelGame.VoxleData;
using System;
using System.ComponentModel;

public partial class ChunkInstance(Chunk chunkData) : Node
{
	public Chunk ChunkData { get;  set; } = chunkData;

	public override void _Ready()
	{

	}


}

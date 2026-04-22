using Godot;
using GodotVoxelGame.VoxleData;
using System;
using System.ComponentModel;

public partial class ChunkInstance : Node
{
	public Chunk chunkData { get;  set; }

	public ChunkInstance(Chunk chunkData)
	{
		this.chunkData = chunkData;
	}
	
	public override void _Ready()
	{

	}

}

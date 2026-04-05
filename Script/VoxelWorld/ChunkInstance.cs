using Godot;
using GodotVoxelGame.VoxleData;
using System;
using System.ComponentModel;

public partial class ChunkInstance : Node
{
	public Chunk chunkData { get;  set; }
	public MeshGenComponent meshComponent { get; set; }
	public ColliderComponent colliderComponent { get; set; }
	
	public override void _Ready()
	{
		ComponentInit();	
	}

    private void ComponentInit()
    {
        meshComponent = new MeshGenComponent();
        colliderComponent = new ColliderComponent();
        
        AddChild(meshComponent);
		AddChild(colliderComponent);
    }

}

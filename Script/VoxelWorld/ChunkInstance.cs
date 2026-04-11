using Godot;
using GodotVoxelGame.VoxleData;
using System;
using System.ComponentModel;

public partial class ChunkInstance : Node
{
	public Chunk chunkData { get;  set; }
	public MeshGenComponent meshComponent { get; set; }
	public ColliderComponent colliderComponent { get; set; }

	public ChunkInstance(Chunk chunkData)
	{
		this.chunkData = chunkData;
	}
	
	public override void _Ready()
	{
		ComponentInit();
		渲染网格();
	}

    public void 渲染网格()
    {
        meshComponent.渲染不带材质的网格(chunkData);
    }

	public void 清空网格()
    {
        meshComponent.清空网格();
    }


    private void ComponentInit()
    {
        meshComponent = new MeshGenComponent();
        colliderComponent = new ColliderComponent();
        
        AddChild(meshComponent);
		AddChild(colliderComponent);
    }

}

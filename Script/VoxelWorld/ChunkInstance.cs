using Godot;
using GodotVoxelGame.Components;
using GodotVoxelGame.VoxleData;
using System;
using System.Collections.Generic;
using System.ComponentModel;

public partial class ChunkInstance : Node
{
	public Chunk chunkData { get;  set; }
	private CollisionComponent collisionComponent;
	public ChunkInstance(Chunk chunkData)
	{
		this.chunkData = chunkData;
	}
	
	public override void _Ready()
	{
		collisionComponent = new CollisionComponent();
		AddChild(collisionComponent);
		// 延迟一帧后再更新碰撞形状，确保_collisionShape已在collisionComponent的_Ready中初始化
		CallDeferred(nameof(UpdateCollisionShape));
	}
	
	private void UpdateCollisionShape()
	{
		collisionComponent.更新碰撞形状(chunkData.顶点数据);
	}



}
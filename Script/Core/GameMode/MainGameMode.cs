using Godot;
using System;

public partial class MainGameMode : GameModeBase
{
	private VoxelWorld _voxelWorld;
	private Player _Player;
	public override void _Ready()
	{
		GUIManager.OpenView("HUD");

		
		
		初始化场景();


	}

    public override void _Process(double delta)
	{
	}

	private void 初始化场景()
    {
		// 加载玩家
		var _PlayerPackedScene = GD.Load<PackedScene>("uid://djm8ih2gs78mk");
		if(_PlayerPackedScene != null)
		{
			_Player = _PlayerPackedScene.Instantiate<Player>();
			// TODO: 根据地形生成玩家位置
			_Player.Position = new Vector3(0,100,0);
			AddChild(_Player);
		}

		// 加载VoxelWorld场景
        var _voxelWorldPackedScene = GD.Load<PackedScene>("uid://dy5riq6n2ec5b");
		if(_voxelWorldPackedScene != null)
		{
			_voxelWorld = _voxelWorldPackedScene.Instantiate<VoxelWorld>();

			_voxelWorld.玩家引用 = _Player;
			_voxelWorld.视距 = 10;

			AddChild(_voxelWorld);
		}
		
    }
}

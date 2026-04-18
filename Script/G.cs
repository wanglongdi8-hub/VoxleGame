using Godot;
using System;

public partial class G : Node
{
	public static G Instance { get; private set; }
    private SceneManager 场景管理器 {get; set;}
    public BlockAtlasConfig AtlasConfig { get; set; }
	public bool 启用面剔除 { get; set; } = true;

	public override void _Ready()
    {
        Instance = this;
		Engine.MaxFps = 90;

		// 从场景文件加载SceneManager
		var sceneManagerScene = GD.Load<PackedScene>("res://Assets/Core/SceneManager.tscn");
		if(sceneManagerScene != null)
		{
			场景管理器 = sceneManagerScene.Instantiate<SceneManager>();
			AddChild(场景管理器);
		}
		else
		{
			GD.PrintErr("无法加载SceneManager场景文件");
			场景管理器 = new SceneManager();
			AddChild(场景管理器);
		}
    }
	
	public SceneManager GetSceneManager()
	{
		return 场景管理器;
	}
}
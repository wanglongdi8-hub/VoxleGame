using Godot;
using System;

public partial class G : Node
{
	public static G Instance { get; private set; }
    [Export]public SceneManager 场景管理器 {get; set;} = new SceneManager();
    public BlockAtlasConfig AtlasConfig { get; set; }
	public bool 启用面剔除 { get; set; } = true;

	public override void _Ready()
    {
        Instance = this;
		Engine.MaxFps = 90;

		AddChild(场景管理器);
    }
	
	public SceneManager GetSceneManager()
	{
		return 场景管理器;
	}
}

using Godot;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;


public partial class SceneManager : Node
{
    [Export] public Godot.Collections.Array<SceneConfig> SceneConfigList { get; set; } = [];
	private Dictionary<StringName, SceneConfig> _sceneConfigDict = [];

	public override void _Ready()
	{
		BuildSceneConfigDict();
	}

    private void BuildSceneConfigDict()
    {
        foreach (var config in SceneConfigList)
		{
			_sceneConfigDict[config.Id] = config;
		}
    }

    public void 切换场景(StringName SceneName)
	{
		if(!_sceneConfigDict.ContainsKey(SceneName))
		{
			GD.PrintErr($"场景切换失败：找不到场景配置 '{SceneName}'");
			GD.Print($"可用的场景ID：{string.Join(", ", _sceneConfigDict.Keys)}");
			return;
		}
		
		var sceneConfig = _sceneConfigDict[SceneName];
		if(sceneConfig.ScenePack == null)
		{
			GD.PrintErr($"场景切换失败：场景 '{SceneName}' 的PackedScene为null");
			return;
		}
		
		GD.Print($"切换场景到 '{SceneName}'，场景文件：{sceneConfig.ScenePack.ResourcePath}");
		GetTree().ChangeSceneToPacked(sceneConfig.ScenePack);
	}
}
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
		if(!_sceneConfigDict.ContainsKey(SceneName)) return;
		GetTree().ChangeSceneToPacked(_sceneConfigDict[SceneName].ScenePack);

		// 释放旧场景
	}
}

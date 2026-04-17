using Godot;
using Godot.Collections;
using System;

public partial class GUIViewManager : Node
{
	[Export] public Godot.Collections.Array<GUIViewConfig> ViewConfigList { get; set; } = [];
	[Export] public Node GuiRoot { get; set; } = null;
	private Dictionary<StringName, GUIViewConfig> _viewConfigDict = [];
	private int ViewInstanceCount = 0;
	private Dictionary<int, BaseGUIView> _viewInstanceDict = [];

	public override void _Ready()
	{
		BuildViewConfigDict();
	}

	public void BuildViewConfigDict()
	{
		foreach (var config in ViewConfigList)
		{
			_viewConfigDict[config.Id] = config;
		}
	}

	public GUIViewConfig GetViewConfig(StringName name)
	{
		if (_viewConfigDict.ContainsKey(name))
		{
			return _viewConfigDict[name];
		}
		else
		{
			GD.PrintErr($"GUIViewManager: No view config found for name {name}");
			return null;
		}
	}

	private int GenNewViewInstanceId()
	{
		return ViewInstanceCount++;
	}

	private BaseGUIView GetViewInstance(int viewID)
	{
		return _viewInstanceDict.ContainsKey(viewID) ? _viewInstanceDict[viewID] : null;
	}

	public int OpenView(StringName name)
	{
		var config = GetViewConfig(name);
		if (config == null)
		{
			GD.PrintErr($"GUIViewManager: Cannot open view '{name}' - config not found");
			return -1;
		}
		
		if (config.Prefab == null)
		{
			GD.PrintErr($"GUIViewManager: Cannot open view '{name}' - prefab is null");
			return -1;
		}
		
		var viewInstanceId = GenNewViewInstanceId();
		PackedScene prefab = config.Prefab;
		var view = prefab.Instantiate() as BaseGUIView;
		if (view == null)
		{
			GD.PrintErr($"GUIViewManager: Cannot open view '{name}' - instantiated object is not a BaseGUIView");
			return -1;
		}
		
		view.Config = config;
		view.viewinstanceId = viewInstanceId;
		_viewInstanceDict[viewInstanceId] = view;
		view.GUIManager = this;
		GuiRoot.AddChild(view);
		view.OpenView();
		return viewInstanceId;
	}

	public void CloseView(int viewId)
	{
		var view = GetViewInstance(viewId);
		view.CloseView();
		_viewInstanceDict.Remove(viewId);
		view.QueueFree();
	}
}
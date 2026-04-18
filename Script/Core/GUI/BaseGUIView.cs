using Godot;
using System;

public partial class BaseGUIView : Node
{
	public GUIViewManager GUIManager {get; set;}
	public GUIViewConfig Config { get; set; } = null;
	public int viewinstanceId { get; set; } = -1;

	protected virtual void Open()
	{
		
	}

	protected virtual void Close()
	{
		
	}

	public virtual void OpenView()
	{
		// 其他处理逻辑
		Open();
	}

	public virtual void CloseView()
	{
		// 其他处理逻辑
		Close();
	}

	protected void CloseSelf()
	{
		GUIManager.CloseView(viewinstanceId);
	}

}

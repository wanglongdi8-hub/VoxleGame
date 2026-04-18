using Godot;
using System;

public partial class NewGameMenu : BaseGUIView
{
	[Export] public LineEdit 世界种子 { get; set; }
	[Export] public LineEdit 世界名称 { get; set; }
	[Export] public Button 返回 { get; set; }
	[Export] public Button 创建新世界 { get; set; }

	public override void _Ready()
	{
		世界种子.Text = "12345678901234567890123456789012";
		世界名称.Text = "新世界";
	}

	public void _on_back_pressed()
	{
		CloseSelf();
	}

	public void _on_newgame_pressed()
	{
		G.Instance.GetSceneManager().切换场景("MainGame");
	}
}
using Godot;
using System;

public partial class StartMenut : BaseGUIView
{

	protected override void Open()
	{
		
	}

	protected override void Close()
	{
		
	}

	public void OnQuitButtonPressed()
	{
		GetTree().Quit();
	}

	public void _on_settings_button_pressed()
	{
		GUIManager.OpenView("SettingsMenu");
	}

	public void _on_xing_cun_dang_pressed()
	{
		GUIManager.OpenView("NewGameMenu");
	}
}

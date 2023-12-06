using Godot;
using System;

public partial class PauseMenu : MarginContainer
{
	private GUIManager GUI;

	public override void _Ready()
	{
		GUI = GetParent<GUIManager>();
	}

	#region Buttons
	private void _on_resume_button_down()
	{
		GUI.DisablePauseMenu();
    }

    private void _on_settings_button_down()
    {
        GUI.DisableGUI(GUI.PauseMenu);
        GUI.EnableGUI(GUI.SettingsMenu);
    }

    private void _on_back_button_down()
	{
		GameManager.Main.MultiplayerManager.LeaveGame();
		GameManager.Main.LoadMainMenu();
	}

	private void _on_quit_button_down()
	{
		GetTree().Quit();
	}
	#endregion
}

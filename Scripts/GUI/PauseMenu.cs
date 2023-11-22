using Godot;
using Kokkies;
using System;

public partial class PauseMenu : MarginContainer
{
	public override void _Ready()
	{
	}

	#region Buttons
	private void _on_resume_button_down()
	{
		GameManager.Main.GUI.DisablePauseMenu();
	}
	
	private void _on_back_button_down()
	{
		GameManager.Main.LoadMainMenu();
	}

	private void _on_quit_button_down()
	{
		GetTree().Quit();
	}
	#endregion
}

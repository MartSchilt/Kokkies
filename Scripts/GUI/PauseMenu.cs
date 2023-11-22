using Godot;
using System;

public partial class PauseMenu : MarginContainer
{
	private GUIManager GUI;

	public override void _Ready()
	{
		GUI = GetParent<GUIManager>();
	}

	public override void _Process(double delta)
	{
	}

	#region Buttons
	private void _on_resume_button_down()
	{
		GUI.DisablePauseMenu();
	}
	#endregion
}

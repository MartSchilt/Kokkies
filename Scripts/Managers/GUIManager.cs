using Godot;
using System;

public partial class GUIManager : CanvasLayer
{
	[Export]
	public MarginContainer PauseMenu;
	[Export]
	public ScoreboardGUI Scoreboard;
	[Export]
	public ShooterGUI ShooterGUI;

	private bool _isPaused = false;

	public override void _Ready()
	{
		Input.MouseMode = Input.MouseModeEnum.Visible;

		// Disable the other GUI elements
		DisablePauseMenu();
		DisableScoreboard();
		DisableShooterGUI();
	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_pause"))
		{
			if (_isPaused)
			{
				GD.Print("Unpause the game");
				_isPaused = false;
				DisablePauseMenu();
				Input.MouseMode = Input.MouseModeEnum.Captured;
			}
			else
			{
				GD.Print("Pause the game");
				_isPaused = true;
				EnablePauseMenu();
				Input.MouseMode = Input.MouseModeEnum.Visible; // Show the mouse
			}
		}
	}

	public void EnablePauseMenu()
	{
		PauseMenu.Show();
	}
	public void DisablePauseMenu()
	{
		PauseMenu.Hide();
	}

	public void EnableScoreboard()
	{
		Scoreboard.Show();
	}
	public void DisableScoreboard()
	{
		Scoreboard.Hide();
	}

	public void EnableShooterGUI()
	{
		ShooterGUI.Show();
	}
	public void DisableShooterGUI()
	{
		ShooterGUI.Hide();
	}
}

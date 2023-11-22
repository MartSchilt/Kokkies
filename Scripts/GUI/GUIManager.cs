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
		GD.Print($"Starting {nameof(GUIManager)}");
	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_pause"))
			if (_isPaused)
				DisablePauseMenu();
			else
				EnablePauseMenu();
	}

	#region GUI Elements
	public void DisableAll()
	{
		DisablePauseMenu();
		DisableScoreboard();
		DisableShooterGUI();
	}

	public void EnablePauseMenu()
	{
		_isPaused = true;
		PauseMenu.Show();
		Input.MouseMode = Input.MouseModeEnum.Visible; // Show the mouse
	}
	public void DisablePauseMenu()
	{
		_isPaused = false;
		PauseMenu.Hide();
		Input.MouseMode = Input.MouseModeEnum.Captured; // Hide the mouse
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
	#endregion
}

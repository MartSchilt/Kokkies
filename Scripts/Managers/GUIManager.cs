using Godot;
using System;

public partial class GUIManager : CanvasLayer
{
	[Export]
	public ScoreboardGUI Scoreboard;
	[Export]
	public ShooterGUI ShooterGUI;

	public override void _Ready()
	{
		DisableShooterGUI();
		DisableScoreboard();
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

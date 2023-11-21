using Godot;
using System;

public partial class GUIManager : CanvasLayer
{
	[Export]
	public OverlayManager ShooterGUI;

	public override void _Ready()
	{
		DisableShooterGUI();
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

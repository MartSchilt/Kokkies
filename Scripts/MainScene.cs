using Godot;
using System;

/// <summary>
/// This is the script for the 'main' node of our game
/// Here you can put properties to be used by all the other scenes
/// This node also loads in any new scenes that you want
/// </summary>
public partial class MainScene : Node
{
	public PackedScene MultiplayerMenu;

	public override void _Ready()
	{
		GD.Print("Starting game...");
		MultiplayerMenu = GD.Load<PackedScene>("res://Scenes/multiplayerMenu.tscn");
		
		// Load first scene
		AddChild(MultiplayerMenu.Instantiate());
	}
}

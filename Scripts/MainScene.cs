using Godot;
using System;

/// <summary>
/// This is the script for the 'main' node of our game
/// Here you can put properties to be used by all the other scenes
/// This node also loads in any new scenes that you want
/// </summary>
public partial class MainScene : Node
{
	[Export]
	public MusicManager MusicManager;
	[Export]
	public PackedScene MultiplayerMenu;

	public override void _Ready()
	{
		GD.Print("Starting game...");

		// Load first scene
		LoadScene(MultiplayerMenu);
	}

	public void LoadScene(string scenePath)
	{
		var scene = GD.Load<PackedScene>(scenePath);
		LoadScene(scene);
	}

	public void LoadScene(PackedScene scene)
	{
		var instance = scene.Instantiate();
		AddChild(instance);

		MusicManager.PlayMusic(instance.Name);
	}
}

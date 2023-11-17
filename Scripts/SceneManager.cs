using Godot;
using Kokkies;
using System;
using System.Linq;

public partial class SceneManager : Node3D
{
	[Export]
	public PackedScene PlayerScene;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		foreach(var player in GameManager.Players)
		{
			var currentPlayer = PlayerScene.Instantiate() as Node3D;
			currentPlayer.Name = player.Id.ToString();
			AddChild(currentPlayer);
			var spawn = GetTree().GetNodesInGroup("SpawnLocation")[player.SpawnLocation] as Node3D;
			currentPlayer.GlobalPosition = spawn.GlobalPosition;
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}

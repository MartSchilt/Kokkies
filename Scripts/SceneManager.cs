using Godot;
using Kokkies;

public partial class SceneManager : Node3D
{
	[Export]
	public PackedScene PlayerScene;

	public override void _Ready()
	{
		var index = 0;
		foreach(var player in GameManager.Players)
		{
			var currentPlayer = PlayerScene.Instantiate() as Node3D;
			currentPlayer.Name = player.Id.ToString();
			currentPlayer.SetMeta("PlayerId", player.Id);
			AddChild(currentPlayer);
			foreach (Node3D spawn in GetTree().GetNodesInGroup("SpawnLocation"))
				if (spawn.Name == index.ToString())
					currentPlayer.GlobalPosition = spawn.GlobalPosition;

			index++;
		}
	}

	public override void _Process(double delta)
	{
	}
}

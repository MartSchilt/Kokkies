using Godot;
using Kokkies;

public partial class SceneManager : Node3D
{
	public PackedScene PlayerScene;

	public override void _Ready()
	{
		PlayerScene = GD.Load<PackedScene>("res://Scenes/player.tscn");

		if (GameManager.Players.Count <= 0)
		{
			GameManager.Players.Add(new()
			{
				Id = 1,
				Name = "Kokkie",
				Color = new Color(),
				Score = 100,
			});
		}

		var index = 0;
		foreach (var player in GameManager.Players)
		{
			var currentPlayer = PlayerScene.Instantiate() as Node3D;
			currentPlayer.Name = player.Id.ToString();
			currentPlayer.SetMeta("PlayerId", player.Id);
			AddChild(currentPlayer);
			if (GetTree().GetNodesInGroup("SpawnLocation").Count < 1)
			{
				currentPlayer.GlobalPosition = new Vector3(10, 10, 10); // Spawn players above
			}
			else
			{
				foreach (Node3D spawn in GetTree().GetNodesInGroup("SpawnLocation"))
					if (spawn.Name == index.ToString())
						currentPlayer.GlobalPosition = spawn.GlobalPosition;
			}

			index++;
		}

	}
}

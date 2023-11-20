using Godot;
using Kokkies;
using System.Linq;

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
				Score = 0
			});
		}

		var index = 0;
		foreach (var player in GameManager.Players)
		{
			var currentPlayer = PlayerScene.Instantiate() as PlayerCharacter;
			currentPlayer.Name = player.Id.ToString();
			currentPlayer.SetMeta("SpawnLocation", index.ToString());
			AddChild(currentPlayer);
			Respawn(currentPlayer);
			index++;
		}
	}

	public void Respawn(PlayerCharacter playerCharacter)
	{
		if (GetTree().GetNodesInGroup("SpawnLocation").Count < 1)
		{
			playerCharacter.GlobalPosition = new Vector3(10, 10, 10); // Spawn players above
		}
		else
		{
			foreach (Node3D spawn in GetTree().GetNodesInGroup("SpawnLocation"))
				if (spawn.Name == playerCharacter.GetMeta("SpawnLocation").AsString())
				{
					playerCharacter.GlobalPosition = spawn.GlobalPosition;
					break;
				}

			playerCharacter.Player.Health = playerCharacter.MaxHealth;
			playerCharacter.Alive = true;
			playerCharacter.Rotation = new(0, 0, 0);
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
	public void AddPoints(int playerId, int points)
	{
		var playerCharacter = (PlayerCharacter)GetChildren().ToList()
			.Find(x => x.Name.Equals(playerId.ToString()));

		if (playerCharacter == null) return;

		playerCharacter.Player.Score += points;
		playerCharacter.OverlayManager.ScoreValue = playerCharacter.Player.Score;

		GD.Print($"{playerCharacter.Name} has earned {points}, " +
				 $"totaling to {playerCharacter.Player.Score} points!");


	}
}

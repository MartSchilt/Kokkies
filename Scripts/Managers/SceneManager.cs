using Godot;
using Kokkies;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class SceneManager : Node3D
{
	public GUIManager GUI;
	public PackedScene PlayerScene;

	private List<Node> _playerCharacters;

	public override void _Ready()
	{
		PlayerScene = GD.Load<PackedScene>("res://Scenes/player.tscn");
		_playerCharacters = new();

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
			_playerCharacters.Add(currentPlayer);
			AddChild(currentPlayer);
			Respawn(currentPlayer);
			
			index++;
		}

		try
		{
			GUI = GetParent().GetNode<GUIManager>("GUI");
			GUI.Scoreboard.Update(_playerCharacters);
		}
		catch (Exception e)
		{
			GD.PrintErr($"GUI not loaded, are you loading the level from the menu? If not, ignore this message: {e.Message}");
		}
	}

	public void ResetScene()
	{
		foreach(var player in _playerCharacters)
		{
			Respawn((PlayerCharacter)player);
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

			playerCharacter.Health = playerCharacter.MaxHealth;
			playerCharacter.Ammo = playerCharacter.MaxAmmo;
			playerCharacter.Alive = true;
			playerCharacter.Rotation = new(0, 0, 0);
			playerCharacter.NameLabel.Text = playerCharacter.Player.Name + "#" + playerCharacter.Player.Id + $"({playerCharacter.Health}/100)";
		}
	}

	// Don't know for sure if we need to call this locally or not...
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	public void AddPoints(int playerId, int points)
	{
		var playerCharacter = (PlayerCharacter)_playerCharacters
			.Find(x => x.Name.Equals(playerId.ToString()));

		if (playerCharacter == null) 
			return;

		playerCharacter.Player.Score += points;
		if (playerCharacter.Player.Score >= 100)
		{
			// Win the game
		}

		GUI.Scoreboard.Update(_playerCharacters);
		// Update ShooterGUI if this is the client's player
		if (playerCharacter.Name == Multiplayer.GetUniqueId().ToString())
			playerCharacter.OverlayManager.ScoreValue = playerCharacter.Player.Score;


		GD.Print($"{playerCharacter.Name} has earned {points}, " +
				 $"totaling to {playerCharacter.Player.Score} points!");
	}
}

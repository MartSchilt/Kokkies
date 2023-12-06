using Godot;
using System;
using System.Collections.Generic;

public partial class SceneManager : Node3D
{
	[Signal]
	public delegate void SceneLoadedEventHandler();

	[Export]
	public PackedScene PlayerScene;

	public List<Node> PlayerCharacters;

	public override void _Ready()
	{
		GD.Print($"Starting {nameof(SceneManager)}");

		PlayerCharacters = new();

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
			var currentPlayer = PlayerScene.Instantiate();
			currentPlayer.Name = player.Id.ToString();
			currentPlayer.SetMeta("SpawnLocation", index.ToString());
			PlayerCharacters.Add(currentPlayer);
			AddChild(currentPlayer);
			Respawn((BaseCharacter)currentPlayer);
			
			index++;
		}

		try
		{
			GameManager.Main.GUI.Scoreboard.Update(PlayerCharacters);
		}
		catch (Exception e)
		{
			GD.PrintErr($"GUI not loaded, are you loading the level from the menu? If not, ignore this message: {e.Message}");
		}

		EmitSignal(SignalName.SceneLoaded);
	}

	public void ResetScene()
	{
		foreach(var player in PlayerCharacters)
		{
			Respawn((BaseCharacter)player);
		}
	}

	public void Respawn(BaseCharacter playerCharacter)
	{
		if (GetTree().GetNodesInGroup("SpawnLocation").Count < 1)
		{
			playerCharacter.GlobalPosition = new Vector3(10, 10, 10); // Spawn players above
			playerCharacter.Rotation = new(0, 0, 0);
		}
		else
		{
			foreach (Node3D spawn in GetTree().GetNodesInGroup("SpawnLocation"))
				if (spawn.Name == playerCharacter.GetMeta("SpawnLocation").AsString())
				{
					playerCharacter.GlobalPosition = spawn.GlobalPosition;
					playerCharacter.Rotation = spawn.Rotation;
					break;
				}
		}

		playerCharacter.Health = playerCharacter.MaxHealth;
		playerCharacter.Alive = true;
		playerCharacter.NameLabel.Text = playerCharacter.Player.Name + "#" + playerCharacter.Player.Id + $"({playerCharacter.Health}/100)";
	}

	// Don't know for sure if we need to call this locally or not...
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	public void AddPoints(int playerId, int points)
	{
		var playerCharacter = (BaseCharacter)PlayerCharacters
			.Find(x => x.Name.Equals(playerId.ToString()));

		if (playerCharacter == null) 
			return;

		playerCharacter.Player.Score += points;
		if (playerCharacter.Player.Score >= 100)
		{
			// Win the game
		}

		GameManager.Main.GUI.Scoreboard.Update(PlayerCharacters);
		
		// Update ShooterGUI if this is the client's player
		//if (playerCharacter.Name == Multiplayer.GetUniqueId().ToString())
		//		playerCharacter.OverlayManager.ScoreValue = playerCharacter.Player.Score;

		GD.Print($"{playerCharacter.Name} has earned {points}, " +
				 $"totaling to {playerCharacter.Player.Score} points!");
	}

	public Node GetPlayerAudio(int playerId)
	{
		var playerCharacter = (BaseCharacter)PlayerCharacters
			.Find(x => x.Name.Equals(playerId.ToString()));

		return playerCharacter.AudioPlayer;
	}
}

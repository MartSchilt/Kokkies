using Godot;
using System.Collections.Generic;

public partial class ScoreboardGUI : MarginContainer
{
	private VBoxContainer _vBox;

	public override void _Ready()
	{
		_vBox = GetNode<VBoxContainer>(nameof(VBoxContainer)).GetNode<VBoxContainer>(nameof(VBoxContainer));
	}

	public void Update(List<Node> playerCharacters)
	{
		foreach (var child in _vBox.GetChildren())
			_vBox.RemoveChild(child);

		foreach (var playerCharacter in playerCharacters)
			AddPlayer((PlayerCharacter)playerCharacter);
	}

	private void AddPlayer(PlayerCharacter playerCharacter)
	{
		HBoxContainer row = new();
		_vBox.AddChild(row);
		
		var player = playerCharacter.Player;

		Label playerName = new();
		playerName.Text = player.Name + "#" + player.Id;
		row.AddChild(playerName);

		Label playerScore = new();
		playerScore.Text = player.Score.ToString();
		row.AddChild(playerScore);
	}
}

using Godot;
using System;
using System.Collections.Generic;

namespace Kokkies;

public class Player
{
	public long Id { get; set; }
	public string Name { get; set; }
	public int Score { get; set; }
}

public partial class GameManager : Node
{
	[Export]
    public static int MaxPlayers = 10;
    public static List<Player> Players;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Players = new List<Player>();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}

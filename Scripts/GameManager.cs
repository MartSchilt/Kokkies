using Godot;
using System.Collections.Generic;

namespace Kokkies;

public class Player
{
	public long Id { get; set; }
	public string Name { get; set; }
    public Color Color { get; set; }
    public int Score { get; set; }
}

public partial class GameManager : Node
{
    public static int MaxPlayers = 10;
    public static List<Player> Players;
	public static string Address;

	public override void _Ready()
	{
		Players = new List<Player>();
	}
}
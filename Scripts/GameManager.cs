using Godot;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Kokkies;

/// <summary>
/// Manages the active "Game" that is started by the client
/// Gets implemented in the Project Autoload.
/// It therefore acts similar to a Singleton, without requiring us to keep track of the instance
/// </summary>
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

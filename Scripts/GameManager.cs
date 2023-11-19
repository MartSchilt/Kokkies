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
    public const int STANDARD_PORT = 8910;

    private const string STANDARD_NAME = "Kokkie";
    private const string STANDARD_IP = "127.0.0.1";

    public static int MaxPlayers = 10;
    public static List<Player> Players;
	public static string Address;
    public static string PlayerName { get; private set; }
    public static string IpAddress { get; private set; }
    public static int Port { get; set; }

    public override void _Ready()
	{
		Players = new List<Player>();

        PlayerName = STANDARD_NAME;
        IpAddress = STANDARD_IP;
        Port = STANDARD_PORT;
    }

    public static void SetPlayerName(string value)
    {
        if (value != "")
            PlayerName = value;
        else
            PlayerName = STANDARD_NAME;
    }

    public static void SetIpAddress(string value)
    {
        if (value != "")
            IpAddress = value;
        else
            IpAddress = STANDARD_IP;
    }
}

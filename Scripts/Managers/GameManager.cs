using Godot;
using System.Collections.ObjectModel;

namespace Kokkies;

/// <summary>
/// Manages the active "Game" that is started by the client
/// Gets implemented in the Project Autoload.
/// It therefore acts similar to a Singleton, without requiring us to keep track of the instance
/// </summary>
public partial class GameManager : Node
{
    #region Constants
    public const int STANDARD_PORT = 8910;

    private const string STANDARD_NAME = "Kokkie";
    private const string STANDARD_IP = "127.0.0.1";
    #endregion

    #region Properties
    public static string Address { get; set; }
    public static string PlayerName { get; private set; }
    public static string IpAddress { get; private set; }
    public static int Port { get; set; }
    public static MainScene Main { get; set; }
    #endregion

    #region Fields
    public static int MaxPlayers = 10;
    public static ObservableCollection<Player> Players;

    private static int _musicBusIndex;
    #endregion

    public override void _Ready()
    {
        GD.Print($"Starting {nameof(GameManager)}");

        Main = GetParent().GetNode<MainScene>("Main");
        Players = new ObservableCollection<Player>();

        PlayerName = STANDARD_NAME;
        IpAddress = STANDARD_IP;
        Port = STANDARD_PORT;

        _musicBusIndex = AudioServer.GetBusIndex("Music");
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

    public static void MusicVolume(float value)
    {
        AudioServer.SetBusVolumeDb(_musicBusIndex, value);
    }
}

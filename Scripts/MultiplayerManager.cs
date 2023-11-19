using Godot;
using Kokkies;
using System;

public partial class MultiplayerManager : Node
{
    public Node Main;
    public VoiceOrchestrator VOrchestrator;

    private ENetMultiplayerPeer peer;
    private string playerName;
    private string ipAddress;
    private int Port;

    public override void _Ready()
    {
        // Get the Main Node (if it is not there, just use self as Main Node)
        Main = GetParent() ?? this;

        // Multiplayer stuff
        Multiplayer.PeerConnected += PlayerConnected;
        Multiplayer.PeerDisconnected += PlayerDisconnected;
        Multiplayer.ConnectedToServer += ConnectedToServer;
        Multiplayer.ConnectionFailed += ConnectionFailed;
        Multiplayer.ServerDisconnected += DisconnectedFromServer;
        peer = new();

        // Initializing
        VOrchestrator = new();
        AddChild(VOrchestrator);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void StartMultiplayerScene(string scenePath)
    {
        var scene = GD.Load<PackedScene>(scenePath);
        var instance = scene.Instantiate();
        Main.AddChild(instance);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    public void SendPlayerInfo(long id, string name, Color color)
    {
        // Only add the player if it is not already added, otherwise ignore this
        if (GameManager.Players.Find(p => p.Id == id) == null)
        {
            Player player = new()
            {
                Id = id,
                Name = name,
                Score = 0,
                Color = color
            };
            GameManager.Players.Add(player);
            var fullName = name + "#" + id;
            GD.Print("Player Connected: " + fullName);
        }

        // Send every player info to all the clients
        // We do this to make sure that the player that connects later also gets info on the players that connected earlier
        if (Multiplayer.IsServer())
            for (int i = 0; i < GameManager.Players.Count; i++)
                Rpc(nameof(SendPlayerInfo), GameManager.Players[i].Id, GameManager.Players[i].Name, GameManager.Players[i].Color);
    }

    // Is basically a void method, but returns an Error object, just in case something goes wrong
    public Error HostGame()
    {
        var error = peer.CreateServer(Port, GameManager.MaxPlayers);
        if (error != Error.Ok)
        {
            GD.PrintErr("Failed to host: " + error);
            return error;
        }

        peer.Host.Compress(ENetConnection.CompressionMode.RangeCoder);
        Multiplayer.MultiplayerPeer = peer;
        GD.Print("Hosting started on " + Port + "...");
        return error;
    }

    public Error JoinGame(string ipAddress, int port)
    {
        var error = peer.CreateClient(ipAddress, port);
        if (error != Error.Ok)
        {
            GD.PrintErr("Failed to create client: " + error);
            return error;
        }

        peer.Host.Compress(ENetConnection.CompressionMode.RangeCoder);
        Multiplayer.MultiplayerPeer = peer;
        VOrchestrator.Recording = true; // Make sure it records the microphone
        return error;
    }

    private Color RandomColor()
    {
        Random rnd = new Random();
        byte[] b = new byte[3];
        rnd.NextBytes(b);
        return Color.Color8(b[0], b[1], b[2]);
    }

    #region Peer-to-peer methods
    private void PlayerConnected(long id)
    {
        // Do nothing... For now?
    }

    private void PlayerDisconnected(long id)
    {
        GD.Print("Player Disconnected: " + id);
        GameManager.Players.Remove(GameManager.Players.Find(p => p.Id == id));
    }

    private void ConnectedToServer()
    {
        GD.Print("Connected To Server");
        var id = Multiplayer.GetUniqueId();
        Rpc(nameof(SendPlayerInfo), id, playerName, RandomColor());
    }

    private void DisconnectedFromServer()
    {
        GD.Print("Disconnected From Server");
        GameManager.Players.Clear();
    }

    private void ConnectionFailed()
    {
        GD.PrintErr("Connection Failed!");
    }
    #endregion
}
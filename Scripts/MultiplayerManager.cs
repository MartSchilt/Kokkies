using Godot;
using Kokkies;
using System;

public partial class MultiplayerManager : Node
{
    public Node Main;
    public VoiceOrchestrator VOrchestrator;

    private ENetMultiplayerPeer peer;

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

        // VOIP
        VOrchestrator = new();
        Main.AddChild(VOrchestrator);
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
        var error = peer.CreateServer(GameManager.Port, GameManager.MaxPlayers);
        if (error != Error.Ok)
        {
            GD.PrintErr("Failed to host: " + error);
            return error;
        }

        peer.Host.Compress(ENetConnection.CompressionMode.RangeCoder);
        Multiplayer.MultiplayerPeer = peer;
        GD.Print("Hosting started on " + GameManager.Port + "...");
        SendPlayerInfo(Multiplayer.GetUniqueId(), GameManager.PlayerName, Helper.RandomColor());
        VOrchestrator.Recording = true; // Make sure it records the microphone
        return error;
    }

    public Error JoinGame()
    {
        var error = peer.CreateClient(GameManager.IpAddress, GameManager.Port);
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
        Rpc(nameof(SendPlayerInfo), id, GameManager.PlayerName, Helper.RandomColor());
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

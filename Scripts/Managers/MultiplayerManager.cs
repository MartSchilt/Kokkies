using Godot;
using Kokkies;
using System.Linq;

public partial class MultiplayerManager : Node
{
    public Node Main;
    public VoiceOrchestrator VOrchestrator;
    public RichTextLabel Logger;
    public bool ShouldLogVoice { get; set; }

    private ENetMultiplayerPeer peer;

    public override void _Ready()
    {
        // Get the Main Node (if it is not there, just use self as Main Node)
        Main = GetParent() ?? this;

        Logger = Main.GetNode<RichTextLabel>("MultiplayerMenu/MarginContainer/HBoxContainer/VBoxContainer/HBoxContainer/VBoxContainer3/Log");

        // Multiplayer stuff
        Multiplayer.PeerConnected += PlayerConnected;
        Multiplayer.PeerDisconnected += PlayerDisconnected;
        Multiplayer.ConnectedToServer += ConnectedToServer;
        Multiplayer.ConnectionFailed += ConnectionFailed;
        Multiplayer.ServerDisconnected += DisconnectedFromServer;
        peer = new();

        // VOIP
        ShouldLogVoice = false;
        VOrchestrator = new();
        VOrchestrator.Name = nameof(VoiceOrchestrator);
        Main.AddChild(VOrchestrator);

        VOrchestrator.sentVoiceData += (data) =>
        {
            LogVoice(true, data);
        };
        VOrchestrator.receivedVoiceData += (data, id) =>
        {
            LogVoice(false, data, id);
        };
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
        if (GameManager.Players.ToList().Find(p => p.Id == id) == null)
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
        VOrchestrator.Recording = true;
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
        VOrchestrator.Recording = true;
        return error;
    }

    private void LogVoice(bool sent, float[] data, int id = -1)
    {
        if (ShouldLogVoice)
        {
            if (sent)
                Logger.AddText("\n Sent data of size " + data.Length);
            else
                Logger.AddText("\n Received data of size " + data.Length + " from " + id.ToString());
        }
    }

    #region Peer-to-peer methods
    private void PlayerConnected(long id)
    {
        // Do nothing... For now?
    }

    private void PlayerDisconnected(long id)
    {
        GD.Print("Player Disconnected: " + id);
        GameManager.Players.Remove(GameManager.Players.ToList().Find(p => p.Id == id));
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

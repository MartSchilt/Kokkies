using Godot;
using System.Linq;

public partial class MultiplayerManager : Node
{
    public VoiceOrchestrator VOrchestrator;
    public bool ShouldLogVoice = false;

    private ENetMultiplayerPeer peer;

    public override void _Ready()
    {
        GD.Print($"Starting {nameof(MultiplayerManager)}");

        // Multiplayer stuff
        Multiplayer.PeerConnected += PlayerConnected;
        Multiplayer.PeerDisconnected += PlayerDisconnected;
        Multiplayer.ConnectedToServer += ConnectedToServer;
        Multiplayer.ConnectionFailed += ConnectionFailed;
        Multiplayer.ServerDisconnected += DisconnectedFromServer;

        // VOIP
        VOrchestrator = GameManager.Main.VoiceOrchestrator;
        VOrchestrator.sentVoiceData += (data) => LogVoice(true, data);
        VOrchestrator.receivedVoiceData += (data, id) => LogVoice(false, data, id);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void StartMultiplayerScene(string scenePath)
    {
        GameManager.Main.LoadScene(scenePath);
        VOrchestrator.StartInstances();
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
                Color = color,
            };
            GameManager.Players.Add(player);
            var fullName = name + "#" + id;
            GD.Print($"Player Connected: {fullName}");
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
        peer = new();
        var error = peer.CreateServer(GameManager.Port, GameManager.MaxPlayers);
        if (error != Error.Ok)
        {
            GD.PrintErr($"Failed to host: {error}");
            return error;
        }

        peer.Host.Compress(ENetConnection.CompressionMode.RangeCoder);
        Multiplayer.MultiplayerPeer = peer;
        GD.Print($"Hosting started on {GameManager.Port}...");
        SendPlayerInfo(Multiplayer.GetUniqueId(), GameManager.PlayerName, Helper.RandomColor());
        return error;
    }

    public Error JoinGame()
    {
        peer = new();
        var error = peer.CreateClient(GameManager.IpAddress, GameManager.Port);
        if (error != Error.Ok)
        {
            GD.PrintErr($"Failed to create client: {error}");
            return error;
        }

        peer.Host.Compress(ENetConnection.CompressionMode.RangeCoder);
        Multiplayer.MultiplayerPeer = peer;
        return error;
    }

    public void LeaveGame()
    {
        GD.Print("Leaving multiplayer room...");

        GameManager.Players.Clear();

        if (peer == null)
            return;

        // Stop hosting
        if (Multiplayer.GetUniqueId() == 1)
        {
            // Disconnect all other peers first and send them a disconnect message
            // Would be better to migrate the host to a different client, but this is probably a lot of work
            foreach (var peerIndex in Multiplayer.GetPeers())
                if (peerIndex != 1)
                    peer.DisconnectPeer(peerIndex);

            // Destroy the connection
            peer.Host.Destroy();

            // Remove your own hosting
            Multiplayer.MultiplayerPeer = null;
        }
        // Or disconnect from the host
        else
            peer.DisconnectPeer(1);
    }

    private void LogVoice(bool sent, float[] data, int id = -1)
    {
        if (ShouldLogVoice)
        {
            if (sent)
                GD.Print($"Sent data of size {data.Length}");
            else
                GD.Print($"Received data of size {data.Length} from {id}");
        }
    }

    #region Peer-to-peer methods
    private void PlayerConnected(long id)
    {
        // Do nothing... For now?
    }

    private void PlayerDisconnected(long id)
    {
        GD.Print($"Player Disconnected: {id}");
        // Remove the player from the GameManager
        GameManager.Players.Remove(GameManager.Players.Where(p => p.Id == id).First<Player>());
        // Remove the BaseCharacter from the scene
        var players = GetTree().GetNodesInGroup("Player");
        foreach (var player in players)
            if (player.Name == id.ToString())
                player.QueueFree();

        // Update the scoreboard so it doesn't include the player that just left
        //GameManager.Main.GUI.Scoreboard.Update();
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
        Multiplayer.MultiplayerPeer = null;
        GameManager.Players.Clear();
        // Go back to the main menu for now
        // Would be better to migrate the host to a different client, but this is probably a lot of work
        GameManager.Main.LoadMainMenu();
    }

    private void ConnectionFailed()
    {
        GD.PrintErr("Connection Failed!");
        Multiplayer.MultiplayerPeer = null;
    }
    #endregion
}

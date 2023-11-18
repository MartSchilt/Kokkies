using Godot;
using Kokkies;

public partial class MultiplayerController : Control
{
	[Export]
	public string Address = "127.0.0.1";
	[Export]
	public int Port = 8910;
   
	private ENetMultiplayerPeer peer;
	private int index = 0;

	public override void _Ready()
	{
		Multiplayer.PeerConnected += PlayerConnected;
		Multiplayer.PeerDisconnected += PlayerDisconnected;
		Multiplayer.ConnectedToServer += ConnectedToServer;
		Multiplayer.ConnectionFailed += ConnectionFailed;

		peer = new();
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	public void StartGame()
	{
		var scene = GD.Load<PackedScene>("res://Scenes/dev.tscn");
		var instance = scene.Instantiate();
		AddChild(instance);
		Hide();
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
	public void SendPlayerInfo(long id, string name)
	{
		Player player = new() { Id = id, Name = name, Score = 0, SpawnLocation = index};
		index++;
		GameManager.Players.Add(player);
		GD.Print("Player Connected: " + id + " - " + name);

		if (Multiplayer.IsServer())
		{
			for (int i = 0; i < GameManager.Players.Count; i++)
				Rpc(nameof(SendPlayerInfo), GameManager.Players[i].Id, GameManager.Players[i].Name);
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
	}

	private void ConnectedToServer()
	{
		GD.Print("Connected To Server");
		var id = Multiplayer.GetUniqueId();
		Rpc(nameof(SendPlayerInfo), id, "Test" + id);
	}

	private void ConnectionFailed()
	{
		GD.Print("Connection Failed!");
	}
	#endregion
	#region Button Presses
	private void _on_host_button_down()
	{
		var error = peer.CreateServer(Port, GameManager.MaxPlayers);
		if (error != Error.Ok)
		{
			GD.Print("Failed to host: " + error);
			return;
		}

		peer.Host.Compress(ENetConnection.CompressionMode.RangeCoder);
		Multiplayer.MultiplayerPeer = peer;
		GD.Print("Hosting started...");
		SendPlayerInfo(Multiplayer.GetUniqueId(), "Host");
	}

	private void _on_join_button_down()
	{
		var error = peer.CreateClient(Address, Port);
		if (error != Error.Ok)
		{
			GD.Print("Failed to create client: " + error);
			return;
		}

		peer.Host.Compress(ENetConnection.CompressionMode.RangeCoder);
		Multiplayer.MultiplayerPeer = peer;
	}

	private void _on_start_button_down()
	{
		Rpc(nameof(StartGame));
	}
	#endregion
}

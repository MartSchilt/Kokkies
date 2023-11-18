using Godot;
using Kokkies;
using System;
using System.Linq;

public partial class MultiplayerController : Control
{
	[Export]
	public int Port = 8910;
   
	private ENetMultiplayerPeer peer;
	private string playerName = string.Empty;
	private string ipAddress = string.Empty;

	public override void _Ready()
	{
		Multiplayer.PeerConnected += PlayerConnected;
		Multiplayer.PeerDisconnected += PlayerDisconnected;
		Multiplayer.ConnectedToServer += ConnectedToServer;
		Multiplayer.ConnectionFailed += ConnectionFailed;

		peer = new();

		if (OS.GetCmdlineArgs().Contains("--server"))
		{
			GD.Print("Started as server");
			HostGame();
		}
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
	public void SendPlayerInfo(long id, string name, Color color)
	{
		Player player = new() { 
			Id = id, 
			Name = name, 
			Score = 0, 
			Color = color
		};
		GameManager.Players.Add(player);
		GD.Print("Player Connected: " + name + "#" + id);

		if (Multiplayer.IsServer())
		{
			for (int i = 0; i < GameManager.Players.Count; i++)
				Rpc(nameof(SendPlayerInfo), GameManager.Players[i].Id, GameManager.Players[i].Name, GameManager.Players[i].Color);
		}
	}

	public void HostGame()
	{
		var error = peer.CreateServer(Port, GameManager.MaxPlayers);
		if (error != Error.Ok)
		{
			GD.PrintErr("Failed to host: " + error);
			return;
		}

		peer.Host.Compress(ENetConnection.CompressionMode.RangeCoder);
		Multiplayer.MultiplayerPeer = peer;
		GD.Print("Hosting started...");
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
	}

	private void ConnectedToServer()
	{
		GD.Print("Connected To Server");
		var id = Multiplayer.GetUniqueId();
		Rpc(nameof(SendPlayerInfo), id, playerName, RandomColor());
	}

	private void ConnectionFailed()
	{
		GD.PrintErr("Connection Failed!");
	}
	#endregion
	#region Button Presses
	private void _on_host_button_down()
	{
		HostGame();
		SendPlayerInfo(Multiplayer.GetUniqueId(), playerName, RandomColor());
	}

	private void _on_join_button_down()
	{
		if (ipAddress == "") // For faster debug
			ipAddress= "127.0.0.1";

		var error = peer.CreateClient(ipAddress, Port);
		if (error != Error.Ok)
		{
			GD.PrintErr("Failed to create client: " + error);
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
	#region Text Inputs
	private void _on_name_input_text_changed(string new_text)
	{
		playerName = new_text;
	}
	private void _on_ip_input_text_changed(string new_text)
	{
		ipAddress = new_text;
	}
	#endregion
}
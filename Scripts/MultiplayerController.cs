using Godot;
using Kokkies;
using System;
using System.Linq;

public partial class MultiplayerController : Control
{
	public Node Main;
	public VoiceOrchestrator voiceOrchestrator;
	public SpinBox spinBoxInputThreshold;
	public HSlider sliderInputThreshold;
	public Button startMPButton;
	public Label statusLabel;
	public RichTextLabel playerList;

	private const string STATUS = "Status: ";
	private const string STANDARD_NAME = "Kokkie";
	private const string STANDARD_IP = "127.0.0.1";
	private const int STANDARD_PORT = 8910;

	private ENetMultiplayerPeer peer;
	private string playerName = STANDARD_NAME;
	private string ipAddress = STANDARD_IP;
	private int Port = STANDARD_PORT;

	public override void _Ready()
	{
		// Get the Main Node (if it is not there, just use self as Main Node)
		Main = GetParent() ?? this;

		// UI Elements
		// I really hate how we need to declare the nodepath like this...
		spinBoxInputThreshold = GetNode<SpinBox>("MarginContainer/HBoxContainer/VBoxContainer/HBoxContainer/VBoxContainer/HBoxContainer5/Value");
		sliderInputThreshold = GetNode<HSlider>("MarginContainer/HBoxContainer/VBoxContainer/HBoxContainer/VBoxContainer/InputThreshold");
		startMPButton = GetNode<Button>("MarginContainer/HBoxContainer/VBoxContainer/HBoxContainer/VBoxContainer2/MultiplayerTest");
		statusLabel = GetNode<Label>("MarginContainer/HBoxContainer/VBoxContainer/HBoxContainer/VBoxContainer/Status");
		playerList = GetNode<RichTextLabel>("MarginContainer/HBoxContainer/VBoxContainer/HBoxContainer/VBoxContainer/Players");

		// Multiplayer stuff
		Multiplayer.PeerConnected += PlayerConnected;
		Multiplayer.PeerDisconnected += PlayerDisconnected;
		Multiplayer.ConnectedToServer += ConnectedToServer;
		Multiplayer.ConnectionFailed += ConnectionFailed;
		Multiplayer.ServerDisconnected += DisconnectedFromServer;
		peer = new();

		// Change the way the game works when it is started as a server
		if (OS.GetCmdlineArgs().Contains("--server"))
		{
			GD.Print("Started as server");
			HostGame();
		}
		else
		{
			GD.Print("Started as client");

			// Initializing
			voiceOrchestrator = new();
			AddChild(voiceOrchestrator);

			spinBoxInputThreshold.Value = voiceOrchestrator.InputThreshold;
			sliderInputThreshold.Value = voiceOrchestrator.InputThreshold;
			statusLabel.Text = STATUS + "Ready to host or join";
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	public void StartGame()
	{
		var scene = GD.Load<PackedScene>("res://Scenes/dev.tscn");
		var instance = scene.Instantiate();
		GetParent().AddChild(instance);
		Hide();
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
            UpdateUI();
        }

		// Send every player info to all the clients
		// We do this to make sure that the player that connects later also gets info on the players that connected earlier
		if (Multiplayer.IsServer())
		{
			for (int i = 0; i < GameManager.Players.Count; i++)
				Rpc(nameof(SendPlayerInfo), GameManager.Players[i].Id, GameManager.Players[i].Name, GameManager.Players[i].Color);
		}
	}

	// Is basically a void method, but returns an Error object, just in case something goes wrong
	public Error HostGame()
	{
		statusLabel.Text = STATUS + "Starting server...";
		var error = peer.CreateServer(Port, GameManager.MaxPlayers);
		if (error != Error.Ok)
		{
			GD.PrintErr("Failed to host: " + error);
			statusLabel.Text = STATUS + "Failed to host!";
			return error;
		}

		peer.Host.Compress(ENetConnection.CompressionMode.RangeCoder);
		Multiplayer.MultiplayerPeer = peer;
		startMPButton.Disabled = false;
		GD.Print("Hosting started on " + Port + "...");
		statusLabel.Text = STATUS + "Hosting on " + Port;
		return error;
	}

	public void UpdateUI()
	{
		playerList.Text = "Players: ";
		foreach(var player in GameManager.Players)
		{
			var playerName = player.Name + "#" + player.Id;
            playerList.Text += "\n - " + playerName;
		}
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
        UpdateUI();
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
	}

	private void ConnectionFailed()
	{
		GD.PrintErr("Connection Failed!");
	}
	#endregion
	
	#region Button Presses
	private void _on_host_button_down()
	{
		if (HostGame() == Error.Ok)
		{
			SendPlayerInfo(Multiplayer.GetUniqueId(), playerName, RandomColor());
			voiceOrchestrator.Recording = true; // Make sure it records the microphone
		}
	}

	private void _on_join_button_down()
	{
		statusLabel.Text = STATUS + "Connecting...";
		var error = peer.CreateClient(ipAddress, Port);
		if (error != Error.Ok)
		{
			GD.PrintErr("Failed to create client: " + error);
			statusLabel.Text = STATUS + "Failed to connect!";
			return;
		}

		peer.Host.Compress(ENetConnection.CompressionMode.RangeCoder);
		Multiplayer.MultiplayerPeer = peer;
		statusLabel.Text = STATUS + "Connected!";
		voiceOrchestrator.Recording = true; // Make sure it records the microphone
	}
	private void _on_multiplayer_test_button_down()
	{
		Rpc(nameof(StartGame));
	}
	#endregion
	
	#region Text Inputs
	private void _on_name_input_text_changed(string new_text)
	{
		if (new_text != "")
			playerName = new_text;
		else
			playerName = STANDARD_NAME;
	}

	private void _on_ip_input_text_changed(string new_text)
	{
		if (new_text != "")
			ipAddress = new_text;
		else
			ipAddress = STANDARD_IP;
	}

	private void _on_port_input_text_changed(string new_text)
	{
		if (new_text != "")
		{
			var parsed = int.TryParse(new_text, out Port);
			if (parsed)
				return;
		}
		Port = STANDARD_PORT;
	}
	#endregion

	private void _on_input_thresh_value_changed(float value)
	{
		voiceOrchestrator.InputThreshold = value;
		spinBoxInputThreshold.Value = value;
	}

	private void _on_listen_toggled(bool button_pressed)
	{
		voiceOrchestrator.Listen = button_pressed;
	}
}

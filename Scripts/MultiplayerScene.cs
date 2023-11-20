using Godot;
using Kokkies;
using System;
using System.Linq;

public partial class MultiplayerScene : Control
{
	private const string STATUS = "Status: ";
	private const float STANDARD_INPUT_THRESHOLD = 0.005f;
	
	public Node Main;
	public MultiplayerManager MPManager;

	public RichTextLabel Logger;
	public SpinBox spinBoxInputThreshold;
	public HSlider sliderInputThreshold;
	public Button startMPButton;
	public Label statusLabel;
	public RichTextLabel playerList;

	private ENetMultiplayerPeer peer;

	public override void _Ready()
	{
		// Get the Main Node (if it is not there, just use self as Main Node)
		Main = GetParent() ?? this;

		// Instantiate the Multiplayer Client
		MPManager = new();
		MPManager.Name = nameof(MultiplayerManager);
		Main.AddChild(MPManager);

		// UI Elements
		// I really hate how we need to declare the nodepath like this...
		Logger = GetNode<RichTextLabel>("MarginContainer/HBoxContainer/VBoxContainer/HBoxContainer/VBoxContainer3/Log");
		spinBoxInputThreshold = GetNode<SpinBox>("MarginContainer/HBoxContainer/VBoxContainer/HBoxContainer/VBoxContainer/HBoxContainer5/Value");
		sliderInputThreshold = GetNode<HSlider>("MarginContainer/HBoxContainer/VBoxContainer/HBoxContainer/VBoxContainer/InputThreshold");
		startMPButton = GetNode<Button>("MarginContainer/HBoxContainer/VBoxContainer/HBoxContainer/VBoxContainer2/MultiplayerTest");
		statusLabel = GetNode<Label>("MarginContainer/HBoxContainer/VBoxContainer/HBoxContainer/VBoxContainer/Status");
		playerList = GetNode<RichTextLabel>("MarginContainer/HBoxContainer/VBoxContainer/HBoxContainer/VBoxContainer/Players");

		GameManager.Players.CollectionChanged += (s, e) => UpdateUI();

		// Change the way the game works when it is started as a server
		if (OS.GetCmdlineArgs().Contains("--server"))
		{
			GD.Print("Started as server");
			MPManager.HostGame();
		}
		else
		{
			GD.Print("Started as client");
			spinBoxInputThreshold.Value = STANDARD_INPUT_THRESHOLD;
			sliderInputThreshold.Value = STANDARD_INPUT_THRESHOLD;

			Log("Ready to host or join");
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	public void StartGame()
	{
		MPManager.StartMultiplayerScene("res://Scenes/dev.tscn");
		Hide();
	}

	public void UpdateUI()
	{
		playerList.Text = "Players: ";
		foreach (var player in GameManager.Players)
		{
			var playerName = player.Name + "#" + player.Id;
			playerList.Text += "\n - " + playerName;
		}
	}

	private void Log(string text)
	{
		statusLabel.Text = STATUS + text;
		Logger.AddText("\n" + text);
	}

	#region Button Presses
	private void _on_host_button_down()
	{
		Log("Starting server...");

		if (MPManager.HostGame() == Error.Ok)
		{
			Log("Hosting on " + GameManager.Port);
			startMPButton.Disabled = false;
		}
		else
			Log("Failed to host!");
	}

	private void _on_join_button_down()
	{
		Log("Connecting...");

		if (MPManager.JoinGame() == Error.Ok)
			Log("Connected!");
		else
			Log("Failed to connect!");
	}

	private void _on_multiplayer_test_button_down()
	{
		Rpc(nameof(StartGame));
	}
	#endregion

	#region Text Inputs
	private void _on_name_input_text_changed(string new_text)
	{
		GameManager.SetPlayerName(new_text);
	}

	private void _on_ip_input_text_changed(string new_text)
	{
		GameManager.SetIpAddress(new_text);
	}

	private void _on_port_input_text_changed(string new_text)
	{
		int port;
		if (new_text != "")
		{
			var parsed = int.TryParse(new_text, out port);
			if (parsed)
			{
				GameManager.Port = port;
				return;
			}
		}
		GameManager.Port = GameManager.STANDARD_PORT;
	}
	#endregion

	private void _on_input_thresh_value_changed(float value)
	{
		MPManager.VOrchestrator.InputThreshold = value;
		spinBoxInputThreshold.Value = value;
	}

	private void _on_listen_toggled(bool button_pressed)
	{
		MPManager.VOrchestrator.Listen = button_pressed;
	}
}

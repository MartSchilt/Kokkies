using Godot;
using Kokkies;
using System;
using System.Linq;

public partial class MultiplayerScene : Control
{
    public Node Main;
    public MultiplayerManager MPManager;

    public SpinBox spinBoxInputThreshold;
    public HSlider sliderInputThreshold;
    public Button startMPButton;
    public Label statusLabel;
    public RichTextLabel playerList;

    private const string STATUS = "Status: ";
    private const string STANDARD_NAME = "Kokkie";
    private const string STANDARD_IP = "127.0.0.1";
    private const int STANDARD_PORT = 8910;
    private const float STANDARD_INPUT_THRESHOLD = 0.005f;

    private ENetMultiplayerPeer peer;
    private string playerName = STANDARD_NAME;
    private string ipAddress = STANDARD_IP;
    private int port = STANDARD_PORT;

    public override void _Ready()
    {
        // Get the Main Node (if it is not there, just use self as Main Node)
        Main = GetParent() ?? this;

        // Instantiate the Multiplayer Client
        MPManager = new();
        Main.AddChild(MPManager);

        // UI Elements
        // I really hate how we need to declare the nodepath like this...
        spinBoxInputThreshold = GetNode<SpinBox>("MarginContainer/HBoxContainer/VBoxContainer/HBoxContainer/VBoxContainer/HBoxContainer5/Value");
        sliderInputThreshold = GetNode<HSlider>("MarginContainer/HBoxContainer/VBoxContainer/HBoxContainer/VBoxContainer/InputThreshold");
        startMPButton = GetNode<Button>("MarginContainer/HBoxContainer/VBoxContainer/HBoxContainer/VBoxContainer2/MultiplayerTest");
        statusLabel = GetNode<Label>("MarginContainer/HBoxContainer/VBoxContainer/HBoxContainer/VBoxContainer/Status");
        playerList = GetNode<RichTextLabel>("MarginContainer/HBoxContainer/VBoxContainer/HBoxContainer/VBoxContainer/Players");

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
            statusLabel.Text = STATUS + "Ready to host or join";
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void StartGame()
    {
        MPManager.StartMultiplayerScene("res://Scenes/dev.tscn");
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

    private Color RandomColor()
    {
        Random rnd = new Random();
        byte[] b = new byte[3];
        rnd.NextBytes(b);
        return Color.Color8(b[0], b[1], b[2]);
    }

    #region Button Presses
    private void _on_host_button_down()
    {
        statusLabel.Text = STATUS + "Starting server...";

        if (MPManager.HostGame() == Error.Ok)
        {
            statusLabel.Text = STATUS + "Hosting on " + port;
            MPManager.SendPlayerInfo(Multiplayer.GetUniqueId(), playerName, RandomColor());
            MPManager.VOrchestrator.Recording = true; // Make sure it records the microphone
        }
        else
        {
            statusLabel.Text = STATUS + "Failed to host!";
        }
    }

    private void _on_join_button_down()
    {
        statusLabel.Text = STATUS + "Connecting...";

        if (MPManager.JoinGame(ipAddress, port) == Error.Ok)
        {
            statusLabel.Text = STATUS + "Connected!";
        }
        else
        {
            statusLabel.Text = STATUS + "Failed to connect!";
        }
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
            var parsed = int.TryParse(new_text, out port);
            if (parsed)
                return;
        }
        port = STANDARD_PORT;
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

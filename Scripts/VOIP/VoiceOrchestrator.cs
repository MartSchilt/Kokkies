using Godot;
using System.Collections.Generic;

public enum TypeVoiceInstance
{
    NATIVE,
    GDSCRIPT,
    CSHARP
}

public partial class VoiceOrchestrator : Node
{
    [Signal]
    public delegate void ReceivedVoiceDataEventHandler(float[] data, int id);
    [Signal]
    public delegate void SentVoiceDataEventHandler(float[] data);
    [Signal]
    public delegate void CreatedInstanceEventHandler();
    [Signal]
    public delegate void RemovedInstanceEventHandler();

    #region Properties
    private bool _listen;
    [Export]
    public bool Listen
    {
        get => _listen;
        set
        {
            if (_id != null)
            {
                var instance = _instances.Find(i => i.Name == _id.ToString());
                instance.Listen = value;
            }

            _listen = value;
        }
    }

    private bool _recording;
    [Export]
    public bool Recording
    {
        get => _recording;
        set
        {
            if (_id != null)
            {
                var instance = _instances.Find(i => i.Name == _id.ToString());
                instance.Recording = value;
            }

            _recording = value;
        }
    }
    
    private float _inputThreshold;
    [Export]
    public float InputThreshold
    {
        get => _inputThreshold;
        set
        {
            if (_id != null)
            {
                var instance = _instances.Find(i => i.Name == _id.ToString());
                instance.InputThreshold = value;
            }

            _inputThreshold = value;
        }
    }
    #endregion

    [Export]
    public TypeVoiceInstance TVI = TypeVoiceInstance.CSHARP;

    private List<VoiceInstance> _instances;
    private int? _id = null;

    public override void _Ready()
    {
        GD.Print($"Starting {nameof(VoiceOrchestrator)}");
        _instances = new List<VoiceInstance>();

        Multiplayer.ConnectedToServer += ConnectedOK;
        Multiplayer.ServerDisconnected += ServerDisconnected;
        Multiplayer.ConnectionFailed += ServerDisconnected;
        Multiplayer.PeerConnected += PlayerConnected;
        Multiplayer.PeerDisconnected += PlayerDisconnected;

        Listen = false;
        Recording = true;
        InputThreshold = 0.005f;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Multiplayer.HasMultiplayerPeer() && Multiplayer.GetPeers().Length > 0 && Multiplayer.IsServer() && _id == null)
            CreateInstance(Multiplayer.GetUniqueId());

        if (!Multiplayer.HasMultiplayerPeer() && _id == 1)
            Reset();
    }

    public void CreateInstance(long id)
    {
        GD.Print("Creating voice instance: " + id + " <- " + Multiplayer.GetUniqueId());
        VoiceInstance instance = new();

        // Used to be a check for different voice instance types
        // NATIVE or GDSCRIPT
        // But right now we are only using our own C# code

        if (id == Multiplayer.GetUniqueId())
        {
            instance.Recording = Recording;
            instance.Listen = Listen;
            instance.InputThreshold = InputThreshold;

            instance.SentVoiceData += SentVoiceData;

            _id = (int)id;
        }

        instance.ReceivedVoiceData += ReceivedVoiceData;
        instance.Name = id.ToString();

        _instances.Add(instance);
        AddChild(instance);
        EmitSignal(SignalName.CreatedInstance);
    }

    public void RemoveInstance(long id)
    {
        var instance = _instances.Find(i => i.Name == id.ToString());

        if (id == _id)
            _id = null;

        if (instance != null)
            _instances.Remove(instance);

        EmitSignal(SignalName.RemovedInstance, id);
    }

    public void Reset()
    {
        for (int i = 0; i < _instances.Count; i++)
            RemoveInstance(i);
    }

    public void StartInstances()
    {
        foreach (var instance in _instances)
            instance.Start = true;
    }

    private void ConnectedOK()
    {
        if ((Multiplayer.HasMultiplayerPeer() || Multiplayer.IsServer()) && _id == 1)
            Reset();

        CreateInstance(Multiplayer.GetUniqueId());
    }

    private void ServerDisconnected()
    {
        Reset();
    }

    private void PlayerConnected(long id)
    {
        CreateInstance(id);
    }

    private void PlayerDisconnected(long id)
    {
        RemoveInstance(id);
    }

    private void ReceivedVoiceData(float[] data, int id)
    {
        EmitSignal(SignalName.ReceivedVoiceData, data, id);
    }

    private void SentVoiceData(float[] data)
    {
        EmitSignal(SignalName.SentVoiceData, data);
    }
}

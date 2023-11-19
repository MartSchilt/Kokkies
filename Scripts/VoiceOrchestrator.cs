using Godot;
using System.Collections.Generic;

namespace Kokkies;

public enum TypeVoiceInstance
{
    NATIVE,
    GDSCRIPT
}

public partial class VoiceOrchestrator : Node
{
    [Signal]
    public delegate void receivedVoiceDataEventHandler(float[] data, int id);
    [Signal]
    public delegate void sentVoiceDataEventHandler(float[] data);
    [Signal]
    public delegate void createdInstanceEventHandler();
    [Signal]
    public delegate void removedInstanceEventHandler();

    [Export]
    public bool Listen
    {
        get => _listen;
        set
        {
            if (ID != null)
                instances[ID ?? 0].Listen = value;

            _listen = value;
        }
    }
    [Export]
    public bool Recording
    {
        get => _recording;
        set
        {
            if (ID != null)
                instances[ID ?? 0].Recording = value;

            _recording = value;
        }
    }
    [Export]
    public float InputThreshold
    {
        get => _inputThreshold;
        set
        {
            if (ID != null)
                instances[ID ?? 0].InputThreshold = value;

            _inputThreshold = value;
        }
    }
    [Export]
    public TypeVoiceInstance TVI;

    private bool _listen;
    private bool _recording;
    private float _inputThreshold;
    private List<VoiceInstance> instances;
    private int? ID;

    public override void _Ready()
    {
        instances = new List<VoiceInstance>();

        Multiplayer.ConnectedToServer += ConnectedOK;
        Multiplayer.ServerDisconnected += ServerDisconnected;
        Multiplayer.ConnectionFailed += ServerDisconnected;

        Multiplayer.PeerConnected += PlayerConnected;
        Multiplayer.PeerDisconnected += PlayerDisconnected;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Multiplayer.HasMultiplayerPeer() && Multiplayer.IsServer() && ID == null)
            CreateInstance(Multiplayer.GetUniqueId());

        if ((!Multiplayer.HasMultiplayerPeer() || !Multiplayer.IsServer()) && ID == 1)
            Reset();
    }

    public void CreateInstance(long id)
    {
        VoiceInstance instance = new();
        // Used to be a check for different voice instance types?

        if (id == Multiplayer.GetUniqueId())
        {
            instance.Recording = Recording;
            instance.Listen = Listen;
            instance.InputThreshold = InputThreshold;

            sentVoiceData += SentVoiceData;

            ID = (int)id;
        }

        receivedVoiceData += ReceivedVoiceData;

        instance.Name = id.ToString();
        instances[(int)id] = instance;
        AddChild(instance);
        EmitSignal(SignalName.createdInstance);
    }
        
    public void RemoveInstance(long id)
    {
        var instance = instances[(int)id];

        if (id == ID)
            ID = null;

        instances.Remove(instance);

        EmitSignal(SignalName.removedInstance, id);
    }

    public void Reset()
    {
        for (int i = 0; i < instances.Count; i++)
            RemoveInstance(i);
    }

    private void ConnectedOK()
    {
        if ((Multiplayer.HasMultiplayerPeer() || Multiplayer.IsServer()) && ID == 1)
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
        EmitSignal(SignalName.receivedVoiceData, data, id);
    }

    private void SentVoiceData(float[] data)
    {
        EmitSignal(SignalName.sentVoiceData, data);
    }
}

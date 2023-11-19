using Godot;
using System;
using System.Collections.Generic;

namespace Kokkies;

public enum TypeVoiceInstance
{
    NATIVE,
    GDSCRIPT,
    CSHARP
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
            {
                var instance = instances.Find(i => i.Name == ID.ToString());
                instance.Listen = value;
            }

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
            {
                var instance = instances.Find(i => i.Name == ID.ToString());
                instance.Recording = value;
            }

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
            {
                var instance = instances.Find(i => i.Name == ID.ToString());
                instance.InputThreshold = value;
            }

            _inputThreshold = value;
        }
    }
    [Export]
    public TypeVoiceInstance TVI = TypeVoiceInstance.CSHARP;

    private bool _listen;
    private bool _recording;
    private float _inputThreshold;
    private List<VoiceInstance> instances;
    private int? ID = null;

    public override void _Ready()
    {
        instances = new List<VoiceInstance>();

        Multiplayer.ConnectedToServer += ConnectedOK;
        Multiplayer.ServerDisconnected += ServerDisconnected;
        Multiplayer.ConnectionFailed += ServerDisconnected;
        Multiplayer.PeerConnected += PlayerConnected;
        Multiplayer.PeerDisconnected += PlayerDisconnected;

        Listen = false;
        Recording = false;
        InputThreshold = 0.005f;
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
        GD.Print("Creating instance: " + id + " - " + Multiplayer.GetUniqueId());
        VoiceInstance instance = new();
        // Used to be a check for different voice instance types?

        if (id == Multiplayer.GetUniqueId())
        {
            instance.Recording = Recording;
            instance.Listen = Listen;
            instance.InputThreshold = InputThreshold;

            instance.sentVoiceData += SentVoiceData;

            ID = (int)id;
        }

        instance.receivedVoiceData += ReceivedVoiceData;

        instance.Name = id.ToString();
        instances.Add(instance);
        AddChild(instance);
        EmitSignal(SignalName.createdInstance);
    }
        
    public void RemoveInstance(long id)
    {
        var _instance = instances.Find(i => i.Name == id.ToString());

        if (id == ID)
            ID = null;

        if (_instance != null)
            instances.Remove(_instance);

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

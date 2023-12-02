using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class VoiceInstance : Node3D
{
    [Signal]
    public delegate void ReceivedVoiceDataEventHandler(float[] data, int id);
    [Signal]
    public delegate void SentVoiceDataEventHandler(float[] data);

    [Export]
    public NodePath CustomAudio;

    public bool Start = false;
    public bool Listen;
    public bool Recording;
    public float InputThreshold;
    public bool ProximityChat = true;

    private const int MAX_SAMPLES = 10;

    private int _recordIndex;
    private bool _prevFrameRecording = false;
    private Queue<float> _receiveBuffer = new Queue<float>();
    private AudioStreamWav _audioWav;
    private Node _voice;
    private AudioEffectCapture _effectCapture;
    private AudioStreamGeneratorPlayback _playback;
    private VoiceMic _mic;

    public override void _Process(double delta)
    {
        if (_playback != null)
            ProcessVoice();

        ProcessMic();
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    public void Speak(float[] sampleData, int id)
    {
        if (Start)
        {
            if (_playback == null)
                CreateVoice(id);

            EmitSignal(SignalName.ReceivedVoiceData, sampleData, id);

            foreach (var item in sampleData)
                _receiveBuffer.Enqueue(item);
        }
    }

    private void CreateVoice(int playerId)
    {
        if (ProximityChat)
        {
            var audioStreamPlayer = GameManager.Main.GetCurrentScene().GetPlayerAudio(playerId);
            switch (audioStreamPlayer)
            {
                case AudioStreamPlayer:
                    _voice = audioStreamPlayer as AudioStreamPlayer;
                    break;
                case AudioStreamPlayer2D:
                    _voice = audioStreamPlayer as AudioStreamPlayer2D;
                    break;
                case AudioStreamPlayer3D:
                    _voice = audioStreamPlayer as AudioStreamPlayer3D;
                    break;
                default:
                    GD.PrintErr("Custom Audio Player is not an AudioStreamPlayer!");
                    break;
            }
        }
        else
        {
            _voice = new AudioStreamPlayer();
            _voice.Name = "VoiceStream";
            AddChild(_voice);
        }

        AudioStreamGenerator generator = new();
        generator.BufferLength = 0.1f;

        // Sadly the AudioStreams do not have a generic interface...
        switch (_voice)
        {
            case AudioStreamPlayer:
                var voice1D = _voice as AudioStreamPlayer;
                voice1D.Stream = generator;
                voice1D.Play();
                _playback = voice1D.GetStreamPlayback() as AudioStreamGeneratorPlayback;
                break;
            case AudioStreamPlayer2D:
                var voice2D = _voice as AudioStreamPlayer2D;
                voice2D.Stream = generator;
                voice2D.Play();
                _playback = voice2D.GetStreamPlayback() as AudioStreamGeneratorPlayback;
                break;
            case AudioStreamPlayer3D:
                var voice3D = _voice as AudioStreamPlayer3D;
                voice3D.Stream = generator;
                voice3D.Play();
                _playback = voice3D.GetStreamPlayback() as AudioStreamGeneratorPlayback;
                break;
            default:
                GD.PrintErr("No Voice so no playback generated");
                break;
        }
    }

    private void ProcessVoice()
    {
        if (_playback.GetFramesAvailable() < 1)
            return;

        if (_receiveBuffer.Count > 1)
            for (int i = 0; i < Math.Min(_playback.GetFramesAvailable(), _receiveBuffer.Count); i++)
                _playback.PushFrame(new(_receiveBuffer.Dequeue(), _receiveBuffer.Peek()));
    }

    private void CreateMic()
    {
        _mic = new();
        _mic.Name = nameof(VoiceMic);
        AddChild(_mic);
        var recordBusIndex = AudioServer.GetBusIndex(_mic.Bus);
        _effectCapture = (AudioEffectCapture)AudioServer.GetBusEffect(recordBusIndex, 0);
    }

    private void ProcessMic()
    {
        if (Recording)
        {
            if (_effectCapture == null)
                CreateMic();

            if (_prevFrameRecording == false)
                _effectCapture.ClearBuffer();

            Vector2[] stereoData = _effectCapture.GetBuffer(_effectCapture.GetFramesAvailable());
            if (stereoData.Length > 0)
            {
                float[] data = new float[stereoData.Length];
                float maxValue = 0.0f;

                for (int i = 0; i < stereoData.Length; i++)
                {
                    float value = (stereoData[i].X + stereoData[i].Y) / 2.0f;
                    maxValue = Math.Max(value, maxValue);
                    data[i] = value;
                }

                if (maxValue < InputThreshold)
                    return;

                if (Listen)
                    Speak(data, Multiplayer.GetUniqueId());

                Rpc(nameof(Speak), data, Multiplayer.GetUniqueId()); // should be network unique id?
                EmitSignal(SignalName.SentVoiceData, data);
            }
        }

        _prevFrameRecording = Recording;
    }

    private float GetPeakVolume()
    {
        List<float> samples = new();
        float sample = AudioServer.GetBusPeakVolumeLeftDb(_recordIndex, 0);
        samples.Add(sample);
        if (samples.Count > MAX_SAMPLES)
            samples.Remove(samples.First());

        return samples.Average();
    }
}

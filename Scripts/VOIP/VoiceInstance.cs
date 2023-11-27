using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class VoiceInstance : Node3D
{
    [Signal]
    public delegate void receivedVoiceDataEventHandler(float[] data, int id);
    [Signal]
    public delegate void sentVoiceDataEventHandler(float[] data);

    [Export]
    public NodePath customAudio;

    public bool Start = false;
    public bool Listen;
    public bool Recording;
    public float InputThreshold;
    public bool ProximityChat = true;

    private const int MAX_SAMPLES = 10;

    private int recordIndex;
    private bool prevFrameRecording = false;
    private Queue<float> receiveBuffer = new Queue<float>();
    private AudioStreamWav audioWav;
    private Node voice;
    private AudioEffectCapture effectCapture;
    private AudioStreamGeneratorPlayback playback;
    private VoiceMic Mic;

    public override void _Process(double delta)
    {
        if (playback != null)
            ProcessVoice();

        ProcessMic();
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    public void Speak(float[] sampleData, int id)
    {
        if (Start)
        {
            if (playback == null)
                CreateVoice(id);

            EmitSignal(SignalName.receivedVoiceData, sampleData, id);

            foreach (var item in sampleData)
                receiveBuffer.Enqueue(item);
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
                    voice = audioStreamPlayer as AudioStreamPlayer;
                    break;
                case AudioStreamPlayer2D:
                    voice = audioStreamPlayer as AudioStreamPlayer2D;
                    break;
                case AudioStreamPlayer3D:
                    voice = audioStreamPlayer as AudioStreamPlayer3D;
                    break;
                default:
                    GD.PrintErr("Custom Audio Player is not an AudioStreamPlayer!");
                    break;
            }
        }
        else
        {
            voice = new AudioStreamPlayer();
            voice.Name = "VoiceStream";
            AddChild(voice);
        }

        AudioStreamGenerator generator = new();
        generator.BufferLength = 0.1f;

        // Sadly the AudioStreams do not have a generic interface...
        switch (voice)
        {
            case AudioStreamPlayer:
                var voice1D = voice as AudioStreamPlayer;
                voice1D.Stream = generator;
                voice1D.Play();
                playback = voice1D.GetStreamPlayback() as AudioStreamGeneratorPlayback;
                break;
            case AudioStreamPlayer2D:
                var voice2D = voice as AudioStreamPlayer2D;
                voice2D.Stream = generator;
                voice2D.Play();
                playback = voice2D.GetStreamPlayback() as AudioStreamGeneratorPlayback;
                break;
            case AudioStreamPlayer3D:
                var voice3D = voice as AudioStreamPlayer3D;
                voice3D.Stream = generator;
                voice3D.Play();
                playback = voice3D.GetStreamPlayback() as AudioStreamGeneratorPlayback;
                break;
            default:
                GD.PrintErr("No Voice so no playback generated");
                break;
        }
    }

    private void ProcessVoice()
    {
        if (playback.GetFramesAvailable() < 1)
            return;

        if (receiveBuffer.Count > 1)
            for (int i = 0; i < Math.Min(playback.GetFramesAvailable(), receiveBuffer.Count); i++)
                playback.PushFrame(new(receiveBuffer.Dequeue(), receiveBuffer.Peek()));
    }

    private void CreateMic()
    {
        Mic = new();
        Mic.Name = nameof(VoiceMic);
        AddChild(Mic);
        var recordBusIndex = AudioServer.GetBusIndex(Mic.Bus);
        effectCapture = (AudioEffectCapture)AudioServer.GetBusEffect(recordBusIndex, 0);
    }

    private void ProcessMic()
    {
        if (Recording)
        {
            if (effectCapture == null)
                CreateMic();

            if (prevFrameRecording == false)
                effectCapture.ClearBuffer();

            Vector2[] stereoData = effectCapture.GetBuffer(effectCapture.GetFramesAvailable());
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
                EmitSignal(SignalName.sentVoiceData, data);
            }
        }

        prevFrameRecording = Recording;
    }

    private float GetPeakVolume()
    {
        List<float> samples = new();
        float sample = AudioServer.GetBusPeakVolumeLeftDb(recordIndex, 0);
        samples.Add(sample);
        if (samples.Count > MAX_SAMPLES)
            samples.Remove(samples.First());

        return samples.Average();
    }
}

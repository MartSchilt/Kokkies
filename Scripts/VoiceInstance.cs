using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Kokkies;

public partial class VoiceInstance : Node3D
{
    [Signal]
    public delegate void receivedVoiceDataEventHandler(float[] data, int id);
    [Signal]
    public delegate void sentVoiceDataEventHandler(float[] data);

    [Export]
    public NodePath customAudio;

    public bool Listen;
    public bool Recording;
    public float InputThreshold;

    private const int MAX_SAMPLES = 10;

    private int recordIndex;
    private bool prevFrameRecording = false;
    private Queue<float> receiveBuffer = new Queue<float>();
    private AudioStreamWav audioWav;
    private AudioStreamPlayer voice;
    private AudioEffectCapture effectCapture;
    private AudioStreamGeneratorPlayback playback;
    private VoiceMic Mic;

    public override void _Ready()
    {
        GD.Print("Voice Instance Ready!");
    }

    public override void _Process(double delta)
    {
        if (playback != null)
            ProcessVoice();

        ProcessMic();
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    public void Speak(float[] sampleData, int id)
    {
        if (playback == null)
            CreateVoice();

        EmitSignal(SignalName.receivedVoiceData, sampleData, id);

        foreach (var item in sampleData)
            receiveBuffer.Enqueue(item);
    }

    private void CreateVoice()
    {
        if (customAudio != null && !customAudio.IsEmpty)
        {
            var audioStreamPlayer = GetNode(customAudio);
            if (audioStreamPlayer is AudioStreamPlayer || audioStreamPlayer is AudioStreamPlayer2D || audioStreamPlayer is AudioStreamPlayer3D)
                voice = audioStreamPlayer as AudioStreamPlayer;
            else
                GD.PrintErr("Custom Audio Player is not an AudioStreamPlayer!");
        }
        else
        {
            voice = new();
            AddChild(voice);
        }

        AudioStreamGenerator generator = new();
        generator.BufferLength = 0.1f;
        voice.Stream = generator;
        voice.Play();

        playback = voice.GetStreamPlayback() as AudioStreamGeneratorPlayback;
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

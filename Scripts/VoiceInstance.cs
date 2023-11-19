using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Kokkies;

public partial class VoiceInstance : Node3D
{
    [Export]
    public NodePath customAudio;

    public bool Listen = true;
    public bool Recording = false;
    public float InputThreshold = 0.005f;

    private const int MAX_SAMPLES = 10;

    private int recordIndex;
    private bool prevFrameRecording = false;
    private Array<float> receiveBuffer = new Array<float>();
    private AudioStreamWav audioWav;
    private AudioStreamPlayer voice;
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
        GD.Print("Playback");
        if (playback == null)
            CreateVoice();

        EmitSignal("receivedVoiceData", sampleData, id);

        receiveBuffer.AddRange(sampleData);
    }

    private void CreateVoice()
    {
        if (customAudio != null)
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

        playback = voice.GetStreamPlayback() as AudioStreamGeneratorPlayback;
        voice.Play();
    }

    private void ProcessVoice()
    {
        if (playback.GetFramesAvailable() < 1)
            return;

        for (int i = 0; i < Math.Min(playback.GetFramesAvailable(), receiveBuffer.Count); i++)
        {
            playback.PushFrame(new(receiveBuffer[0], receiveBuffer[1]));
            receiveBuffer.RemoveAt(0);
        }
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

                if (listen)
                    Speak(data, Multiplayer.GetUniqueId());

                Rpc(nameof(Speak), data, Multiplayer.GetUniqueId()); // should be network unique id?
                EmitSignal("sentVoiceData", data);
            }
        }

        prevFrameRecording = recording;
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

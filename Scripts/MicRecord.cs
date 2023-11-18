using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class MicRecord : Node3D
{
    private const int MAX_SAMPLES = 10;

    private AudioEffectRecord audioRecord;
    private AudioEffectCapture audioCapture;
    private AudioStreamWav audioWav;
    private int recordIndex;
    private bool listen;

    public override void _Ready()
    {
        // We get the index of the "Record" bus.
        recordIndex = AudioServer.GetBusIndex("Record");
        // And use it to retrieve its first effect, which has been defined
        // as an "AudioEffectRecord" resource.
        audioRecord = (AudioEffectRecord)AudioServer.GetBusEffect(recordIndex, 0);
        audioCapture = (AudioEffectCapture)AudioServer.GetBusEffect(recordIndex, 1);
    }

    public override void _Process(double delta)
    {
        //GD.Print("DB: " + GetPeakVolume());
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey && @event.IsActionPressed("push_to_talk"))
        {
            OnCapturePressed();
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void Speak()
    {

    }

    private void OnCapturePressed()
    {
        Vector2[] stereoData = audioCapture.GetBuffer(audioCapture.GetFramesAvailable());
        if (stereoData.Length > 0)
        {
            double[] data = new double[stereoData.Length];
            // resize?

            double maxValue = 0.0;
            for (int i = 0; i < stereoData.Length; i++)
            {
                double value = (stereoData[i].X + stereoData[i].Y) / 2.0;
                maxValue = Math.Max(value, maxValue);
                data[i] = value;
            }
            if (maxValue < 0.005) // threshold
                return;
            if (listen)
                Speak();

            Rpc(nameof(Speak), data, Multiplayer.GetUniqueId()); // should be network unique id?
        }
    }

    private void OnRecordPressed()
    {
        if (audioRecord.IsRecordingActive())
        {
            audioWav = audioRecord.GetRecording();
            audioRecord.SetRecordingActive(false);
            GD.Print("Recording saved!");

            OnPlayRecording(audioWav);
        }
        else
        {
            audioRecord.SetRecordingActive(true);
            GD.Print("Recording...");
        }
    }

    private void OnPlayRecording(AudioStreamWav asw)
    {
        GD.Print("Playing back recording...");
        var audioStreamPlayer = GetNode<AudioStreamPlayer3D>("AudioStreamPlayer3D");
        audioStreamPlayer.Stream = asw;
        audioStreamPlayer.Play();
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

using Godot;

namespace Kokkies;

public partial class VoiceMic : AudioStreamPlayer
{
    private string name = "VoiceMicRecorder";

    public override void _Ready()
    {
        var currentNumber = 0;
        while (AudioServer.GetBusIndex(name + currentNumber.ToString()) != -1)
        {
            currentNumber++;
        }

        var busName = name + currentNumber.ToString();
        var index = AudioServer.BusCount;

        AudioServer.AddBus(index);
        AudioServer.SetBusName(index, busName);
        AudioServer.AddBusEffect(index, new AudioEffectCapture());
        AudioServer.SetBusMute(index, true);

        Bus = busName;
        Stream = new AudioStreamMicrophone();
        Play();
    }
}

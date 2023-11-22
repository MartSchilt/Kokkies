using Godot;
using System;

public partial class MusicManager : Node
{
	[Export]
	public AudioStreamPlayer[] musicStreams;

    public override void _Ready()
    {
        GD.Print($"Starting {nameof(MusicManager)}");
    }

    public void PlayMusic(string sceneName)
	{
		foreach (var music in musicStreams)
			if (music.Name == sceneName)
				music.Play();
			else
				music.Stop();
	}
}

using Godot;
using Kokkies;
using System;

public partial class CloudManager : Node3D
{
    [Export]
    public int Size;
    [Export]
    public int NumberOfClouds;
    [Export]
    public int Scatter;

    private const string CLOUD_PATH = "res://Assets/Objects/cloud.obj";
    private const int MIN_HEIGHT = 20;
    private const int MAX_HEIGHT = 60;

    private MeshInstance3D cloudMesh;

	public override void _Ready()
    {
        GD.Print($"Starting {nameof(CloudManager)}");

        Random rnd = new Random();

        // Remove children, just in case someone decided to manually add clouds
        foreach (var child in GetChildren())
            RemoveChild(child);

        for (int i = 0; i < NumberOfClouds; i++)
        {
            cloudMesh = new();
            cloudMesh.Mesh = GD.Load<Mesh>(CLOUD_PATH);
            var x = rnd.Next(0, Size / Scatter);
            var z = rnd.Next(0, Size / Scatter);
            var y = rnd.Next(MIN_HEIGHT, MAX_HEIGHT);
            cloudMesh.Position = new Vector3(x * Scatter, y, z * Scatter);
            AddChild(cloudMesh);
        }
    }

	public override void _Process(double delta)
	{
	}
}

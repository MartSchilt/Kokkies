using Godot;
using System;

public partial class TileManager : Node3D
{
    [Export]
	public float Width;
	[Export]
	public float Height;

	private const float TILE_SIZE = 10;
	private PackedScene FloorTile;
	private PackedScene MountainWall;

    public override void _Ready()
    {
        FloorTile = GD.Load<PackedScene>("res://Scenes/floorTile.tscn");
        MountainWall = GD.Load<PackedScene>("res://Scenes/mountainWall.tscn");

        // Remove children, just in case someone decided to manually add a floor
        foreach (var child in GetChildren())
            RemoveChild(child);

		for(float x = 0; x < Width * TILE_SIZE; x += TILE_SIZE)
		{
            for (float y = 0; y < Height * TILE_SIZE; y += TILE_SIZE)
            {
                MeshInstance3D tile = (MeshInstance3D)FloorTile.Instantiate();
                tile.Position = new Vector3(x, 0, y);
                tile.Name = "Floor" + x + "_" + y;
				AddChild(tile);
            }
        }
    }
}

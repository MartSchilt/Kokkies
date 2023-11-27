using Godot;
using System;

public partial class TileManager : Node3D
{
	[Export]
	public StandardMaterial3D FloorMaterial;
	[Export]
	public PackedScene WallTile;
	[Export]
	public float Width;
	[Export]
	public float Height;

	private const float FLOOR_SIZE = 10;
	private const float WALL_SIZE = 5;

	public PackedScene FloorTile;

	public override void _Ready()
    {
        GD.Print($"Starting {nameof(TileManager)}");
        
		FloorTile = GD.Load<PackedScene>("res://Scenes/floorTile.tscn");

		// Remove children, just in case someone decided to manually added floor or walls
		foreach (var child in GetChildren())
			RemoveChild(child);

		var maxWidth = Width * FLOOR_SIZE;
		var maxHeight = Height * FLOOR_SIZE;
		var halfFloorSize = FLOOR_SIZE / 2;
		var numberOfWallsOnTile = FLOOR_SIZE / WALL_SIZE;
		var halfWallSize = WALL_SIZE / 2;

		for (float x = 0; x < maxWidth; x += FLOOR_SIZE)
		{
			for (float y = 0; y < maxHeight; y += FLOOR_SIZE)
			{
				MeshInstance3D floor = (MeshInstance3D)FloorTile.Instantiate();
				floor.MaterialOverride = FloorMaterial;
				floor.Position = new Vector3(x, 0, y);
				floor.Name = "Floor" + x + "_" + y;
				AddChild(floor);

				if (x == 0 || x == maxWidth - FLOOR_SIZE)
				{
					for (float z = 0; z < numberOfWallsOnTile; z++)
					{
						MeshInstance3D wall = (MeshInstance3D)WallTile.Instantiate();
						if (x == 0)
							wall.Position = new Vector3(x - halfFloorSize, 0, y - halfWallSize + z * halfWallSize * 2);
						else if (x == maxHeight - FLOOR_SIZE)
							wall.Position = new Vector3(x + halfFloorSize, 0, y - halfWallSize + z * halfWallSize * 2);
						wall.RotateY(Mathf.DegToRad(90));
						wall.Name = "Wall" + x + "_" + y + "#" + z;
						AddChild(wall);
					}
				}
				if (y == 0 || y == maxHeight - FLOOR_SIZE)
				{
					for (float z = 0; z < numberOfWallsOnTile; z++)
					{
						MeshInstance3D wall = (MeshInstance3D)WallTile.Instantiate();
						if (y == 0)
							wall.Position = new Vector3(x - halfWallSize + z * halfWallSize * 2, 0, y - halfFloorSize);
						else if (y == maxHeight - FLOOR_SIZE)
							wall.Position = new Vector3(x - halfWallSize + z * halfWallSize * 2, 0, y + halfFloorSize);
						wall.Name = "Wall" + x + "_" + y + "#" + z;
						AddChild(wall);

					}
				}
			}
		}
	}
}

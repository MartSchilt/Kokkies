using Godot;

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
    [Export]
    public float FloorSize = 10;
    [Export]
    public float WallSize = 5;
    [Export]
    public bool GenerateWalls = true;

    public PackedScene FloorTile;

    public override void _Ready()
    {
        GD.Print($"Starting {nameof(TileManager)}");

        FloorTile = GD.Load<PackedScene>("res://Scenes/floorTile.tscn");

        // Remove children, just in case someone decided to manually added floor or walls
        foreach (var child in GetChildren())
            RemoveChild(child);

        var maxWidth = Width * FloorSize;
        var maxHeight = Height * FloorSize;
        var halfFloorSize = FloorSize / 2;
        var numberOfWallsOnTile = FloorSize / WallSize;
        var halfWallSize = WallSize / 2;

        for (float x = 0; x < maxWidth; x += FloorSize)
        {
            for (float y = 0; y < maxHeight; y += FloorSize)
            {
                MeshInstance3D floor = (MeshInstance3D)FloorTile.Instantiate();
                floor.MaterialOverride = FloorMaterial;
                floor.Position = new Vector3(x, 0, y);
                floor.Name = "Floor" + x + "_" + y;
                AddChild(floor);

                if (GenerateWalls)
                {
                    if (x == 0 || x == maxWidth - FloorSize)
                        for (float z = 0; z < numberOfWallsOnTile; z++)
                        {
                            MeshInstance3D wall = (MeshInstance3D)WallTile.Instantiate();
                            if (x == 0)
                            {
                                wall.Position = new Vector3(x - halfFloorSize, 0, y - halfWallSize + z * halfWallSize * 2);
                                wall.RotateY(Mathf.DegToRad(90));
                            }
                            else if (x == maxHeight - FloorSize)
                            {
                                wall.Position = new Vector3(x + halfFloorSize, 0, y - halfWallSize + z * halfWallSize * 2);
                                wall.RotateY(Mathf.DegToRad(-90));
                            }
                            wall.Name = "Wall" + x + "_" + y + "#" + z;
                            AddChild(wall);
                        }

                    if (y == 0 || y == maxHeight - FloorSize)
                        for (float z = 0; z < numberOfWallsOnTile; z++)
                        {
                            MeshInstance3D wall = (MeshInstance3D)WallTile.Instantiate();
                            if (y == 0)
                                wall.Position = new Vector3(x - halfWallSize + z * halfWallSize * 2, 0, y - halfFloorSize);
                            else if (y == maxHeight - FloorSize)
                            {
                                wall.Position = new Vector3(x - halfWallSize + z * halfWallSize * 2, 0, y + halfFloorSize);
                                wall.RotateY(Mathf.DegToRad(180));
                            }
                            wall.Name = "Wall" + x + "_" + y + "#" + z;
                            AddChild(wall);
                        }
                }
            }
        }
    }
}

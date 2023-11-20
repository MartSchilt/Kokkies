using Godot;
using Kokkies;
using System;

public partial class PlayerCharacter : CharacterBody3D
{
	[Export]
	public MultiplayerSynchronizer MpS;
    [Export]
    public MeshInstance3D Mesh;
    [Export]
    public RayCast3D AimCast;
    [Export]
    public Node3D CameraNeck;
    [Export]
    public Camera3D Camera;
    [Export]
    public Label3D NameLabel;

    public const float Speed = 7.5f;
	public const float JumpVelocity = 7.5f;
	public const float CameraSpeed = 0.005f;

	// Get the gravity from the project settings to be synced with RigidBody nodes.
	public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

    private Player player;

    public override void _Ready()
    {
        var parent = GetParent();
        MpS.SetMultiplayerAuthority(int.Parse(parent.Name));
        player = GameManager.Players.Find(p => p.Id == parent.GetMeta("PlayerId").As<long>());

        Camera.Current = IsControlled();
		NameLabel.Text = player.Name + "#" + player.Id;
		StandardMaterial3D mat = new StandardMaterial3D();
        mat.AlbedoColor = player.Color;
		Mesh.MaterialOverlay = mat;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventMouseButton eventMouseButton) return;
        if (!AimCast.IsColliding()) return;
        var target = AimCast.GetCollider() as PlayerCharacter;
        

        GD.Print($"Shot {target.player.Name} (HP: {target.player.Health}) at: {target.Position}");
        target.player.Health -= 20;

        if (target.player.Health <= 0)
        {
			GD.Print($"{player.Name} killed {target.player.Name}");
            player.Score += 5;
        }
    }

    public override void _Process(double delta)
    {
        if (player.Health <= 0)
        {
            var current = delta;
			GD.Print($"{player.Name} died... ");
            Position = new Vector3(-10000, -1000, 10000); // temporarily remove from playing field
            Rotation += new Vector3(45, 0, 45);
            
            if (current == delta + 1000)
            {
                Position = new Vector3(0, 2, 2);
                Rotation = new Vector3(0, 0, 0);
            }
        }

        base._Process(delta);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!IsControlled())
            return;
        
		if (@event is InputEventMouseButton)
			Input.MouseMode = Input.MouseModeEnum.Captured;
		else if (@event.IsActionPressed("ui_cancel"))
			Input.MouseMode = Input.MouseModeEnum.Visible;

		if (Input.MouseMode == Input.MouseModeEnum.Captured && @event is InputEventMouseMotion mouseMotion)
		{
			this.RotateY(-mouseMotion.Relative.X * CameraSpeed);
			Camera.RotateX(-mouseMotion.Relative.Y * CameraSpeed);
			Camera.Rotation = new Vector3(Camera.Rotation.X, Math.Clamp(- mouseMotion.Relative.Y * CameraSpeed, -1, 1), 0);
		}
	}


public override void _PhysicsProcess(double delta)
	{
		if (!IsControlled())
			return;

		Vector3 velocity = Velocity;

		// Add the gravity.
		if (!IsOnFloor())
			velocity.Y -= gravity * (float)delta;

		// Handle Jump.
		if (Input.IsActionJustPressed("jump") && IsOnFloor())
			velocity.Y = JumpVelocity;

		// Get the input direction and handle the movement/deceleration.
		// As good practice, you should replace UI actions with custom gameplay actions.
		Vector2 inputDir = Input.GetVector("left", "right", "forward", "back");
		Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
		if (direction != Vector3.Zero)
		{
			velocity.X = direction.X * Speed;
			velocity.Z = direction.Z * Speed;
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
			velocity.Z = Mathf.MoveToward(Velocity.Z, 0, Speed);
		}

		Velocity = velocity;
		MoveAndSlide();
	}

	private bool IsControlled()
	{
		try
        {
            return MpS.GetMultiplayerAuthority() == Multiplayer.GetUniqueId();
        }
		catch (Exception ex)
		{
			return false;
		}
	}
}

using Godot;
using Kokkies;
using System;
using System.Linq;

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
    public Label3D NameLabel;

    public const float SPEED = 7.5f;
    public const float JUMP_VELOCITY = 7.5f;
    public const float CAMERA_SPEED = 0.005f;

    public bool Alive = true;
    public bool Respawning;
    public Player Player;

    // Get the gravity from the project settings to be synced with RigidBody nodes.
    public float Gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

    private Camera3D camera;
    private Node parent;

    public override void _Ready()
    {
        // Find the dedicated player Node for this client
        parent = GetParent();
        MpS.SetMultiplayerAuthority(int.Parse(parent.Name));
        Player = GameManager.Players.ToList().Find(p => p.Id == parent.GetMeta("PlayerId").As<long>());

        // Initialize
        camera = CameraNeck.GetNode<Camera3D>("Camera3D");

        camera.Current = IsControlled();
        NameLabel.Text = Player.Name + "#" + Player.Id + $"({Player.Health}/100)";
        StandardMaterial3D mat = new StandardMaterial3D();
        mat.AlbedoColor = Player.Color;
        Mesh.MaterialOverlay = mat;
        Player.Health = 100;
    }

    // This method gets called before _UnhandledInput
    // In here you should handle "important" events
    // Probably want the GUI events to go in here
    public override void _Input(InputEvent @event)
    {
        // We don't want to handle any input if this player is not controlled by the current client
        if (!IsControlled())
            return;

        if (@event is InputEventMouseButton mouseButtonEvent)
        {
            // Mouse Button Events
            // If the player clicks on the screen, the game will capture all the mouse movements
            Input.MouseMode = Input.MouseModeEnum.Captured;
        }
        else if (@event is InputEventMouseMotion mouseMotionEvent)
        {
            // Mouse Motion Events
            if (Input.MouseMode == Input.MouseModeEnum.Captured)
            {
                RotateY(-mouseMotionEvent.Relative.X * CAMERA_SPEED);
                CameraNeck.RotateX(-mouseMotionEvent.Relative.Y * CAMERA_SPEED);
                CameraNeck.Rotation = new Vector3(CameraNeck.Rotation.X, Math.Clamp(-mouseMotionEvent.Relative.Y * CAMERA_SPEED, -1, 1), 0);
            }
        }
        else if (@event is InputEventKey keyEvent)
        {
            // Keyboard Events
            if (keyEvent.IsActionPressed("ui_cancel"))
            {
                Input.MouseMode = Input.MouseModeEnum.Visible;
            }

            switch (keyEvent.Keycode)
            {
                // Respawn should go to the spawn points which are loaded in the SceneManager.
                // Perhaps do the respawning in there?
                case Key.R:
                    GlobalPosition = new Vector3(0, 2, 2);
                    GlobalRotation = new Vector3(0, 0, 0);
                    break;
            }
        }
    }

    // Here all the input events go if they were not handled by _Input
    // We want gameplay things to go in here
    public override void _UnhandledInput(InputEvent @event)
    {
        // We don't want to handle any input if this player is not controlled by the current client
        if (!IsControlled())
            return;

        if (@event is InputEventMouseButton mouseButtonEvent)
        {
            // Mouse Button Events
            switch (mouseButtonEvent.ButtonIndex)
            {
                // Hitscan shooting
                case MouseButton.Left:
                    if (!AimCast.IsColliding())
                        return;

                    var target = AimCast.GetCollider() as PlayerCharacter;
                    GD.Print($"{target.Player.Id} took damage from {Player.Id}");
                    var targetCharacter = parent.GetParent().GetChildren().ToList().Find(p => p.Name == target.Player.Id.ToString()).GetChild(1);
                    targetCharacter.Rpc(nameof(Damage), target.Player.Id, 20);
                    break;
            }
        }
        else if (@event is InputEventMouseMotion mouseMotionEvent)
        {
            // Mouse Motion Events
        }
        else if (@event is InputEventKey keyEvent)
        {
            // Keyboard Events
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        NameLabel.Text = Player.Name + "#" + Player.Id + $"({Player.Health}/100)";

        if (!IsControlled())
            return;

        Vector3 velocity = Velocity;

        // Add the gravity.
        if (!IsOnFloor())
            velocity.Y -= Gravity * (float)delta;

        // Handle Jump.
        if (Input.IsActionJustPressed("jump") && IsOnFloor())
            velocity.Y = JUMP_VELOCITY;

        // Get the input direction and handle the movement/deceleration.
        // As good practice, you should replace UI actions with custom gameplay actions.
        Vector2 inputDir = Input.GetVector("left", "right", "forward", "back");
        Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
        if (direction != Vector3.Zero)
        {
            velocity.X = direction.X * SPEED;
            velocity.Z = direction.Z * SPEED;
        }
        else
        {
            velocity.X = Mathf.MoveToward(Velocity.X, 0, SPEED);
            velocity.Z = Mathf.MoveToward(Velocity.Z, 0, SPEED);
        }

        Velocity = velocity;
        MoveAndSlide();
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    public void Damage(int playerId, int dmg)
    {
        GD.Print($"{playerId} took damage, and I'm {Player.Id}");

        if (Alive)
            Player.Health -= dmg;

        if (Player.Health <= 0)
            Respawn();
    }

    private void Respawn()
    {
        Alive = false;
        GlobalPosition = new Vector3(0, 2, 2);
        GlobalRotation = new Vector3(0, 0, 0);
        Player.Health = 100;
        Alive = true;
    }

    private bool IsControlled()
    {
        try
        {
            return MpS.GetMultiplayerAuthority() == Multiplayer.GetUniqueId();
        }
        catch (Exception)
        {
            return false;
        }
    }
}

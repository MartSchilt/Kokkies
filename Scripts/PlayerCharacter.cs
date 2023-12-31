using Godot;
using Kokkies;
using System;
using System.Linq;

public enum SoundType
{
    Gun,
    Hit,
    Death
}

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
    [Export]
    public Timer RespawnTimer;
    [Export]
    public float Speed = 10f;
    [Export]
    public float JumpVelocity = 10f;
    [Export]
    public float CameraSpeed = 0.005f;
    [Export]
    public int MaxHealth = 100;

    public bool Alive;
    public bool Respawning;
    public Player Player;

    // Get the gravity from the project settings to be synced with RigidBody nodes.
    public float Gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

    private SceneManager sceneManager;
    private Camera3D camera;
    private AudioStreamPlayer3D gunSound;
    private AudioStreamPlayer3D hitSound;
    private AudioStreamPlayer3D deathSound;

    public override void _Ready()
    {
        // Find the dedicated player Node for this client
        MpS.SetMultiplayerAuthority(int.Parse(Name));
        Player = GameManager.Players.ToList().Find(p => p.Id.ToString() == Name);

        // Initialize
        sceneManager = GetParent() as SceneManager;
        camera = CameraNeck.GetNode<Camera3D>("Camera3D");
        gunSound = GetNode<AudioStreamPlayer3D>("GunSound");
        hitSound = GetNode<AudioStreamPlayer3D>("HitSound");
        deathSound = GetNode<AudioStreamPlayer3D>("DeathSound");

        camera.Current = IsControlled();
        StandardMaterial3D mat = new();
        mat.AlbedoColor = Player.Color;
        Mesh.MaterialOverlay = mat;

        RespawnTimer.Timeout += () => sceneManager.Respawn(this);
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
                RotateY(-mouseMotionEvent.Relative.X * CameraSpeed);
                CameraNeck.RotateX(-mouseMotionEvent.Relative.Y * CameraSpeed);
                CameraNeck.Rotation = new Vector3(CameraNeck.Rotation.X, Math.Clamp(-mouseMotionEvent.Relative.Y * CameraSpeed, -1, 1), 0);
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
                case Key.R:
                    Respawn();
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

        if (!Alive)
            return;

        if (@event is InputEventMouseButton mouseButtonEvent && mouseButtonEvent.Pressed)
        {
            // Mouse Button down Events
            switch (mouseButtonEvent.ButtonIndex)
            {
                // Hitscan shooting
                case MouseButton.Left:
                    Shoot();
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
        if (!IsControlled())
            return;

        if (!Alive)
            return;

        Vector3 velocity = Velocity;

        // Add the gravity.
        if (!IsOnFloor())
            velocity.Y -= Gravity * (float)delta;

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

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    public void Damage(int playerId, int targetId, int dmg)
    {
        if (targetId != Player.Id)
            return;

        Rpc(nameof(PlaySound), (int)SoundType.Hit);

        if (Alive)
        {
            Player.Health -= dmg;

            if (Player.Health <= 0)
            {
                sceneManager.Rpc(nameof(sceneManager.AddPoints), playerId, 10);
                Respawn();
            }

            NameLabel.Text = Player.Name + "#" + Player.Id + $"({Player.Health}/100)";
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void PlaySound(SoundType soundType)
    {
        switch (soundType)
        {
            case SoundType.Gun:
                gunSound.Play();
                break;
            case SoundType.Hit:
                hitSound.Play();
                break;
            case SoundType.Death:
                deathSound.Play();
                break;
        }
    }

    private void Shoot()
    {
        Rpc(nameof(PlaySound), (int)SoundType.Gun);

        if (!AimCast.IsColliding())
            return;

        if (AimCast.GetCollider() is PlayerCharacter target)
        {
            foreach (var child in sceneManager.GetChildren())
            {
                if (child.Name == target.Player.Id.ToString())
                {
                    child.Rpc(nameof(Damage), Player.Id, target.Player.Id, 20);
                    break;
                }
            }
        }
    }

    private void Respawn()
    {
        if (Alive)
        {
            Alive = false;
            Rpc(nameof(PlaySound), (int)SoundType.Death);
            RotateX(Mathf.DegToRad(-90));

            RespawnTimer.Start();
        }
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

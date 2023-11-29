using Godot;
using System;
using System.Linq;

public enum SoundType
{
	Death,
	Hit,
	Gun,
	Reload
}

public partial class BaseCharacter : CharacterBody3D
{
	[Export]
	public float Speed = 10f;
	[Export]
	public float JumpVelocity = 10f;
	[Export]
	public float CameraSpeed = 0.005f;
	[Export]
	public int MaxHealth = 100;
	[Export]
	public MultiplayerSynchronizer MpS;
	[Export]
	public MeshInstance3D Mesh;
	[Export]
	public Node3D CameraNeck;
	[Export]
	public Label3D NameLabel;
	[Export]
	public AnimationPlayer AnimationPlayer;
	[Export]
	public Timer RespawnTimer;

	private int _health;
	public virtual int Health { get; set; }
	public AudioStreamPlayer3D AudioPlayer { get; set; }

	// Get the gravity from the project settings to be synced with RigidBody nodes.
	public float Gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
	public bool Alive;
	public Player Player;
	public SceneManager SceneManager;
	public Camera3D Camera;

	public AudioStreamPlayer3D DeathSound;
	public AudioStreamPlayer3D HitSound;
	public AudioStreamPlayer3D GunSound;
	public AudioStreamPlayer3D ReloadSound;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
    {
        // Find the dedicated player Node for this client
        MpS.SetMultiplayerAuthority(int.Parse(Name));
		Player = GameManager.Players.ToList().Find(p => p.Id.ToString() == Name);
		AddToGroup("Player");

		// Add audio player for VOIP
		AudioPlayer = new();
		AddChild(AudioPlayer);

		// Initialize
		Name = Player.Id.ToString();
		Health = MaxHealth;
		SceneManager = GetParent() as SceneManager;
		Camera = CameraNeck.GetNode<Camera3D>("Camera3D");

        Camera.Current = IsControlled();
		RespawnTimer.Timeout += () => SceneManager.Respawn(this);

		StandardMaterial3D mat = new();
		mat.AlbedoColor = Player.Color;
		Mesh.MaterialOverlay = mat;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}


	// Called after every 'delta' elapsed time. This time is almost perfectly constant and runs as a separate process than the default '_Process'
	public override void _PhysicsProcess(double delta)
	{
		// We don't want the character to move if they are dead
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
			if (keyEvent.IsAction("ui_show_scoreboard"))
			{
				if (keyEvent.IsPressed())
                    GameManager.Main.GUI.EnableGUI(GameManager.Main.GUI.Scoreboard);
				else if (keyEvent.IsReleased())
                    GameManager.Main.GUI.EnableGUI(GameManager.Main.GUI.Scoreboard);
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
		}
		else if (@event is InputEventMouseMotion mouseMotionEvent)
		{
			// Mouse Motion Events
		}
		else if (@event is InputEventKey keyEvent)
		{
			// Keyboard down Events
			if (keyEvent.IsPressed())
				switch (keyEvent.Keycode)
				{
					case Key.T:
						Respawn();
						break;
				}
		}
	}

	public void Respawn()
	{
		if (Alive)
		{
			Alive = false;
			Rpc(nameof(PlaySound), (int)SoundType.Death);
			RotateX(Mathf.DegToRad(-90));

			if (RespawnTimer.TimeLeft == 0)
				RespawnTimer.Start();
		}
	}

	public bool IsControlled()
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

	#region RPC methods
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
	public void Damage(int playerId, int targetId, int dmg)
	{
		if (targetId != Player.Id)
			return;

		Rpc(nameof(PlaySound), (int)SoundType.Hit);

		if (Alive)
		{
			Health -= dmg;

			if (Health <= 0)
			{
				SceneManager.Rpc(nameof(SceneManager.AddPoints), playerId, 10);
				Respawn();
			}

			NameLabel.Text = Player.Name + "#" + Player.Id + $"({Health}/100)";
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	public void PlaySound(SoundType soundType)
	{
		switch (soundType)
		{
			case SoundType.Death:
				DeathSound.Play();
				break;
			case SoundType.Hit:
				HitSound.Play();
				break;
			case SoundType.Gun:
				GunSound.Play();
				break;
			case SoundType.Reload:
				ReloadSound.Play();
				break;
		}
	}
	#endregion
}

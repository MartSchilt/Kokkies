using Godot;
using Kokkies;
using System;
using System.Linq;

public enum SoundType
{
	Gun,
	Hit,
	Death,
	Reload
}

public partial class PlayerCharacter : CharacterBody3D
{
	#region Exports
	[Export]
	public float Speed = 10f;
	[Export]
	public float JumpVelocity = 10f;
	[Export]
	public float CameraSpeed = 0.005f;
	[Export]
	public int MaxHealth = 100;
	[Export]
	public int MaxAmmo = 6;
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
	public Timer ReloadTimer;
	[Export]
	public Timer RespawnTimer;
	#endregion

	#region Properties
	private int _health;
	public int Health
	{
		get => _health;
		set
		{
			_health = value;
			if (OverlayManager != null)
				OverlayManager.HealthValue = value;
		}
	}

	private int _ammo;
	public int Ammo
	{
		get => _ammo;
		set
		{
			_ammo = value;
			if (OverlayManager != null)
				OverlayManager.AmmoValue = value;
		}
	}
	#endregion

	#region Fields
	public bool Alive;
	public bool Reloading;
	public Player Player;
	public GUIManager GUI;
	public ShooterGUI OverlayManager;

	// Get the gravity from the project settings to be synced with RigidBody nodes.
	public float Gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

	private SceneManager sceneManager;
	private Camera3D camera;
	private RayCast3D _aimCast;
	private AudioStreamPlayer3D gunSound;
	private AudioStreamPlayer3D hitSound;
	private AudioStreamPlayer3D deathSound;
	private AudioStreamPlayer3D reloadSound;
	#endregion

	public override void _Ready()
	{
		// Find the dedicated player Node for this client
		MpS.SetMultiplayerAuthority(int.Parse(Name));
		Player = GameManager.Players.ToList().Find(p => p.Id.ToString() == Name);

		// Initialize
		sceneManager = GetParent() as SceneManager;
		camera = CameraNeck.GetNode<Camera3D>("Camera3D");
		_aimCast = CameraNeck.GetNode<RayCast3D>("AimCast");
		gunSound = GetNode<AudioStreamPlayer3D>("Sounds/GunSound");
		hitSound = GetNode<AudioStreamPlayer3D>("Sounds/HitSound");
		deathSound = GetNode<AudioStreamPlayer3D>("Sounds/DeathSound");
		reloadSound = GetNode<AudioStreamPlayer3D>("Sounds/ReloadSound");

		// Only add UI if this is the client's player
		if (IsControlled() && sceneManager.GetParent().HasNode("GUI"))
		{
			GUI = sceneManager.GetParent().GetNode<GUIManager>("GUI");
			OverlayManager = GUI.ShooterGUI;
			GUI.EnableShooterGUI();
		}

		camera.Current = IsControlled();
		StandardMaterial3D mat = new();
		mat.AlbedoColor = Player.Color;
		Mesh.MaterialOverlay = mat;

		RespawnTimer.Timeout += () => sceneManager.Respawn(this);
		ReloadTimer.Timeout += () =>
		{
			Ammo = MaxAmmo;
			Reloading = false;
		};
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
			else if (keyEvent.IsAction("ui_show_scoreboard"))
			{
				if (keyEvent.IsPressed())
					GUI.EnableScoreboard();
				else if (keyEvent.IsReleased())
					GUI.DisableScoreboard();
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
			// Keyboard down Events
			if (keyEvent.IsPressed())
				switch (keyEvent.Keycode)
				{
					case Key.R:
						Reload();
						break;
					case Key.T:
						Respawn();
						break;
				}
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
			Health -= dmg;

			if (Health <= 0)
			{
				sceneManager.Rpc(nameof(sceneManager.AddPoints), playerId, 10);
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
			case SoundType.Gun:
				gunSound.Play();
				break;
			case SoundType.Hit:
				hitSound.Play();
				break;
			case SoundType.Death:
				deathSound.Play();
				break;
			case SoundType.Reload:
				reloadSound.Play();
				break;
		}
	}

	private void Shoot()
	{
		// Only handle things if you are not reloading
		if (!Reloading)
		{
			// Only shoot if you have Ammo
			if (Ammo > 0)
			{
				Rpc(nameof(PlaySound), (int)SoundType.Gun);
				AnimationPlayer.Play("Shoot");
				Ammo -= 1;

				if (!_aimCast.IsColliding())
					return;

				if (_aimCast.GetCollider() is PlayerCharacter target)
				{
					foreach (var child in sceneManager.GetChildren())
						if (child.Name == target.Player.Id.ToString())
						{
							child.Rpc(nameof(Damage), Player.Id, target.Player.Id, 20);
							break;
						}
				}
			}
			// Auto reload for ease of use
			else
				Reload();
		}
	}

	private void Reload()
	{
		if (!Reloading)
		{
			Reloading = true;
			PlaySound(SoundType.Reload);
			AnimationPlayer.Play("Reload");

			if (ReloadTimer.TimeLeft == 0)
				ReloadTimer.Start();
		}
	}

	private void Respawn()
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

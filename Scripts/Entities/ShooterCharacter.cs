using Godot;
using System;

public partial class ShooterCharacter : BaseCharacter
{
	#region Exports
	[Export]
	public int MaxAmmo = 6;
	[Export]
	public Timer ReloadTimer;
	#endregion

	#region Properties
	private int _health;
	public override int Health
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
	public bool Reloading;
	public ShooterGUI OverlayManager;

	private RayCast3D _aimCast;
	#endregion

	public override void _Ready()
	{
		base._Ready();

		// Initialize
		Ammo = MaxAmmo;

		_aimCast = CameraNeck.GetNode<RayCast3D>("AimCast");
		DeathSound = GetNode<AudioStreamPlayer3D>("Sounds/DeathSound");
		HitSound = GetNode<AudioStreamPlayer3D>("Sounds/HitSound");
		GunSound = GetNode<AudioStreamPlayer3D>("Sounds/GunSound");
		ReloadSound = GetNode<AudioStreamPlayer3D>("Sounds/ReloadSound");

		// Only add UI if this is the client's player
		if (IsControlled())
		{
			OverlayManager = GameManager.Main.GUI.ShooterGUI;
			GameManager.Main.GUI.EnableShooterGUI();
		}

		ReloadTimer.Timeout += () =>
		{
			Ammo = MaxAmmo;
			Reloading = false;
		};
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		base._UnhandledInput(@event);

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
				}
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

				if (_aimCast.GetCollider() is BaseCharacter target)
				{
					foreach (var child in SceneManager.GetChildren())
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
}

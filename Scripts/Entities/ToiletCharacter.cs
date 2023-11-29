using Godot;
using System;

public partial class ToiletCharacter : BaseCharacter
{
    public ToiletGUI OverlayManager;
    
	// Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
		base._Ready();
		
		DeathSound = GetNode<AudioStreamPlayer3D>("Sounds/DeathSound");
        
		// Only add UI if this is the client's player
        if (IsControlled())
        {
            OverlayManager = GameManager.Main.GUI.ToiletGUI;
            GameManager.Main.GUI.EnableGUI(OverlayManager);
        }
    }

	public override void _UnhandledInput(InputEvent @event)
	{
		base._Input(@event);

		// We don't want to handle any input if this player is not controlled by the current client
		if (!IsControlled())
			return;

		if (@event is InputEventMouseButton mouseButtonEvent && mouseButtonEvent.Pressed)
		{
			// Mouse Button down Events
		}
		else if (@event is InputEventKey keyEvent && keyEvent.Pressed)
		{
			// Keyboard down Events
			OverlayManager.SetKeyRequest($"{keyEvent.Keycode}");
		}
	}
}

using Godot;
using System;

public partial class ToiletCharacter : BaseCharacter
{
    public ToiletGUI OverlayManager;
    public ToiletManager ToiletManager;

    private Key _key;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        base._Ready();

        ToiletManager = GetParent().GetNode<ToiletManager>(nameof(ToiletManager));

        DeathSound = GetNode<AudioStreamPlayer3D>("Sounds/DeathSound");

        // Only add UI if this is the client's player
        if (IsControlled())
        {
            OverlayManager = GameManager.Main.GUI.ToiletGUI;
            GameManager.Main.GUI.EnableGUI(OverlayManager);
        }

        // Add audio effects
        //AudioEffectReverb reverb = new();
        //GD.Print(AudioServer.GetBusEffectCount(GameManager.VoiceMicIndex));
        //AudioServer.AddBusEffect(GameManager.VoiceMicIndex, reverb);
        //GD.Print(AudioServer.GetBusEffectCount(GameManager.VoiceMicIndex));
        //GD.Print(AudioServer.IsBusEffectEnabled(GameManager.VoiceMicIndex, 0));
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        base._Input(@event);

        // We don't want to handle any input if this player is not controlled by the current client
        // Or if the game is paused
        if (!IsControlled() || GameManager.Main.GUI.IsPaused)
            return;

        if (@event is InputEventMouseButton mouseButtonEvent && mouseButtonEvent.Pressed)
        {
            // Mouse Button down Events
        }
        else if (@event is InputEventKey keyEvent && keyEvent.IsPressed())
        {
            // Keyboard down Events
            if (keyEvent.Keycode == _key)
                ToiletManager.Rpc(nameof(ToiletManager.PressedRightKey), Player.Id);
        }
    }

    public void SetKey(Key key)
    {
        if (IsControlled())
        {
            _key = key;
            OverlayManager.SetKeyRequest(key);
        }
    }
}

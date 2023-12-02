using Godot;
using System;

public partial class ToiletGUI : MarginContainer
{
    [Export]
    public ProgressBar ProgressBar;
    [Export]
    public TextureRect KeyRequest;

    public override void _Ready()
    {
    }

    public override void _Process(double delta)
    {
    }

    public void SetKeyRequest(Key key)
    {
        var texture = GD.Load<Texture2D>($"res://Assets/Textures/ButtonPrompts/Keyboard & Mouse/Light/{key}_Key_Light.png");
        KeyRequest.Texture = texture;
    }
}

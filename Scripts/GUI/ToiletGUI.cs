using Godot;
using System;
using System.Collections.Generic;

public partial class ToiletGUI : MarginContainer
{
    [Export]
    public ProgressBar ProgressBar;
    [Export]
    public Control KeySequence;

    public override void _Ready()
    {
    }

    public override void _Process(double delta)
    {
    }

    public void SetKeySequence(List<Key> keySequence)
    {
        foreach(Node child in KeySequence.GetChildren())
            KeySequence.RemoveChild(child);

        foreach (Key key in keySequence)
        {
            TextureRect keyRequest = new();
            var texture = GD.Load<Texture2D>($"res://Assets/Textures/ButtonPrompts/Keyboard & Mouse/Light/{key}_Key_Light.png");
            keyRequest.Texture = texture;
            KeySequence.AddChild(keyRequest);
        }
    }

    public void KeyPressed(Key key, int index)
    {
        var keyRequest = KeySequence.GetChild<TextureRect>(index);
        var texture = GD.Load<Texture2D>($"res://Assets/Textures/ButtonPrompts/Keyboard & Mouse/Dark/{key}_Key_Dark.png");
        keyRequest.Texture = texture;
    }
}

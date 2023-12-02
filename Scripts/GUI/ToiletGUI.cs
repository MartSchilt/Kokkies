using Godot;
using System;

public partial class ToiletGUI : MarginContainer
{
    [Export]
    public Label KeyRequest;

    public override void _Ready()
    {
    }

    public override void _Process(double delta)
    {
    }

    public void SetKeyRequest(Key key)
    {
        KeyRequest.Text = $"{key}";
    }
}

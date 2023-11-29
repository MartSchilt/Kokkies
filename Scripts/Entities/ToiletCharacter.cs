using Godot;
using System;

public partial class ToiletCharacter : BaseCharacter
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		base._Ready();
		
		DeathSound = GetNode<AudioStreamPlayer3D>("Sounds/DeathSound");
	}
}

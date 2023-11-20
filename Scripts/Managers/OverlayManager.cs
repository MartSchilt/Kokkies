using Godot;
using System;
using Kokkies;

public partial class OverlayManager : Control
{
	private int _health;
	private int _score;

	public int HealthValue
	{
		get => _health;
		set
		{
			_health = value;
			_healthLabel.Text = $"Health: {_health}";
		}
	}

	public int ScoreValue
	{
		get => _score;
		set
		{
			_score = value;
			_scoreLabel.Text = $"Score: {_score}";
		}
	}

	public int AmmoValue { get; set; }

	private Label _healthLabel;
	private Label _scoreLabel;
	private Label _ammoLabel;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_health = 0;
		_score = 0;
		AmmoValue = 0;
		_healthLabel = GetNode<Label>("HealthLabel");
		_scoreLabel = GetNode<Label>("ScoreLabel");
		_ammoLabel = GetNode<Label>("AmmoLabel");
	}
}

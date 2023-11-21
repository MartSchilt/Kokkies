using Godot;

public partial class ShooterGUI : Control
{
	[Export]
    public Label HealthLabel;
    [Export]
    public Label ScoreLabel;
    [Export]
    public Label AmmoLabel;
    
	public int HealthValue
	{
		get => _healthValue;
		set
		{
			_healthValue = value;
			HealthLabel.Text = $"Health: {_healthValue}";
		}
	}

	public int ScoreValue
	{
		get => _scoreValue;
		set
		{
			_scoreValue = value;
			ScoreLabel.Text = $"Score: {_scoreValue}";
		}
	}

	public int AmmoValue
	{
		get => _ammoValue;
		set
		{
			_ammoValue = value;
			AmmoLabel.Text = $"Ammo: {_ammoValue}";
		}
	}

	public CanvasLayer GUI;
    
	private int _healthValue;
	private int _scoreValue;
	private int _ammoValue;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GUI = GetParent() as CanvasLayer;
		
		HealthValue = 0;
		ScoreValue = 0;
		AmmoValue = 0;
	}
}

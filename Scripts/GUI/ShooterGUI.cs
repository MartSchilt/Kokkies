using Godot;

public partial class ShooterGUI : Control
{
	[Export]
    public Label HealthLabel;
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

	public int AmmoValue
	{
		get => _ammoValue;
		set
		{
			_ammoValue = value;
			AmmoLabel.Text = $"Ammo: {_ammoValue}";
		}
	}
    
	private int _healthValue;
	private int _ammoValue;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{		
		HealthValue = 0;
		AmmoValue = 0;
	}
}

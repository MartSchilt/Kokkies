using Godot;
using Kokkies;
using System.Linq;

/// <summary>
/// This is the script for the 'main' node of our game
/// Here you can put properties to be used by all the other scenes
/// This node also loads in any new scenes that you want
/// </summary>
public partial class MainScene : Node
{
	[Export]
	public MusicManager MusicManager;
	[Export]
	public PackedScene MainMenu;
	[Export]
	public GUIManager GUI;
	[Export]
	public VoiceOrchestrator VoiceOrchestrator;
	[Export]
	public MultiplayerManager MultiplayerManager;

	public Node Scenes;

	private MainMenu _mainMenu;

	public override void _Ready()
	{
		GD.Print($"Starting {nameof(MainScene)}");

		Scenes = GetNode("Scenes");

		_mainMenu = MainMenu.Instantiate() as MainMenu;
		AddChild(_mainMenu);
		MusicManager.PlayMusic(_mainMenu.Name);

		LoadMainMenu();
	}

	public bool IsInMenu()
	{
		return _mainMenu.Visible;
	}

	public void LoadScene(string scenePath)
	{
		var scene = GD.Load<PackedScene>(scenePath);
		LoadScene(scene);
	}

	public void LoadScene(PackedScene scene)
	{
		_mainMenu.Hide();

		var instance = scene.Instantiate();
		Scenes.AddChild(instance);

		MusicManager.PlayMusic(instance.Name);
	}

	public void LoadMainMenu()
	{
		GUI.DisableAll();

		// Remove all the scenes
		foreach (var scene in Scenes.GetChildren())
			scene.QueueFree();

		_mainMenu.Show();

		// Show the mouse
		Input.MouseMode = Input.MouseModeEnum.Visible;
	}

	public SceneManager GetCurrentScene()
	{
		if (IsInMenu())
			return Scenes.GetChildren().First() as SceneManager;
		else
			return null;
	}
}

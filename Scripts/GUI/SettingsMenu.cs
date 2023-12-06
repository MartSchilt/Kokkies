using Godot;
using System;

public partial class SettingsMenu : MarginContainer
{
	[Export]
    public HSlider SliderInputThreshold;
    [Export]
    public SpinBox SpinBoxInputThreshold;
    [Export]
    public HSlider SliderMusicVolume;

	private GUIManager GUI;
	private MultiplayerManager _mpManager;

	public override void _Ready()
	{
		GUI = GetParent<GUIManager>();
		_mpManager = GameManager.Main.MultiplayerManager;
	}

	#region VOIP
	private void _on_record_toggled(bool toggled_on)
	{
		_mpManager.VOrchestrator.Recording = toggled_on;
	}
	private void _on_input_thresh_value_changed(float value)
	{
		_mpManager.VOrchestrator.InputThreshold = value;
        SpinBoxInputThreshold.Value = value;
	}

	private void _on_listen_toggled(bool button_pressed)
	{
		_mpManager.VOrchestrator.Listen = button_pressed;
	}

	private void _on_log_voice_toggled(bool button_pressed)
	{
		_mpManager.ShouldLogVoice = button_pressed;
	}
	#endregion

	#region Music
	private void _on_music_toggled(bool toggled_on)
	{
		GameManager.MuteMusic(!toggled_on);
        SliderMusicVolume.Editable = toggled_on;
    }

    private void _on_music_volume_value_changed(double value)
    {
        GameManager.MusicVolume(value);
    }
    #endregion

    #region Buttons
    private void _on_back_button_down()
	{
		GUI.DisableGUI(GUI.SettingsMenu);
		GUI.EnableGUI(GUI.PauseMenu);
	}
	#endregion
}

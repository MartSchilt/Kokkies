using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class ToiletManager : Node
{
    public SceneManager SceneManager;
    public List<Key> _keySequence;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        GD.Print($"Starting {nameof(ToiletManager)}");

        SceneManager = GetParent() as SceneManager;

        _keySequence = new List<Key>();

        SceneManager.SceneLoaded += GenerateKeyRequest;
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void PressedRightKey(int playerId)
    {
        // Play sound for points?
        //Rpc(nameof(PlaySound), (int)SoundType.Hit);
        
        if (playerId == Multiplayer.GetUniqueId())
            SceneManager.Rpc(nameof(SceneManager.AddPoints), playerId, 10);

        GenerateKeyRequest();
    }

    private void GenerateKeyRequest()
    {
        Random rnd = new();
        var key = (Key)rnd.Next(65, 90); // A till Z
        _keySequence.Add(key);
        
        foreach (ToiletCharacter player in SceneManager.PlayerCharacters.Cast<ToiletCharacter>())
            player.SetKey(key);
    }
}

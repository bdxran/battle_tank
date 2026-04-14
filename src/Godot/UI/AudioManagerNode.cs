using Godot;
using BattleTank.GameLogic.Network;
using BattleTank.Godot.Network;
using BattleTank.Godot.Renderer;

namespace BattleTank.Godot.UI;

/// <summary>
/// Manages sound effects for gameplay events.
/// Audio files are loaded from res://assets/sounds/ — missing files are silently ignored.
/// Requires actual .ogg files to be added to the assets/sounds/ directory.
/// </summary>
public partial class AudioManagerNode : Node
{
    private AudioStreamPlayer _firePlayer = null!;
    private AudioStreamPlayer _hitPlayer = null!;
    private AudioStreamPlayer _deathPlayer = null!;

    private ClientNetworkManager _network = null!;
    private GameRenderer _renderer = null!;

    public void Initialize(ClientNetworkManager network, GameRenderer renderer)
    {
        _network = network;
        _renderer = renderer;

        _renderer.BulletCreated += OnBulletCreated;
        _renderer.TankHit += OnTankHit;
        _renderer.TankEliminated += OnTankEliminated;
    }

    public override void _Ready()
    {
        _firePlayer = CreatePlayer("res://assets/sounds/fire.ogg");
        _hitPlayer = CreatePlayer("res://assets/sounds/hit.ogg");
        _deathPlayer = CreatePlayer("res://assets/sounds/death.ogg");
    }

    public override void _ExitTree()
    {
        if (_renderer is null) return;
        _renderer.BulletCreated -= OnBulletCreated;
        _renderer.TankHit -= OnTankHit;
        _renderer.TankEliminated -= OnTankEliminated;
    }

    private void OnBulletCreated() => Play(_firePlayer);
    private void OnTankHit() => Play(_hitPlayer);
    private void OnTankEliminated() => Play(_deathPlayer);

    private static void Play(AudioStreamPlayer player)
    {
        if (player.Stream is not null)
            player.Play();
    }

    private AudioStreamPlayer CreatePlayer(string path)
    {
        var player = new AudioStreamPlayer();
        var stream = ResourceLoader.Load<AudioStream>(path);
        if (stream is not null)
            player.Stream = stream;
        AddChild(player);
        return player;
    }
}

using Godot;
using Microsoft.Extensions.Logging;
using BattleTank.GameLogic.Network;
using BattleTank.GameLogic.Rules;
using BattleTank.GameLogic.Shared;

namespace BattleTank.Godot.Nodes;

public partial class GameRoomNode : Node
{
    private const float TickInterval = 1f / Constants.TickRate;

    private ILogger<GameRoomNode> _logger = null!;
    private GameRoom _room = null!;
    private Network.ServerNetworkManager _network = null!;
    private float _accumulator;

    public void Initialize(Network.ServerNetworkManager network, ILogger<GameRoomNode> logger, ILogger<GameRoom> roomLogger)
    {
        _logger = logger;
        _network = network;
        _room = new GameRoom(roomLogger);

        _network.PlayerConnected += OnPlayerConnected;
        _network.PlayerDisconnected += OnPlayerDisconnected;
        _network.InputReceived += OnInputReceived;
    }

    public override void _Process(double delta)
    {
        if (_room is null) return;

        _accumulator += (float)delta;
        while (_accumulator >= TickInterval)
        {
            _accumulator -= TickInterval;
            DoTick();
        }
    }

    public override void _ExitTree()
    {
        if (_network is null) return;
        _network.PlayerConnected -= OnPlayerConnected;
        _network.PlayerDisconnected -= OnPlayerDisconnected;
        _network.InputReceived -= OnInputReceived;
    }

    private void OnPlayerConnected(int peerId)
    {
        var result = _room.AddPlayer(peerId);
        if (!result.IsSuccess)
        {
            _logger.LogWarning("Could not add player {PeerId}: {Error}", peerId, result.Error);
            return;
        }

        var fullState = _room.GetFullState();
        _network.SendToPlayer(peerId, new NetworkMessage(
            MessageType.GameStateFull,
            GameStateSerializer.Serialize(fullState)));

        var joined = new PlayerJoinedMessage(peerId, $"Player{peerId}");
        _network.Broadcast(new NetworkMessage(
            MessageType.PlayerJoined,
            GameStateSerializer.Serialize(joined)));
    }

    private void OnPlayerDisconnected(int peerId)
    {
        _room.RemovePlayer(peerId);

        if (_room.Phase == GamePhase.GameOver)
            BroadcastGameOver();
    }

    private void OnInputReceived(int peerId, PlayerInput input)
    {
        _room.ApplyInput(peerId, input);
    }

    private void DoTick()
    {
        _room.Tick(TickInterval);

        if (_room.Phase == GamePhase.GameOver)
        {
            BroadcastGameOver();
            _room.Reset();
            return;
        }

        _network.Broadcast(new NetworkMessage(
            MessageType.GameStateDelta,
            GameStateSerializer.Serialize(_room.GetDeltaState(0))));
    }

    private void BroadcastGameOver()
    {
        var msg = new GameOverMessage(_room.WinnerId);
        _network.Broadcast(new NetworkMessage(
            MessageType.GameOver,
            GameStateSerializer.Serialize(msg)));

        _logger.LogInformation("Game over broadcast. Winner: {WinnerId}", _room.WinnerId);
    }
}

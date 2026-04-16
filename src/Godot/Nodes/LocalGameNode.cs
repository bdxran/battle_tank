using System;
using Godot;
using Microsoft.Extensions.Logging.Abstractions;
using BattleTank.GameLogic.Network;
using BattleTank.GameLogic.Rules;
using BattleTank.GameLogic.Shared;
using BattleTank.Godot.Network;

namespace BattleTank.Godot.Nodes;

/// <summary>
/// Offline game node: runs a GameRoom locally without any network layer.
/// Implements IGameStateProvider so GameRenderer can consume snapshots directly.
/// All game modes are supported; empty slots are filled with SimpleBot instances.
/// </summary>
public partial class LocalGameNode : Node, IGameStateProvider
{
    private const float TickInterval = 1f / Constants.TickRate;
    private const int MaxTicksPerFrame = 5;
    public const int LocalPlayerId = 1;

    public event Action<GameStateFull>? GameStateFullReceived;
    public event Action<GameStateDelta>? GameStateDeltaReceived;
    public event Action<PlayerEliminatedMessage>? PlayerEliminated;
    public event Action<GameOverMessage>? GameOver;

    private GameRoom _room = null!;
    private float _accumulator;
    private uint _lastAckedTick;
    private uint _inputSeq;
    private bool _firstTick = true;
    private bool _gameOver;
    public bool Running { get; set; } = false;

    public PlayerInfo[] GetLeaderboard() => _room?.GetLeaderboard() ?? [];

    public void Initialize(GameMode mode, string nickname)
    {
        IBattleRules rules = mode switch
        {
            GameMode.Training => new TrainingRules(),
            GameMode.Teams => new TeamsRules(),
            GameMode.Deathmatch => new DeathmatchRules(),
            GameMode.CaptureZone => new CaptureZoneRules(),
            _ => new BattleRoyaleRules(),
        };

        _room = new GameRoom(NullLogger<GameRoom>.Instance, rules);
        _room.AddPlayer(LocalPlayerId, nickname);

        int botsToAdd = Constants.MaxPlayersPerRoom - 1;
        for (int i = 0; i < botsToAdd; i++)
        {
            var result = _room.AddBot();
            if (!result.IsSuccess)
                break;
        }

        // Skip the lobby countdown: the Godot CountdownNode handles the visual countdown
        _room.ForceStart();

        // Emit initial state so the renderer can display the map before the countdown ends
        var full = _room.GetFullState();
        _lastAckedTick = full.SequenceNumber;
        _firstTick = false;
        GameStateFullReceived?.Invoke(full);
    }

    public override void _Process(double delta)
    {
        if (_room is null || _gameOver || !Running) return;

        _accumulator += (float)delta;
        int ticks = 0;
        while (_accumulator >= TickInterval && ticks < MaxTicksPerFrame)
        {
            _accumulator -= TickInterval;
            DoTick();
            ticks++;
        }
        // Prevent spiral of death: discard excess if we can't keep up
        if (_accumulator > TickInterval)
            _accumulator = 0f;
    }

    private void DoTick()
    {
        var flags = ReadInput();
        _room.ApplyInput(LocalPlayerId, new PlayerInput(LocalPlayerId, flags, ++_inputSeq));

        _room.Tick(TickInterval);

        // Dispatch eliminations before state snapshot
        foreach (var elim in _room.GetAndClearEliminations())
            PlayerEliminated?.Invoke(new PlayerEliminatedMessage(elim.EliminatedId, elim.KillerId));

        if (_firstTick)
        {
            _firstTick = false;
            var full = _room.GetFullState();
            _lastAckedTick = full.SequenceNumber;
            GameStateFullReceived?.Invoke(full);
        }
        else
        {
            var delta = _room.GetDeltaState(_lastAckedTick);
            _lastAckedTick = delta.SequenceNumber;
            GameStateDeltaReceived?.Invoke(delta);
        }

        if (_room.Phase == GamePhase.GameOver && !_gameOver)
        {
            _gameOver = true;
            GameOver?.Invoke(new GameOverMessage(
                _room.WinnerId,
                _room.GetLeaderboard(),
                _room.WinnerTeamId));
        }
    }

    private static InputFlags ReadInput()
    {
        InputFlags flags = InputFlags.None;
        if (Input.IsActionPressed("move_forward")) flags |= InputFlags.MoveForward;
        if (Input.IsActionPressed("move_backward")) flags |= InputFlags.MoveBackward;
        if (Input.IsActionPressed("rotate_left")) flags |= InputFlags.RotateLeft;
        if (Input.IsActionPressed("rotate_right")) flags |= InputFlags.RotateRight;
        if (Input.IsActionPressed("fire")) flags |= InputFlags.Fire;
        return flags;
    }
}

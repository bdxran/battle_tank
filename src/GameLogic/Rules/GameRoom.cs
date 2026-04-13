using System;
using System.Collections.Generic;
using System.Numerics;
using Microsoft.Extensions.Logging;
using BattleTank.GameLogic.Entities;
using BattleTank.GameLogic.Network;
using BattleTank.GameLogic.Physics;
using BattleTank.GameLogic.Shared;

namespace BattleTank.GameLogic.Rules;

public class GameRoom
{
    private static readonly Vector2[] SpawnPoints =
    [
        new(100, 100), new(900, 100), new(100, 900), new(900, 900),
        new(500, 100), new(500, 900), new(100, 500), new(900, 500),
        new(300, 300), new(700, 700)
    ];

    private readonly ILogger<GameRoom> _logger;
    private readonly Dictionary<int, TankEntity> _tanks;
    private readonly Dictionary<int, InputFlags> _inputBuffer;
    private readonly Dictionary<int, uint> _lastInputSeq;
    private readonly Dictionary<int, uint> _lastFireTick;
    private readonly List<BulletEntity> _bullets;
    private int _nextBulletId;
    private uint _currentTick;
    private GamePhase _phase;

    private const uint FireCooldownTicks = 10; // 0.5s at 20 TPS

    public GamePhase Phase => _phase;
    public int WinnerId { get; private set; } = -1;

    public GameRoom(ILogger<GameRoom> logger)
    {
        _logger = logger;
        _tanks = new Dictionary<int, TankEntity>();
        _inputBuffer = new Dictionary<int, InputFlags>();
        _lastInputSeq = new Dictionary<int, uint>();
        _lastFireTick = new Dictionary<int, uint>();
        _bullets = new List<BulletEntity>();
        _phase = GamePhase.WaitingForPlayers;
    }

    public Result<TankEntity> AddPlayer(int playerId)
    {
        if (_phase != GamePhase.WaitingForPlayers)
            return Result<TankEntity>.Fail("Game already in progress");

        if (_tanks.Count >= Constants.MaxPlayersPerRoom)
            return Result<TankEntity>.Fail("Room is full");

        if (_tanks.ContainsKey(playerId))
            return Result<TankEntity>.Fail("Player already in room");

        var spawnIndex = _tanks.Count % SpawnPoints.Length;
        var tank = new TankEntity(playerId, SpawnPoints[spawnIndex]);
        _tanks[playerId] = tank;
        _inputBuffer[playerId] = InputFlags.None;
        _lastInputSeq[playerId] = 0;
        _lastFireTick[playerId] = 0;

        _logger.LogInformation("Player {PlayerId} joined at spawn {SpawnIndex}", playerId, spawnIndex);
        CheckPhaseTransition();

        return Result<TankEntity>.Ok(tank);
    }

    public void RemovePlayer(int playerId)
    {
        if (!_tanks.Remove(playerId))
            return;

        _inputBuffer.Remove(playerId);
        _lastInputSeq.Remove(playerId);
        _lastFireTick.Remove(playerId);

        _logger.LogInformation("Player {PlayerId} removed", playerId);

        if (_phase == GamePhase.InProgress)
            CheckWinCondition();
    }

    public void ApplyInput(int playerId, PlayerInput input)
    {
        if (!_tanks.ContainsKey(playerId))
            return;

        if (input.SequenceNumber <= _lastInputSeq[playerId])
            return;

        _inputBuffer[playerId] = input.Flags;
        _lastInputSeq[playerId] = input.SequenceNumber;
    }

    public void Tick(float deltaTime)
    {
        if (_phase != GamePhase.InProgress)
            return;

        foreach (var (id, tank) in _tanks)
        {
            if (!tank.IsAlive) continue;

            var flags = _inputBuffer[id];
            tank.ApplyInput(flags, deltaTime);

            if ((flags & InputFlags.Fire) != 0)
                TryFire(id, tank);
        }

        TickBullets(deltaTime);
        _currentTick++;
        CheckWinCondition();
    }

    public GameStateFull GetFullState()
    {
        var tankSnapshots = new TankSnapshot[_tanks.Count];
        int i = 0;
        foreach (var tank in _tanks.Values)
            tankSnapshots[i++] = tank.GetSnapshot();

        var bulletSnapshots = GetBulletSnapshots();
        return new GameStateFull(_currentTick, tankSnapshots, bulletSnapshots, _phase);
    }

    public GameStateDelta GetDeltaState(uint lastAckedTick)
    {
        var tankSnapshots = new TankSnapshot[_tanks.Count];
        int i = 0;
        foreach (var tank in _tanks.Values)
            tankSnapshots[i++] = tank.GetSnapshot();

        var bulletSnapshots = GetBulletSnapshots();
        return new GameStateDelta(_currentTick, lastAckedTick, tankSnapshots, bulletSnapshots);
    }

    public void Reset()
    {
        _tanks.Clear();
        _inputBuffer.Clear();
        _lastInputSeq.Clear();
        _lastFireTick.Clear();
        _bullets.Clear();
        _nextBulletId = 0;
        _currentTick = 0;
        _phase = GamePhase.WaitingForPlayers;
        WinnerId = -1;
        _logger.LogInformation("GameRoom reset");
    }

    private void TryFire(int playerId, TankEntity tank)
    {
        if (_currentTick - _lastFireTick[playerId] < FireCooldownTicks)
            return;

        _lastFireTick[playerId] = _currentTick;

        float radians = tank.Rotation * MathF.PI / 180f;
        var direction = new Vector2(MathF.Sin(radians), -MathF.Cos(radians));
        var spawnPos = tank.Position + direction * (Constants.TankRadius + Constants.BulletRadius + 1f);

        _bullets.Add(new BulletEntity(_nextBulletId++, playerId, spawnPos, direction));
    }

    private void TickBullets(float deltaTime)
    {
        for (int i = 0; i < _bullets.Count; i++)
        {
            var bullet = _bullets[i];
            if (!bullet.IsAlive) continue;

            bullet.Tick(deltaTime);

            if (CollisionSystem.IsOutOfBounds(bullet))
            {
                bullet.Kill();
                continue;
            }

            foreach (var tank in _tanks.Values)
            {
                if (CollisionSystem.BulletHitsTank(bullet, tank))
                {
                    tank.TakeDamage(Constants.BulletDamage);
                    bullet.Kill();
                    _logger.LogDebug("Bullet {BulletId} hit tank {TankId}", bullet.Id, tank.Id);
                    break;
                }
            }
        }

        // Remove dead bullets
        for (int i = _bullets.Count - 1; i >= 0; i--)
        {
            if (!_bullets[i].IsAlive)
                _bullets.RemoveAt(i);
        }
    }

    private BulletSnapshot[] GetBulletSnapshots()
    {
        var snapshots = new BulletSnapshot[_bullets.Count];
        for (int i = 0; i < _bullets.Count; i++)
            snapshots[i] = _bullets[i].GetSnapshot();
        return snapshots;
    }

    private void CheckPhaseTransition()
    {
        if (_phase == GamePhase.WaitingForPlayers && _tanks.Count >= Constants.MinPlayersToStart)
        {
            _phase = GamePhase.InProgress;
            _logger.LogInformation("Game started with {Count} players", _tanks.Count);
        }
    }

    private void CheckWinCondition()
    {
        if (_phase != GamePhase.InProgress)
            return;

        int aliveCount = 0;
        int lastAliveId = -1;

        foreach (var (id, tank) in _tanks)
        {
            if (tank.IsAlive)
            {
                aliveCount++;
                lastAliveId = id;
            }
        }

        if (aliveCount <= 1)
        {
            _phase = GamePhase.GameOver;
            WinnerId = lastAliveId;
            _logger.LogInformation("Game over. Winner: {WinnerId}", WinnerId);
        }
    }
}

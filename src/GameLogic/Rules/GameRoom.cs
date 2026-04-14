using System;
using System.Collections.Generic;
using System.Numerics;
using Microsoft.Extensions.Logging;
using BattleTank.GameLogic.Entities;
using BattleTank.GameLogic.Network;
using BattleTank.GameLogic.Physics;
using BattleTank.GameLogic.Shared;

namespace BattleTank.GameLogic.Rules;

public readonly record struct Elimination(int EliminatedId, int KillerId);

public class GameRoom
{
    private static readonly Vector2[] PowerupSpawnPoints =
    [
        new(500, 500), new(250, 250), new(750, 250), new(250, 750), new(750, 750),
        new(500, 200), new(500, 800), new(200, 500), new(800, 500),
    ];

    private readonly ILogger<GameRoom> _logger;
    private readonly IBattleRules _rules;
    private readonly Dictionary<int, TankEntity> _tanks;
    private readonly Dictionary<int, string> _playerNicknames;
    private readonly Dictionary<int, int> _playerKills;
    private readonly Dictionary<int, int> _playerAccountIds;
    private readonly Dictionary<int, int> _playerTeams;
    private readonly Dictionary<int, int> _teamScores;
    private readonly Dictionary<int, InputFlags> _inputBuffer;
    private readonly Dictionary<int, uint> _lastInputSeq;
    private readonly Dictionary<int, uint> _lastFireTick;
    private readonly List<BulletEntity> _bullets;
    private readonly List<PowerupEntity> _powerups;
    private readonly List<ControlPoint> _controlPoints;
    private readonly Queue<(int PlayerId, uint RespawnTick)> _respawnQueue;
    private readonly ZoneController _zone;
    private readonly List<Elimination> _pendingEliminations;
    private readonly GameRoomState _state;
    private int _nextBulletId;
    private int _nextPowerupId;
    private uint _currentTick;
    private uint _countdownStartTick;
    private uint _gameStartTick;
    private uint _lastPowerupSpawnTick;
    private GamePhase _phase;

    private const uint FireCooldownTicks = 10; // 0.5s at 20 TPS

    public GamePhase Phase => _phase;
    public int WinnerId { get; private set; } = -1;
    public int WinnerTeamId { get; private set; } = -1;
    public int CountdownSecondsRemaining { get; private set; }
    public int GameDurationSeconds => (int)((_currentTick - _gameStartTick) / Constants.TickRate);
    public IReadOnlyDictionary<int, string> PlayerNicknames => _playerNicknames;
    public IReadOnlyDictionary<int, int> PlayerKills => _playerKills;
    public IReadOnlyDictionary<int, int> TeamScores => _teamScores;

    public GameRoom(ILogger<GameRoom> logger) : this(logger, new BattleRoyaleRules()) { }

    public GameRoom(ILogger<GameRoom> logger, IBattleRules rules)
    {
        _logger = logger;
        _rules = rules;
        _tanks = new Dictionary<int, TankEntity>();
        _playerNicknames = new Dictionary<int, string>();
        _playerKills = new Dictionary<int, int>();
        _playerAccountIds = new Dictionary<int, int>();
        _playerTeams = new Dictionary<int, int>();
        _teamScores = new Dictionary<int, int>();
        _inputBuffer = new Dictionary<int, InputFlags>();
        _lastInputSeq = new Dictionary<int, uint>();
        _lastFireTick = new Dictionary<int, uint>();
        _bullets = new List<BulletEntity>();
        _powerups = new List<PowerupEntity>();
        _controlPoints = new List<ControlPoint>();
        _respawnQueue = new Queue<(int, uint)>();
        _zone = new ZoneController();
        _pendingEliminations = new List<Elimination>();
        _phase = GamePhase.WaitingForPlayers;

        _state = new GameRoomState(
            _tanks, _playerNicknames, _playerKills, _playerTeams,
            _teamScores, _respawnQueue, _controlPoints);

        _rules.Initialize(_state);
    }

    public PlayerInfo[] GetLeaderboard() => _rules.GetLeaderboard(_state);

    public void SetPlayerAccountId(int playerId, int accountId)
        => _playerAccountIds[playerId] = accountId;

    public int GetPlayerAccountId(int playerId)
        => _playerAccountIds.TryGetValue(playerId, out int id) ? id : -1;

    public Result<TankEntity> AddPlayer(int playerId, string nickname = "")
    {
        if (_phase != GamePhase.WaitingForPlayers && _phase != GamePhase.Lobby)
            return Result<TankEntity>.Fail("Game already in progress");

        if (_tanks.Count >= Constants.MaxPlayersPerRoom)
            return Result<TankEntity>.Fail("Room is full");

        if (_tanks.ContainsKey(playerId))
            return Result<TankEntity>.Fail("Player already in room");

        // Rules assign team first (so GetSpawnPoint can use it)
        _rules.OnPlayerAdded(playerId, _state);

        var spawnPos = _rules.GetSpawnPoint(playerId, _state);
        var tank = new TankEntity(playerId, spawnPos);
        tank.TeamId = _playerTeams.TryGetValue(playerId, out int tid) ? tid : -1;

        _tanks[playerId] = tank;
        _playerNicknames[playerId] = string.IsNullOrWhiteSpace(nickname) ? $"Tank{playerId}" : nickname;
        _playerKills[playerId] = 0;
        _inputBuffer[playerId] = InputFlags.None;
        _lastInputSeq[playerId] = 0;
        _lastFireTick[playerId] = 0;

        _logger.LogInformation("Player {PlayerId} ({Nickname}) joined at {SpawnPos}", playerId, _playerNicknames[playerId], spawnPos);
        CheckPhaseTransition();

        return Result<TankEntity>.Ok(tank);
    }

    public void RemovePlayer(int playerId)
    {
        if (!_tanks.Remove(playerId))
            return;

        _playerNicknames.Remove(playerId);
        _playerKills.Remove(playerId);
        _playerAccountIds.Remove(playerId);
        _playerTeams.Remove(playerId);
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
        if (_phase == GamePhase.Lobby)
        {
            _currentTick++;
            uint ticksElapsed = _currentTick - _countdownStartTick;
            int ticksRemaining = Constants.LobbyCountdownTicks - (int)ticksElapsed;
            CountdownSecondsRemaining = Math.Max(0, ticksRemaining / Constants.TickRate);

            if (ticksElapsed >= (uint)Constants.LobbyCountdownTicks)
            {
                _phase = GamePhase.InProgress;
                _gameStartTick = _currentTick;
                _logger.LogInformation("Lobby countdown finished, game started with {Count} players", _tanks.Count);
            }
            return;
        }

        if (_phase != GamePhase.InProgress)
            return;

        foreach (var (id, tank) in _tanks)
        {
            if (!tank.IsAlive) continue;

            var flags = _inputBuffer[id];
            tank.ApplyInput(flags, deltaTime);
            tank.TickSpeedBoost(_currentTick);

            if ((flags & InputFlags.Fire) != 0)
                TryFire(id, tank);

            CollisionSystem.ClampTankToMap(tank);
            foreach (var wall in MapLayout.Walls)
                CollisionSystem.ResolveTankWallCollision(tank, wall);
        }

        TickBullets(deltaTime);

        if (_rules.UseShrinkingZone)
            TickZone(deltaTime);

        if (_rules.UsesPowerups)
            TickPowerups();

        _rules.OnTick(_currentTick, deltaTime, _state);

        _currentTick++;

        ProcessRespawnQueue();
        CheckWinCondition();
    }

    /// <summary>Returns pending eliminations since last call and clears the list.</summary>
    public IReadOnlyList<Elimination> GetAndClearEliminations()
    {
        if (_pendingEliminations.Count == 0)
            return [];

        var result = _pendingEliminations.ToArray();
        _pendingEliminations.Clear();
        return result;
    }

    public GameStateFull GetFullState()
    {
        var tankSnapshots = new TankSnapshot[_tanks.Count];
        int i = 0;
        foreach (var tank in _tanks.Values)
            tankSnapshots[i++] = tank.GetSnapshot();

        var bulletSnapshots = GetBulletSnapshots();
        var playerInfos = GetPlayerInfos();
        var powerupSnapshots = GetPowerupSnapshots();
        var controlPointSnapshots = GetControlPointSnapshots();

        return new GameStateFull(
            _currentTick, tankSnapshots, bulletSnapshots, _phase,
            _zone.GetSnapshot(), playerInfos, CountdownSecondsRemaining,
            powerupSnapshots, _rules.Mode, controlPointSnapshots);
    }

    public GameStateDelta GetDeltaState(uint lastAckedTick)
    {
        var tankSnapshots = new TankSnapshot[_tanks.Count];
        int i = 0;
        foreach (var tank in _tanks.Values)
            tankSnapshots[i++] = tank.GetSnapshot();

        var bulletSnapshots = GetBulletSnapshots();
        var powerupSnapshots = GetPowerupSnapshots();
        var controlPointSnapshots = GetControlPointSnapshots();

        return new GameStateDelta(
            _currentTick, lastAckedTick, tankSnapshots, bulletSnapshots,
            _zone.GetSnapshot(), powerupSnapshots, controlPointSnapshots);
    }

    public void Reset()
    {
        _tanks.Clear();
        _playerNicknames.Clear();
        _playerKills.Clear();
        _playerAccountIds.Clear();
        _playerTeams.Clear();
        _teamScores.Clear();
        _inputBuffer.Clear();
        _lastInputSeq.Clear();
        _lastFireTick.Clear();
        _bullets.Clear();
        _powerups.Clear();
        _controlPoints.Clear();
        _respawnQueue.Clear();
        _pendingEliminations.Clear();
        _zone.Reset();
        _nextBulletId = 0;
        _nextPowerupId = 0;
        _currentTick = 0;
        _countdownStartTick = 0;
        _gameStartTick = 0;
        _lastPowerupSpawnTick = 0;
        CountdownSecondsRemaining = 0;
        _phase = GamePhase.WaitingForPlayers;
        WinnerId = -1;
        WinnerTeamId = -1;
        _rules.Initialize(_state);
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

            bool hitWall = false;
            foreach (var wall in MapLayout.Walls)
            {
                if (CollisionSystem.BulletHitsWall(bullet, wall))
                {
                    bullet.Kill();
                    hitWall = true;
                    break;
                }
            }
            if (hitWall) continue;

            foreach (var tank in _tanks.Values)
            {
                if (!CollisionSystem.BulletHitsTank(bullet, tank))
                    continue;

                // Friendly fire check
                if (!_rules.IsFriendlyFireEnabled && tank.TeamId >= 0)
                {
                    bool sameTeam = _tanks.TryGetValue(bullet.OwnerId, out var shooter)
                        && shooter.TeamId == tank.TeamId;
                    if (sameTeam) continue;
                }

                bool wasAlive = tank.IsAlive;
                tank.TakeDamage(Constants.BulletDamage);
                bullet.Kill();
                _logger.LogDebug("Bullet {BulletId} hit tank {TankId}", bullet.Id, tank.Id);

                if (wasAlive && !tank.IsAlive)
                {
                    _pendingEliminations.Add(new Elimination(tank.Id, bullet.OwnerId));
                    _rules.OnElimination(tank.Id, bullet.OwnerId, _currentTick, _state);
                }

                break;
            }
        }

        for (int i = _bullets.Count - 1; i >= 0; i--)
        {
            if (!_bullets[i].IsAlive)
                _bullets.RemoveAt(i);
        }
    }

    private void TickZone(float deltaTime)
    {
        foreach (var tank in _tanks.Values)
        {
            if (!tank.IsAlive) continue;
            bool wasAlive = tank.IsAlive;
            _zone.Tick(deltaTime, [tank]);
            if (wasAlive && !tank.IsAlive)
            {
                _pendingEliminations.Add(new Elimination(tank.Id, -1));
                _rules.OnElimination(tank.Id, -1, _currentTick, _state);
            }
        }
    }

    private void ProcessRespawnQueue()
    {
        while (_respawnQueue.Count > 0 && _respawnQueue.Peek().RespawnTick <= _currentTick)
        {
            var (playerId, _) = _respawnQueue.Dequeue();
            if (_tanks.TryGetValue(playerId, out var tank))
            {
                var spawnPos = _rules.GetSpawnPoint(playerId, _state);
                tank.Respawn(spawnPos);
                _logger.LogInformation("Player {PlayerId} respawned at {SpawnPos}", playerId, spawnPos);
            }
        }
    }

    private BulletSnapshot[] GetBulletSnapshots()
    {
        var snapshots = new BulletSnapshot[_bullets.Count];
        for (int i = 0; i < _bullets.Count; i++)
            snapshots[i] = _bullets[i].GetSnapshot();
        return snapshots;
    }

    private ControlPointSnapshot[]? GetControlPointSnapshots()
    {
        if (_controlPoints.Count == 0)
            return null;

        var snapshots = new ControlPointSnapshot[_controlPoints.Count];
        for (int i = 0; i < _controlPoints.Count; i++)
            snapshots[i] = _controlPoints[i].GetSnapshot();
        return snapshots;
    }

    private void CheckPhaseTransition()
    {
        if (_phase == GamePhase.WaitingForPlayers && _tanks.Count >= Constants.MinPlayersToStart)
        {
            _phase = GamePhase.Lobby;
            _countdownStartTick = _currentTick;
            CountdownSecondsRemaining = Constants.LobbyCountdownTicks / Constants.TickRate;
            _logger.LogInformation("Lobby started with {Count} players, countdown {Seconds}s", _tanks.Count, CountdownSecondsRemaining);
        }
    }

    private void CheckWinCondition()
    {
        if (_phase != GamePhase.InProgress)
            return;

        var result = _rules.CheckWinCondition(_state);
        if (result == null)
            return;

        _phase = GamePhase.GameOver;
        WinnerId = result.WinnerPlayerId ?? -1;
        WinnerTeamId = result.WinnerTeamId ?? -1;
        _logger.LogInformation("Game over. Winner player: {WinnerId}, team: {WinnerTeamId}", WinnerId, WinnerTeamId);
    }

    private void TickPowerups()
    {
        if (_currentTick - _lastPowerupSpawnTick >= Constants.PowerupSpawnIntervalTicks)
        {
            _lastPowerupSpawnTick = _currentTick;
            var spawnPos = PowerupSpawnPoints[_nextPowerupId % PowerupSpawnPoints.Length];
            var type = (PowerupType)(_nextPowerupId % 3);
            _powerups.Add(new PowerupEntity(_nextPowerupId++, spawnPos, type));
        }

        float pickupDist = Constants.PowerupRadius + Constants.TankRadius;
        for (int i = 0; i < _powerups.Count; i++)
        {
            var powerup = _powerups[i];
            if (powerup.IsPickedUp) continue;

            foreach (var tank in _tanks.Values)
            {
                if (!tank.IsAlive) continue;

                float dx = tank.Position.X - powerup.Position.X;
                float dy = tank.Position.Y - powerup.Position.Y;
                float dist = MathF.Sqrt(dx * dx + dy * dy);

                if (dist < pickupDist)
                {
                    powerup.PickUp();
                    ApplyPowerup(tank, powerup.Type);
                    _logger.LogDebug("Player {Id} picked up {Type}", tank.Id, powerup.Type);
                    break;
                }
            }
        }

        for (int i = _powerups.Count - 1; i >= 0; i--)
        {
            if (_powerups[i].IsPickedUp)
                _powerups.RemoveAt(i);
        }
    }

    private void ApplyPowerup(TankEntity tank, PowerupType type)
    {
        switch (type)
        {
            case PowerupType.ExtraAmmo:
                _lastFireTick[tank.Id] = 0;
                break;
            case PowerupType.Shield:
                tank.Heal(Constants.ShieldHealAmount);
                break;
            case PowerupType.SpeedBoost:
                tank.ApplySpeedBoost(_currentTick + Constants.SpeedBoostDurationTicks);
                break;
        }
    }

    private PlayerInfo[] GetPlayerInfos()
    {
        var infos = new PlayerInfo[_tanks.Count];
        int i = 0;
        foreach (var (id, _) in _tanks)
        {
            var nickname = _playerNicknames.TryGetValue(id, out var n) ? n : $"Tank{id}";
            var kills = _playerKills.TryGetValue(id, out var k) ? k : 0;
            int teamId = _playerTeams.TryGetValue(id, out var t) ? t : -1;
            infos[i++] = new PlayerInfo(id, nickname, kills, teamId);
        }
        return infos;
    }

    private PowerupSnapshot[] GetPowerupSnapshots()
    {
        var snapshots = new PowerupSnapshot[_powerups.Count];
        for (int i = 0; i < _powerups.Count; i++)
            snapshots[i] = _powerups[i].GetSnapshot();
        return snapshots;
    }
}

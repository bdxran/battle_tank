using System;
using System.Collections.Generic;
using System.Numerics;
using Microsoft.Extensions.Logging;
using BattleTank.GameLogic.AI;
using BattleTank.GameLogic.Entities;
using BattleTank.GameLogic.Network;
using BattleTank.GameLogic.Physics;
using BattleTank.GameLogic.Shared;

namespace BattleTank.GameLogic.Rules;

public readonly record struct Elimination(int EliminatedId, int KillerId);

public partial class GameRoom
{
    private static readonly Vector2[] PowerupSpawnPoints =
    [
        new(500, 500), new(250, 250), new(750, 250), new(250, 750), new(750, 750),
        new(500, 200), new(500, 800), new(200, 500), new(800, 500),
    ];

    private sealed class PlayerSession
    {
        public int AccountId;
        public InputFlags InputBuffer;
        public uint LastInputSeq;
        public uint LastFireTick;
    }

    private readonly ILogger<GameRoom> _logger;
    private readonly IBattleRules _rules;
    private readonly Dictionary<int, IBot> _bots = new();
    private readonly HashSet<int> _botIds = new();
    private int _nextBotId = -1; // bots use negative IDs to avoid collision with ENet peer IDs
    private readonly Dictionary<int, TankEntity> _tanks;
    private readonly Dictionary<int, string> _playerNicknames;
    private readonly Dictionary<int, int> _playerKills;
    private readonly Dictionary<int, int> _playerDeaths;
    private readonly Dictionary<int, int> _playerAssists;
    private readonly Dictionary<int, int> _playerZoneCaptured;
    private readonly Dictionary<int, HashSet<int>> _damageContributors;
    private readonly Dictionary<int, int> _playerTeams;
    private readonly Dictionary<int, int> _teamScores;
    private readonly Dictionary<int, PlayerSession> _playerSessions;
    private readonly List<BulletEntity> _bullets;
    private readonly List<PowerupEntity> _powerups;
    private readonly List<ControlPoint> _controlPoints;
    private readonly Queue<(int PlayerId, uint RespawnTick)> _respawnQueue;
    private readonly ZoneController _zone;
    private readonly List<Elimination> _pendingEliminations;
    private readonly GameRoomState _state;
    private readonly Random _random;
    private int _nextBulletId;
    private int _nextPowerupId;
    private uint _currentTick;
    private uint _countdownStartTick;
    private uint _gameStartTick;
    private uint _lastPowerupSpawnTick;
    private GamePhase _phase;


    public GamePhase Phase => _phase;
    public int WinnerId { get; private set; } = -1;
    public int WinnerTeamId { get; private set; } = -1;
    public int CountdownSecondsRemaining { get; private set; }
    public int GameDurationSeconds => (int)((_currentTick - _gameStartTick) / Constants.TickRate);
    public IReadOnlyDictionary<int, string> PlayerNicknames => _playerNicknames;
    public IReadOnlyDictionary<int, int> PlayerKills => _playerKills;
    public IReadOnlyDictionary<int, int> TeamScores => _teamScores;

    public GameRoom(ILogger<GameRoom> logger) : this(logger, new BattleRoyaleRules(), null) { }

    public GameRoom(ILogger<GameRoom> logger, IBattleRules rules) : this(logger, rules, null) { }

    public GameRoom(ILogger<GameRoom> logger, IBattleRules rules, Random? random)
    {
        _logger = logger;
        _rules = rules;
        _random = random ?? new Random();
        _tanks = new Dictionary<int, TankEntity>();
        _playerNicknames = new Dictionary<int, string>();
        _playerKills = new Dictionary<int, int>();
        _playerDeaths = new Dictionary<int, int>();
        _playerAssists = new Dictionary<int, int>();
        _playerZoneCaptured = new Dictionary<int, int>();
        _damageContributors = new Dictionary<int, HashSet<int>>();
        _playerTeams = new Dictionary<int, int>();
        _teamScores = new Dictionary<int, int>();
        _playerSessions = new Dictionary<int, PlayerSession>();
        _bullets = new List<BulletEntity>();
        _powerups = new List<PowerupEntity>();
        _controlPoints = new List<ControlPoint>();
        _respawnQueue = new Queue<(int, uint)>();
        _zone = new ZoneController();
        _pendingEliminations = new List<Elimination>();
        _phase = GamePhase.WaitingForPlayers;

        _state = new GameRoomState(
            _tanks, _playerNicknames, _playerKills, _playerDeaths,
            _playerAssists, _playerZoneCaptured,
            _playerTeams, _teamScores, _respawnQueue, _controlPoints);

        _rules.Initialize(_state);
    }

    public PlayerInfo[] GetLeaderboard() => _rules.GetLeaderboard(_state);

    public void SetPlayerAccountId(int playerId, int accountId)
    {
        if (_playerSessions.TryGetValue(playerId, out var session))
            session.AccountId = accountId;
    }

    public int GetPlayerAccountId(int playerId)
        => _playerSessions.TryGetValue(playerId, out var session) ? session.AccountId : -1;

    public int GetPlayerTeamId(int playerId)
        => _playerTeams.TryGetValue(playerId, out int teamId) ? teamId : -1;

    /// <summary>Returns true if the given player ID belongs to a bot.</summary>
    public bool IsBot(int playerId) => _botIds.Contains(playerId);

    /// <summary>
    /// Adds a bot-controlled player. Returns the assigned bot player ID (negative).
    /// </summary>
    public Result<int> AddBot(string nickname = "")
    {
        int botId = _nextBotId--;
        string botNickname = string.IsNullOrWhiteSpace(nickname) ? $"Bot{-botId}[BOT]" : nickname;

        var result = AddPlayer(botId, botNickname);
        if (!result.IsSuccess)
            return Result<int>.Fail(result.Error);

        _botIds.Add(botId);
        _bots[botId] = new SimpleBot(botId);
        _logger.LogInformation("Bot {BotId} ({Nickname}) added", botId, botNickname);
        return Result<int>.Ok(botId);
    }

    /// <summary>
    /// Adds a human player to the room. Returns the created <see cref="TankEntity"/> on success,
    /// or a failure result if the room is full or the game is already in progress.
    /// </summary>
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
        _playerDeaths[playerId] = 0;
        _playerSessions[playerId] = new PlayerSession();

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
        _playerDeaths.Remove(playerId);
        _playerTeams.Remove(playerId);
        _playerSessions.Remove(playerId);
        _botIds.Remove(playerId);
        _bots.Remove(playerId);

        _logger.LogInformation("Player {PlayerId} removed", playerId);

        if (_phase == GamePhase.InProgress)
            CheckWinCondition();
    }

    public void ApplyInput(int playerId, PlayerInput input)
    {
        if (!_playerSessions.TryGetValue(playerId, out var session))
            return;

        if (input.SequenceNumber <= session.LastInputSeq)
            return;

        session.InputBuffer = input.Flags;
        session.LastInputSeq = input.SequenceNumber;
    }

    /// <summary>
    /// Advances the game simulation by one tick. Should be called at <see cref="Constants.TickRate"/> Hz.
    /// Handles player input, bullet physics, zone shrink, powerups, respawn queue, and win condition checks.
    /// </summary>
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

        // Compute and apply bot inputs before the main tank loop
        foreach (var (botId, bot) in _bots)
        {
            if (_playerSessions.TryGetValue(botId, out var botSession) && _tanks.TryGetValue(botId, out var botTank) && botTank.IsAlive)
                botSession.InputBuffer = bot.ComputeInput(_tanks, _controlPoints, _currentTick);
        }

        foreach (var (id, tank) in _tanks)
        {
            if (!tank.IsAlive) continue;
            try
            {
                var session = _playerSessions[id];
                var flags = session.InputBuffer;
                tank.ApplyInput(flags, deltaTime);
                tank.TickSpeedBoost(_currentTick);
                tank.TickInvincibility(_currentTick);

                if ((flags & InputFlags.Fire) != 0)
                    TryFire(session, tank);

                CollisionSystem.ClampTankToMap(tank);
                foreach (var wall in MapLayout.Walls)
                    CollisionSystem.ResolveTankWallCollision(tank, wall);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception processing tank {TankId} — skipping", id);
            }
        }

        var aliveTanks = new System.Collections.Generic.List<TankEntity>(_tanks.Values.Count);
        foreach (var t in _tanks.Values)
            if (t.IsAlive) aliveTanks.Add(t);
        for (int i = 0; i < aliveTanks.Count; i++)
            for (int j = i + 1; j < aliveTanks.Count; j++)
                CollisionSystem.ResolveTankTankCollision(aliveTanks[i], aliveTanks[j]);

        try { TickBullets(deltaTime); }
        catch (Exception ex) { _logger.LogError(ex, "Unhandled exception in TickBullets — skipping"); }

        if (_rules.UseShrinkingZone)
        {
            try { TickZone(deltaTime); }
            catch (Exception ex) { _logger.LogError(ex, "Unhandled exception in TickZone — skipping"); }
        }

        if (_rules.UsesPowerups)
        {
            try { TickPowerups(); }
            catch (Exception ex) { _logger.LogError(ex, "Unhandled exception in TickPowerups — skipping"); }
        }

        try { _rules.OnTick(_currentTick, deltaTime, _state); }
        catch (Exception ex) { _logger.LogError(ex, "Unhandled exception in OnTick — skipping"); }

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

    public void Reset()
    {
        _tanks.Clear();
        _playerNicknames.Clear();
        _playerKills.Clear();
        _playerDeaths.Clear();
        _playerAssists.Clear();
        _playerZoneCaptured.Clear();
        _damageContributors.Clear();
        _playerTeams.Clear();
        _teamScores.Clear();
        _playerSessions.Clear();
        _bots.Clear();
        _botIds.Clear();
        _nextBotId = -1;
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

    private void TickZone(float deltaTime)
    {
        // Snapshot alive state before zone tick to detect kills
        var wasAlive = new Dictionary<int, bool>(_tanks.Count);
        foreach (var (id, tank) in _tanks)
            wasAlive[id] = tank.IsAlive;

        // Tick zone once with all tanks (not once per tank — would multiply deltaTime accumulation)
        _zone.Tick(deltaTime, _tanks.Values);

        foreach (var (id, tank) in _tanks)
        {
            if (wasAlive.GetValueOrDefault(id) && !tank.IsAlive)
            {
                _pendingEliminations.Add(new Elimination(id, -1));
                _rules.OnElimination(id, -1, _currentTick, _state);
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
                tank.Respawn(spawnPos, _currentTick);
                if (_playerSessions.TryGetValue(playerId, out var session))
                    session.InputBuffer = InputFlags.None;
                _logger.LogInformation("Player {PlayerId} respawned at {SpawnPos}", playerId, spawnPos);
            }
        }
    }

    /// <summary>
    /// Skips the lobby countdown and starts the game immediately.
    /// Intended for local/offline games where the countdown is handled externally (e.g. Godot CountdownNode).
    /// </summary>
    public void ForceStart()
    {
        if (_phase != GamePhase.Lobby) return;
        _phase = GamePhase.InProgress;
        _gameStartTick = _currentTick;
    }

    private void CheckPhaseTransition()
    {
        if (_phase == GamePhase.WaitingForPlayers && _tanks.Count >= _rules.MinPlayersToStart)
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

}

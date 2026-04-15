using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Microsoft.Extensions.Logging;
using BattleTank.GameLogic.Network;
using BattleTank.GameLogic.Persistence;
using BattleTank.GameLogic.Rules;
using BattleTank.GameLogic.AI;
using BattleTank.GameLogic.Shared;

namespace BattleTank.Godot.Nodes;

public partial class GameRoomNode : Node
{
    private const float TickInterval = 1f / Constants.TickRate;
    private const int MaxTicksPerFrame = 5;

    private ILogger<GameRoomNode> _logger = null!;
    private GameRoom _room = null!;
    private Network.ServerNetworkManager _network = null!;
    private IPlayerRepository _repository = null!;
    private ILeaderboardService _leaderboard = null!;
    private float _accumulator;
    private int _ticksSinceMetrics;
    private const int MetricsIntervalTicks = 100; // every 5 s at 20 TPS

    // Optional room code; if set, clients must supply matching code to join
    public string? RoomCode { get; set; }

    // Whether the current room is a training session (no auth, no stats, bot fill)
    private bool _isTrainingMode;

    // How many bots to fill when lobby countdown ends (0 = no fill)
    private int _botFillCount = Constants.MaxPlayersPerRoom;
    private bool _botsFilled;

    // Tracks peers that connected but haven't authenticated yet (peerId → connection timestamp)
    private readonly Dictionary<int, double> _pendingAuth = new();
    private const double AuthTimeoutSeconds = 30.0;
    // Maps peerId → (accountId, nickname)
    private readonly Dictionary<int, (int AccountId, string Nickname)> _authenticated = new();

    // Rate limiting: tracks failed auth attempts per peer
    private readonly Dictionary<int, int> _authAttempts = new();
    private const int MaxAuthAttemptsPerPeer = 5;

    public void Initialize(
        Network.ServerNetworkManager network,
        ILogger<GameRoomNode> logger,
        ILogger<GameRoom> roomLogger,
        IPlayerRepository repository,
        ILeaderboardService leaderboard,
        bool trainingMode = false,
        int botFillCount = Constants.MaxPlayersPerRoom)
    {
        _logger = logger;
        _network = network;
        _repository = repository;
        _leaderboard = leaderboard;
        _isTrainingMode = trainingMode;
        _botFillCount = trainingMode ? Constants.MaxPlayersPerRoom - 1 : botFillCount;

        IBattleRules rules = trainingMode
            ? new TrainingRules()
            : new BattleRoyaleRules();
        _room = new GameRoom(roomLogger, rules);

        _network.PlayerConnected += OnPlayerConnected;
        _network.PlayerDisconnected += OnPlayerDisconnected;
        _network.InputReceived += OnInputReceived;
        _network.LoginReceived += OnLoginReceived;
        _network.RegisterReceived += OnRegisterReceived;
        _network.LeaderboardRequested += OnLeaderboardRequested;
        _network.JoinTrainingReceived += OnJoinTrainingReceived;
    }

    public override void _Process(double delta)
    {
        if (_room is null) return;

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

    public override void _ExitTree()
    {
        if (_network is null) return;
        _network.PlayerConnected -= OnPlayerConnected;
        _network.PlayerDisconnected -= OnPlayerDisconnected;
        _network.InputReceived -= OnInputReceived;
        _network.LoginReceived -= OnLoginReceived;
        _network.RegisterReceived -= OnRegisterReceived;
        _network.LeaderboardRequested -= OnLeaderboardRequested;
        _network.JoinTrainingReceived -= OnJoinTrainingReceived;
    }

    private void OnPlayerConnected(int peerId)
    {
        _pendingAuth[peerId] = Time.GetUnixTimeFromSystem();
        _logger.LogInformation("Peer {PeerId} connected, awaiting authentication", peerId);
    }

    private void OnPlayerDisconnected(int peerId)
    {
        _pendingAuth.Remove(peerId);
        _authenticated.Remove(peerId);
        _authAttempts.Remove(peerId);
        _room.RemovePlayer(peerId);

        if (_room.Phase == GamePhase.GameOver)
            BroadcastGameOver();
    }

    private void OnInputReceived(int peerId, PlayerInput input)
    {
        if (!_authenticated.ContainsKey(peerId)) return;
        _room.ApplyInput(peerId, input);
    }

    private void OnLoginReceived(int peerId, LoginRequest request)
    {
        if (IsRateLimited(peerId)) return;
        _ = HandleLoginAsync(peerId, request);
    }

    private void OnRegisterReceived(int peerId, RegisterRequest request)
    {
        if (IsRateLimited(peerId)) return;
        _ = HandleRegisterAsync(peerId, request);
    }

    private bool IsRateLimited(int peerId)
    {
        _authAttempts.TryGetValue(peerId, out int attempts);
        if (attempts < MaxAuthAttemptsPerPeer)
        {
            _authAttempts[peerId] = attempts + 1;
            return false;
        }
        _logger.LogWarning("Rate limit: peer {PeerId} exceeded {Max} auth attempts — disconnecting", peerId, MaxAuthAttemptsPerPeer);
        _network.DisconnectPeer(peerId);
        return true;
    }

    private void OnLeaderboardRequested(int peerId, GameMode mode)
    {
        _ = HandleLeaderboardRequestAsync(peerId, mode);
    }

    private void OnJoinTrainingReceived(int peerId, JoinTrainingRequest request)
    {
        if (!_isTrainingMode)
        {
            _logger.LogWarning("Peer {PeerId} sent JoinTraining but this is not a training room", peerId);
            return;
        }

        if (_authenticated.ContainsKey(peerId))
            return; // already joined

        if (!string.IsNullOrEmpty(RoomCode) && request.RoomCode != RoomCode)
        {
            var fail = new LoginResponse(false, -1, "", "", "Code incorrect");
            _network.SendToPlayerReliable(peerId, new NetworkMessage(
                MessageType.LoginResponse,
                GameStateSerializer.Serialize(fail)));
            _logger.LogWarning("Peer {PeerId} supplied wrong room code", peerId);
            return;
        }

        string displayNick = string.IsNullOrWhiteSpace(request.Nickname) ? $"Trainee{peerId}" : request.Nickname;
        AuthenticatePeer(peerId, accountId: -1, displayNick, avatarSeed: "");
    }

    private async Task HandleLoginAsync(int peerId, LoginRequest request)
    {
        try
        {
            if (!string.IsNullOrEmpty(RoomCode) && request.RoomCode != RoomCode)
            {
                var codeFailure = new LoginResponse(false, -1, "", "", "Code incorrect");
                _network.SendToPlayerReliable(peerId, new NetworkMessage(
                    MessageType.LoginResponse,
                    GameStateSerializer.Serialize(codeFailure)));
                _logger.LogWarning("Peer {PeerId} supplied wrong room code", peerId);
                return;
            }

            var account = await _repository.FindByUsernameAsync(request.Username);
            bool passwordValid = account != null && await Task.Run(
                () => BCrypt.Net.BCrypt.Verify(request.Password, account.PasswordHash));
            if (!passwordValid)
            {
                var fail = new LoginResponse(false, -1, "", "", "Invalid username or password");
                _network.SendToPlayerReliable(peerId, new NetworkMessage(
                    MessageType.LoginResponse,
                    GameStateSerializer.Serialize(fail)));
                _logger.LogWarning("Login failed for peer {PeerId} (username: {Username})", peerId, request.Username);
                return;
            }

            AuthenticatePeer(peerId, account!.AccountId, account.Username, account.AvatarSeed);

            var response = new LoginResponse(true, account.AccountId, account.Username, account.AvatarSeed);
            _network.SendToPlayerReliable(peerId, new NetworkMessage(
                MessageType.LoginResponse,
                GameStateSerializer.Serialize(response)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for peer {PeerId}", peerId);
        }
    }

    private async Task HandleRegisterAsync(int peerId, RegisterRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                SendRegisterError(peerId, "Username and password are required");
                return;
            }

            var existing = await _repository.FindByUsernameAsync(request.Username);
            if (existing != null)
            {
                SendRegisterError(peerId, "Username already taken");
                return;
            }

            var hash = await Task.Run(() => BCrypt.Net.BCrypt.HashPassword(request.Password));
            var avatarSeed = request.Username.GetHashCode().ToString("x8");
            var account = await _repository.CreateAccountAsync(request.Username, hash, avatarSeed);

            var response = new RegisterResponse(true, account.AccountId);
            _network.SendToPlayerReliable(peerId, new NetworkMessage(
                MessageType.RegisterResponse,
                GameStateSerializer.Serialize(response)));

            AuthenticatePeer(peerId, account.AccountId, account.Username, account.AvatarSeed);

            var loginResponse = new LoginResponse(true, account.AccountId, account.Username, account.AvatarSeed);
            _network.SendToPlayerReliable(peerId, new NetworkMessage(
                MessageType.LoginResponse,
                GameStateSerializer.Serialize(loginResponse)));

            _logger.LogInformation("Registered new account {Username} (accountId: {AccountId})", account.Username, account.AccountId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for peer {PeerId}", peerId);
            SendRegisterError(peerId, "Registration failed");
        }
    }

    private async Task HandleLeaderboardRequestAsync(int peerId, GameMode mode)
    {
        try
        {
            var entries = await _leaderboard.GetLeaderboardAsync(mode);
            var msgEntries = new LeaderboardEntryMessage[entries.Length];
            for (int i = 0; i < entries.Length; i++)
            {
                var e = entries[i];
                msgEntries[i] = new LeaderboardEntryMessage(e.AccountId, e.Username, e.Wins, e.Kills, e.GamesPlayed);
            }

            var response = new LeaderboardResponse(mode.ToString(), msgEntries);
            _network.SendToPlayerReliable(peerId, new NetworkMessage(
                MessageType.LeaderboardResponse,
                GameStateSerializer.Serialize(response)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching leaderboard for peer {PeerId}", peerId);
        }
    }

    private void AuthenticatePeer(int peerId, int accountId, string nickname, string avatarSeed)
    {
        _pendingAuth.Remove(peerId);
        _authenticated[peerId] = (accountId, nickname);

        var result = _room.AddPlayer(peerId, nickname);
        if (!result.IsSuccess)
        {
            _logger.LogWarning("Could not add authenticated player {PeerId}: {Error}", peerId, result.Error);
            return;
        }

        _room.SetPlayerAccountId(peerId, accountId);

        var fullState = _room.GetFullState();
        _network.SendToPlayer(peerId, new NetworkMessage(
            MessageType.GameStateFull,
            GameStateSerializer.Serialize(fullState)));

        var joined = new PlayerJoinedMessage(peerId, nickname);
        _network.Broadcast(new NetworkMessage(
            MessageType.PlayerJoined,
            GameStateSerializer.Serialize(joined)));

        _logger.LogInformation("Player {PeerId} authenticated as {Nickname} (accountId: {AccountId})", peerId, nickname, accountId);
    }

    private void SendRegisterError(int peerId, string message)
    {
        var response = new RegisterResponse(false, -1, message);
        _network.SendToPlayerReliable(peerId, new NetworkMessage(
            MessageType.RegisterResponse,
            GameStateSerializer.Serialize(response)));
    }

    private void FillBotsIfNeeded()
    {
        if (_botFillCount <= 0) return;

        int humanCount = _authenticated.Count;
        int slotsToFill = Math.Min(_botFillCount, Constants.MaxPlayersPerRoom - humanCount);

        for (int i = 0; i < slotsToFill; i++)
        {
            var result = _room.AddBot();
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Could not add bot: {Error}", result.Error);
                break;
            }

            int botId = result.Value;
            var nickname = _room.PlayerNicknames[botId];
            var joined = new PlayerJoinedMessage(botId, nickname);
            _network.Broadcast(new NetworkMessage(
                MessageType.PlayerJoined,
                GameStateSerializer.Serialize(joined)));
        }

        if (slotsToFill > 0)
            _logger.LogInformation("Filled {Count} bot slot(s)", slotsToFill);
    }

    private void EmitMetrics()
    {
        _logger.LogInformation("[metrics] players={PlayerCount} phase={Phase}",
            _authenticated.Count, _room.Phase);

        foreach (var (peerId, _) in _authenticated)
        {
            int rtt = _network.GetPeerRtt(peerId);
            if (rtt >= 0)
                _logger.LogInformation("[metrics] peer={PeerId} rtt_ms={Rtt}", peerId, rtt);
        }
    }

    private void EvictStalePendingAuth()
    {
        double now = Time.GetUnixTimeFromSystem();
        var stale = new System.Collections.Generic.List<int>();
        foreach (var kv in _pendingAuth)
        {
            if (now - kv.Value > AuthTimeoutSeconds)
                stale.Add(kv.Key);
        }
        foreach (var id in stale)
        {
            _logger.LogWarning("Peer {PeerId} auth timeout — disconnecting", id);
            _pendingAuth.Remove(id);
            _network.DisconnectPeer(id);
        }
    }

    private void DoTick()
    {
        EvictStalePendingAuth();

        var phaseBefore = _room.Phase;
        _room.Tick(TickInterval);

        // Fill empty slots with bots once, right after the game starts
        if (!_botsFilled && phaseBefore == GamePhase.Lobby && _room.Phase == GamePhase.InProgress)
        {
            _botsFilled = true;
            FillBotsIfNeeded();
        }

        _ticksSinceMetrics++;
        if (_ticksSinceMetrics >= MetricsIntervalTicks)
        {
            _ticksSinceMetrics = 0;
            EmitMetrics();
        }

        foreach (var elim in _room.GetAndClearEliminations())
        {
            var msg = new PlayerEliminatedMessage(elim.EliminatedId, elim.KillerId);
            _network.Broadcast(new NetworkMessage(
                MessageType.PlayerEliminated,
                GameStateSerializer.Serialize(msg)));
            _logger.LogInformation("Player {Eliminated} eliminated by {Killer}", elim.EliminatedId, elim.KillerId);
        }

        if (_room.Phase == GamePhase.GameOver)
        {
            BroadcastGameOver();
            var snapshot = CaptureGameResultSnapshot();
            _room.Reset();
            _botsFilled = false;
            _ = SaveGameResultsAsync(snapshot);
            return;
        }

        _network.Broadcast(new NetworkMessage(
            MessageType.GameStateDelta,
            GameStateSerializer.Serialize(_room.GetDeltaState(0))));
    }

    private void BroadcastGameOver()
    {
        var msg = new GameOverMessage(_room.WinnerId, _room.GetLeaderboard());
        _network.Broadcast(new NetworkMessage(
            MessageType.GameOver,
            GameStateSerializer.Serialize(msg)));

        _logger.LogInformation("Game over broadcast. Winner: {WinnerId}", _room.WinnerId);
    }

    private readonly record struct PlayerResult(int AccountId, bool Won, int Kills);
    private readonly record struct GameResultSnapshot(GameMode Mode, int DurationSeconds, PlayerResult[] Results);

    private GameResultSnapshot CaptureGameResultSnapshot()
    {
        var mode = _room.GetFullState().Mode;
        var duration = _room.GameDurationSeconds;
        var winnerId = _room.WinnerId;
        var winnerTeamId = _room.WinnerTeamId;
        var kills = _room.PlayerKills;

        var results = new System.Collections.Generic.List<PlayerResult>();
        foreach (var (peerId, _) in _authenticated)
        {
            var accountId = _room.GetPlayerAccountId(peerId);
            if (accountId < 0) continue;

            bool won = winnerTeamId >= 0
                ? _room.GetPlayerTeamId(peerId) == winnerTeamId
                : peerId == winnerId;
            int playerKills = kills.TryGetValue(peerId, out int k) ? k : 0;
            results.Add(new PlayerResult(accountId, won, playerKills));
        }

        return new GameResultSnapshot(mode, duration, results.ToArray());
    }

    private async Task SaveGameResultsAsync(GameResultSnapshot snapshot)
    {
        // Training mode: no stats persisted
        if (_isTrainingMode) return;

        foreach (var result in snapshot.Results)
        {
            try
            {
                await _repository.UpdateStatsAsync(result.AccountId, snapshot.Mode, result.Won, result.Kills, snapshot.DurationSeconds);
                _logger.LogInformation("Stats saved for account {AccountId} — won={Won} kills={Kills}", result.AccountId, result.Won, result.Kills);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save stats for account {AccountId}", result.AccountId);
            }
        }
    }
}

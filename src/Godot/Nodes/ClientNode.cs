using Godot;
using Microsoft.Extensions.Logging;
using BattleTank.GameLogic.Network;
using BattleTank.GameLogic.Shared;
using BattleTank.Godot.CrashReport;
using BattleTank.Godot.Network;
using BattleTank.Godot.Renderer;
using BattleTank.Godot.UI;

namespace BattleTank.Godot.Nodes;

/// <summary>
/// Root node for the game client. Add to the client scene.
/// Wires ClientNetworkManager → GameRenderer + HudNode + GameOverScreen.
/// Reads keyboard input and sends PlayerInput every frame.
/// </summary>
public partial class ClientNode : Node
{
    private enum GamePhase { Connecting, Lobby, InGame, GameOver }

    [Export] public string ServerAddress { get; set; } = "127.0.0.1";
    [Export] public int ServerPort { get; set; } = Constants.ServerPort;

    private ClientNetworkManager _network = null!;
    private GameRenderer _renderer = null!;
    private HudNode _hud = null!;
    private GameOverScreen _gameOverScreen = null!;
    private LoginScreen _loginScreen = null!;
    private TrainingOverlayNode _trainingOverlay = null!;
    private SpectatorOverlayNode _spectatorOverlay = null!;
    private AudioManagerNode _audioManager = null!;
    private CrashReportScreen _crashReportScreen = null!;
    private CrashReporter _crashReporter = null!;
    private int _localPlayerId;
    private int _accountId = -1;
    private string _nickname = "";
    private uint _inputSequence;
    private bool _eliminated;
    private bool _spectating;
    private bool _authenticated;
    private bool _trainingMode;
    private GamePhase _gamePhase = GamePhase.Connecting;

    public override void _Ready()
    {
        _crashReporter = new CrashReporter();
        _crashReporter.Initialize(() => _gamePhase.ToString());
        _crashReporter.PendingReportsFound += OnPendingReportsFound;

        var loggerFactory = new GodotLoggerFactory(_crashReporter);

        _network = new ClientNetworkManager();
        _network.Initialize(loggerFactory.CreateLogger<ClientNetworkManager>());
        AddChild(_network);

        _hud = new HudNode();
        AddChild(_hud);

        _gameOverScreen = new GameOverScreen();
        AddChild(_gameOverScreen);

        _renderer = new GameRenderer();
        AddChild(_renderer);

        _loginScreen = new LoginScreen();
        _loginScreen.LoginRequested += OnLoginRequested;
        _loginScreen.RegisterRequested += OnRegisterRequested;
        _loginScreen.TrainingRequested += OnTrainingRequested;
        AddChild(_loginScreen);

        _trainingOverlay = new TrainingOverlayNode();
        _trainingOverlay.JoinRankedRequested += OnJoinRankedRequested;
        _trainingOverlay.QuitRequested += OnQuitRequested;
        AddChild(_trainingOverlay);

        _spectatorOverlay = new SpectatorOverlayNode();
        AddChild(_spectatorOverlay);

        _audioManager = new AudioManagerNode();
        AddChild(_audioManager);

        var mailer = new CrashReportMailer();
        _crashReportScreen = new CrashReportScreen();
        _crashReportScreen.Initialize(_crashReporter, mailer);
        AddChild(_crashReportScreen);

        _network.ConnectedToServer += OnConnected;
        _network.DisconnectedFromServer += OnDisconnected;
        _network.PlayerEliminated += OnPlayerEliminated;
        _network.GameOver += OnGameOver;
        _network.LoginResponseReceived += OnLoginResponse;
        _network.RegisterResponseReceived += OnRegisterResponse;
        _network.LeaderboardResponseReceived += OnLeaderboardResponse;
        _network.GameStateDeltaReceived += OnGameStateDeltaForSpectator;
        _network.GameStateFullReceived += OnGameStateFullReceived;

        _loginScreen.Show();

        _crashReporter.CheckPendingReports();

        var error = _network.Connect(ServerAddress, ServerPort);
        if (error != Error.Ok)
            GD.PrintErr($"[ClientNode] Failed to connect: {error}");
        else
            GD.Print($"[ClientNode] Connecting to {ServerAddress}:{ServerPort}");
    }

    public override void _Process(double delta)
    {
        if (!_network.IsConnected() || !_authenticated || _eliminated) return;

        var flags = ReadInput();
        if (flags != InputFlags.None)
        {
            _inputSequence++;
            _network.SendInput(new PlayerInput(
                _localPlayerId,
                flags,
                _inputSequence));
        }
    }

    public override void _ExitTree()
    {
        if (_network is null) return;
        _network.ConnectedToServer -= OnConnected;
        _network.DisconnectedFromServer -= OnDisconnected;
        _network.PlayerEliminated -= OnPlayerEliminated;
        _network.GameOver -= OnGameOver;
        _network.LoginResponseReceived -= OnLoginResponse;
        _network.RegisterResponseReceived -= OnRegisterResponse;
        _network.LeaderboardResponseReceived -= OnLeaderboardResponse;
        _network.GameStateDeltaReceived -= OnGameStateDeltaForSpectator;
        _network.GameStateFullReceived -= OnGameStateFullReceived;
        _network.Disconnect();
    }

    private void OnPendingReportsFound(string[] paths)
    {
        if (paths.Length > 0)
            _crashReportScreen.ShowCrash(paths[0]);
    }

    private void OnConnected()
    {
        _localPlayerId = Multiplayer.GetUniqueId();
        _gamePhase = GamePhase.Lobby;
        GD.Print($"[ClientNode] Connected as peer {_localPlayerId}");
        _renderer.Initialize(_network, _hud, _localPlayerId);
        _audioManager.Initialize(_network, _renderer);
        _loginScreen.OnConnected();
    }

    private void OnDisconnected()
    {
        _authenticated = false;
        _gamePhase = GamePhase.Connecting;
        GD.Print("[ClientNode] Disconnected from server");
    }

    private void OnTrainingRequested()
    {
        if (!_network.IsConnected()) return;

        _trainingMode = true;
        string guestNick = $"Joueur{_localPlayerId}";
        _network.SendJoinTraining(new JoinTrainingRequest(guestNick));
        // Authenticated state is set once the server responds with GameStateFull
    }

    private void OnGameStateFullReceived(GameStateFull state)
    {
        if (!_trainingMode || _authenticated) return;

        // Training mode: server accepted the JoinTraining — consider us in-game
        _authenticated = true;
        _gamePhase = GamePhase.InGame;
        _loginScreen.Hide();
        _trainingOverlay.Show();
        GD.Print("[ClientNode] Training mode active");
    }

    private void OnJoinRankedRequested()
    {
        // Disconnect and reconnect without training mode so the normal login screen appears
        _trainingMode = false;
        _authenticated = false;
        _eliminated = false;
        _spectating = false;
        _gamePhase = GamePhase.Connecting;
        _trainingOverlay.Hide();
        _network.Disconnect();
        _loginScreen.Show();
        var error = _network.Connect(ServerAddress, ServerPort);
        if (error != Error.Ok)
            GD.PrintErr($"[ClientNode] Reconnect failed: {error}");
    }

    private static void OnQuitRequested()
    {
        (Engine.GetMainLoop() as SceneTree)?.Quit();
    }

    private void OnLoginRequested(string username, string password)
    {
        if (!_network.IsConnected()) return;
        _network.SendLogin(new LoginRequest(username, password));
    }

    private void OnRegisterRequested(string username, string password)
    {
        if (!_network.IsConnected()) return;
        _network.SendRegister(new RegisterRequest(username, password));
    }

    private void OnLoginResponse(LoginResponse response)
    {
        if (!response.Success)
        {
            _loginScreen.ShowError(response.ErrorMessage);
            return;
        }

        _accountId = response.AccountId;
        _nickname = response.Nickname;
        _authenticated = true;
        _gamePhase = GamePhase.InGame;
        _loginScreen.Hide();
        GD.Print($"[ClientNode] Authenticated as {_nickname} (accountId: {_accountId})");
    }

    private void OnRegisterResponse(RegisterResponse response)
    {
        if (!response.Success)
        {
            _loginScreen.ShowError(response.ErrorMessage);
        }
    }

    private void OnLeaderboardResponse(LeaderboardResponse response)
    {
        GD.Print($"[ClientNode] Leaderboard received for mode {response.Mode} ({response.Entries.Length} entries)");
    }

    private void OnGameStateDeltaForSpectator(GameStateDelta delta)
    {
        if (!_spectating) return;
        int aliveCount = 0;
        foreach (var tank in delta.Tanks)
            if (tank.Health > 0) aliveCount++;
        _spectatorOverlay.UpdateAliveCount(aliveCount);
    }

    private void OnPlayerEliminated(PlayerEliminatedMessage msg)
    {
        if (msg.EliminatedPlayerId == _localPlayerId)
        {
            _eliminated = true;
            _spectating = true;
            _renderer.EnterSpectatorMode();
            _spectatorOverlay.Show();
        }
    }

    private void OnGameOver(GameOverMessage msg)
    {
        _eliminated = true;
        _spectating = false;
        _gamePhase = GamePhase.GameOver;
        _spectatorOverlay.Hide();
        _gameOverScreen.ShowWin(_localPlayerId, msg.WinnerPlayerId);
    }

    private static InputFlags ReadInput()
    {
        InputFlags flags = InputFlags.None;

        if (Input.IsActionPressed("move_forward"))
            flags |= InputFlags.MoveForward;
        if (Input.IsActionPressed("move_backward"))
            flags |= InputFlags.MoveBackward;
        if (Input.IsActionPressed("rotate_left"))
            flags |= InputFlags.RotateLeft;
        if (Input.IsActionPressed("rotate_right"))
            flags |= InputFlags.RotateRight;
        if (Input.IsActionPressed("fire"))
            flags |= InputFlags.Fire;

        return flags;
    }
}

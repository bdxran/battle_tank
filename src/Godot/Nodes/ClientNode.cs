using Godot;
using Microsoft.Extensions.Logging;
using BattleTank.GameLogic.Network;
using BattleTank.GameLogic.Shared;
using BattleTank.Godot.CrashReport;
using BattleTank.Godot.Network;
using BattleTank.Godot.Renderer;
using BattleTank.Godot.Settings;
using BattleTank.Godot.UI;

namespace BattleTank.Godot.Nodes;

/// <summary>
/// Root node for the game client. Manages all navigation flows:
///   MainMenu → Solo (LocalGameNode) | Host (HostNode) | Join (RoomBrowserScreen → LoginScreen)
/// </summary>
public partial class ClientNode : Node
{
    private enum GamePhase { MainMenu, Solo, Connecting, Lobby, InGame, GameOver }

    [Export] public string ServerAddress { get; set; } = "127.0.0.1";
    [Export] public int ServerPort { get; set; } = Constants.ServerPort;

    // Network (used for remote modes)
    private ClientNetworkManager _network = null!;

    // Rendering
    private GameRenderer _renderer = null!;
    private HudNode _hud = null!;

    // UI screens (persistent)
    private MainMenuScreen _mainMenuScreen = null!;
    private SoloModeScreen _soloModeScreen = null!;
    private HostSetupScreen _hostSetupScreen = null!;
    private ServerInfoScreen _serverInfoScreen = null!;
    private RoomBrowserScreen? _roomBrowserScreen;
    private GameOverScreen _gameOverScreen = null!;
    private LoginScreen _loginScreen = null!;
    private TrainingOverlayNode _trainingOverlay = null!;
    private PauseMenuNode _pauseMenu = null!;
    private SpectatorOverlayNode _spectatorOverlay = null!;
    private AudioManagerNode _audioManager = null!;
    private CountdownNode _countdown = null!;
    private KeybindingsScreen _keybindingsScreen = null!;
    private CrashReportScreen _crashReportScreen = null!;

    // In-process server (host mode)
    private HostNode? _hostNode;

    // Local offline game
    private LocalGameNode? _localGameNode;

    // Per-session state
    private int _localPlayerId;
    private int _accountId = -1;
    private string _nickname = "";
    private uint _inputSequence;
    private bool _eliminated;
    private bool _spectating;
    private bool _authenticated;
    private bool _trainingMode;
    private string? _pendingRoomCode;
    private GamePhase _gamePhase = GamePhase.MainMenu;
    private GameLogic.Shared.GameMode _pendingMode;
    private string _pendingNickname = "";

    private ScoreboardOverlay _scoreboard = null!;
    private PlayerInfo[] _lastLeaderboard = [];
    private int[] _lastTeamScores = [];
    private bool _countdownActive;
    private GameLogic.Shared.GameMode _currentMode;

    private CrashReporter _crashReporter = null!;

    public override void _Ready()
    {
        InputSettings.Load();
        _crashReporter = new CrashReporter();
        _crashReporter.Initialize(() => _gamePhase.ToString());
        _crashReporter.PendingReportsFound += OnPendingReportsFound;

        var loggerFactory = new GodotLoggerFactory(_crashReporter);

        // Network (only active for remote modes)
        _network = new ClientNetworkManager();
        _network.Initialize(loggerFactory.CreateLogger<ClientNetworkManager>());
        AddChild(_network);

        _hud = new HudNode();
        AddChild(_hud);
        _hud.Hide();

        _gameOverScreen = new GameOverScreen();
        _gameOverScreen.RestartRequested += OnGameOverRestart;
        _gameOverScreen.MenuRequested += OnGameOverMenu;
        AddChild(_gameOverScreen);

        _scoreboard = new ScoreboardOverlay();
        AddChild(_scoreboard);

        _renderer = new GameRenderer();
        AddChild(_renderer);

        _loginScreen = new LoginScreen();
        _loginScreen.LoginRequested += OnLoginRequested;
        _loginScreen.RegisterRequested += OnRegisterRequested;
        _loginScreen.TrainingRequested += OnTrainingRequested;
        AddChild(_loginScreen);
        _loginScreen.Hide();

        _trainingOverlay = new TrainingOverlayNode();
        _trainingOverlay.JoinRankedRequested += OnJoinRankedRequested;
        AddChild(_trainingOverlay);

        _pauseMenu = new PauseMenuNode();
        _pauseMenu.ResumeRequested += OnPauseMenuResume;
        _pauseMenu.QuitRequested += OnQuitRequested;
        AddChild(_pauseMenu);

        _spectatorOverlay = new SpectatorOverlayNode();
        AddChild(_spectatorOverlay);

        _audioManager = new AudioManagerNode();
        AddChild(_audioManager);

        _countdown = new CountdownNode();
        _countdown.CountdownFinished += OnCountdownFinished;
        AddChild(_countdown);

        var mailer = new CrashReportMailer();
        _crashReportScreen = new CrashReportScreen();
        _crashReportScreen.Initialize(_crashReporter, mailer);
        AddChild(_crashReportScreen);

        // Main menu screens
        _mainMenuScreen = new MainMenuScreen();
        _mainMenuScreen.SoloRequested += OnSoloRequested;
        _mainMenuScreen.HostRequested += OnHostRequested;
        _mainMenuScreen.JoinRequested += OnJoinMenuRequested;
        _mainMenuScreen.SettingsRequested += OnSettingsRequested;
        AddChild(_mainMenuScreen);

        _keybindingsScreen = new KeybindingsScreen();
        _keybindingsScreen.BackRequested += OnSettingsBack;
        AddChild(_keybindingsScreen);

        _soloModeScreen = new SoloModeScreen();
        _soloModeScreen.SoloModeSelected += OnSoloModeSelected;
        _soloModeScreen.BackRequested += () => { _soloModeScreen.Hide(); _mainMenuScreen.Show(); };
        AddChild(_soloModeScreen);
        _soloModeScreen.Hide();

        _hostSetupScreen = new HostSetupScreen();
        _hostSetupScreen.HostConfigured += OnHostConfigured;
        _hostSetupScreen.BackRequested += () => { _hostSetupScreen.Hide(); _mainMenuScreen.Show(); };
        AddChild(_hostSetupScreen);
        _hostSetupScreen.Hide();

        _serverInfoScreen = new ServerInfoScreen();
        _serverInfoScreen.PlayRequested += OnServerInfoPlayRequested;
        _serverInfoScreen.StopRequested += OnServerInfoStopRequested;
        AddChild(_serverInfoScreen);
        _serverInfoScreen.Hide();

        // Remote network events
        _network.ConnectedToServer += OnConnected;
        _network.DisconnectedFromServer += OnDisconnected;
        _network.PlayerEliminated += OnPlayerEliminated;
        _network.GameOver += OnGameOver;
        _network.LoginResponseReceived += OnLoginResponse;
        _network.RegisterResponseReceived += OnRegisterResponse;
        _network.LeaderboardResponseReceived += OnLeaderboardResponse;
        _network.GameStateDeltaReceived += OnGameStateDeltaForSpectator;
        _network.GameStateFullReceived += OnGameStateFullReceived;

        _mainMenuScreen.Show();
        _crashReporter.CheckPendingReports();
    }

    public override void _Process(double delta)
    {
        if (_gamePhase == GamePhase.InGame || _gamePhase == GamePhase.Solo)
        {
            bool tabHeld = Input.IsKeyPressed(Key.Tab);
            if (tabHeld && !_scoreboard.Visible)
            {
                if (_localGameNode != null)
                    _lastLeaderboard = _localGameNode.GetLeaderboard();
                _scoreboard.UpdateFrom(_lastLeaderboard, _lastTeamScores, _currentMode);
                _scoreboard.Show();
            }
            else if (!tabHeld && _scoreboard.Visible)
            {
                _scoreboard.Hide();
            }
        }

        if ((_gamePhase == GamePhase.InGame || _gamePhase == GamePhase.Solo) && !_countdownActive && Input.IsActionJustPressed("ui_cancel"))
        {
            if (_pauseMenu.Visible)
                OnPauseMenuResume();
            else
                OpenPauseMenu();
            return;
        }

        // Remote input loop (skip when paused)
        if (_gamePhase == GamePhase.InGame && !_pauseMenu.Visible
            && _network.IsConnected() && _authenticated && !_eliminated)
        {
            var flags = ReadInput();
            if (flags != InputFlags.None)
            {
                _inputSequence++;
                _network.SendInput(new PlayerInput(_localPlayerId, flags, _inputSequence));
            }
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

    // ── Main menu ────────────────────────────────────────────────────────────

    private void OnSoloRequested()
    {
        _mainMenuScreen.Hide();
        _soloModeScreen.Show();
    }

    private void OnSoloModeSelected(GameLogic.Shared.GameMode mode, string nickname)
    {
        _soloModeScreen.Hide();
        StartLocalGame(mode, nickname);
    }

    private void OnHostRequested()
    {
        _mainMenuScreen.Hide();
        _hostSetupScreen.Show();
    }

    private void OnHostConfigured(string gameName, int port, string? roomCode,
        GameLogic.Shared.GameMode mode, int durationSeconds, int scoreToWin)
    {
        _hostSetupScreen.Hide();

        _hostNode = new HostNode();
        _hostNode.ServerReady += (ip, p) =>
        {
            _serverInfoScreen.SetInfo(ip, p, roomCode);
            _serverInfoScreen.Show();
        };
        _hostNode.ServerFailed += error =>
        {
            _hostNode.QueueFree();
            _hostNode = null;
            _hostSetupScreen.ShowError(error);
            _hostSetupScreen.Show();
        };
        AddChild(_hostNode);
        _hostNode.Initialize(gameName, port, roomCode, mode, durationSeconds, scoreToWin);
    }

    private void OnServerInfoPlayRequested()
    {
        _serverInfoScreen.Hide();
        StartRemoteConnection("127.0.0.1", ServerPort, roomCode: null);
    }

    private void OnServerInfoStopRequested()
    {
        _serverInfoScreen.Hide();
        _hostNode?.QueueFree();
        _hostNode = null;
        _mainMenuScreen.Show();
    }

    private void OnJoinMenuRequested()
    {
        _mainMenuScreen.Hide();
        _roomBrowserScreen = new RoomBrowserScreen();
        _roomBrowserScreen.JoinRequested += OnRoomBrowserJoin;
        _roomBrowserScreen.BackRequested += OnRoomBrowserBack;
        AddChild(_roomBrowserScreen);
    }

    private void OnSettingsRequested()
    {
        _mainMenuScreen.Hide();
        _keybindingsScreen.Show();
    }

    private void OnSettingsBack()
    {
        _keybindingsScreen.Hide();
        _mainMenuScreen.Show();
    }

    private void OnRoomBrowserJoin(string address, int port, string? roomCode)
    {
        _roomBrowserScreen?.QueueFree();
        _roomBrowserScreen = null;
        StartRemoteConnection(address, port, roomCode);
    }

    private void OnRoomBrowserBack()
    {
        _roomBrowserScreen?.QueueFree();
        _roomBrowserScreen = null;
        _mainMenuScreen.Show();
    }

    // ── Solo local ───────────────────────────────────────────────────────────

    private void StartLocalGame(GameLogic.Shared.GameMode mode, string nickname)
    {
        _pendingMode = mode;
        _pendingNickname = nickname;
        _gamePhase = GamePhase.Solo;

        _localGameNode = new LocalGameNode();
        _localGameNode.PlayerEliminated += OnLocalPlayerEliminated;
        _localGameNode.GameOver += OnLocalGameOver;
        _localGameNode.Running = false;
        AddChild(_localGameNode);

        _renderer.Show();
        _renderer.Initialize(_localGameNode, _hud, LocalGameNode.LocalPlayerId);
        _hud.Show();

        _currentMode = mode;
        _localGameNode.Initialize(mode, nickname);

        _countdownActive = true;
        _countdown.StartCountdown();
    }

    private void OnCountdownFinished()
    {
        _countdownActive = false;
        if (_localGameNode != null)
            _localGameNode.Running = true;

        bool isTraining = _pendingMode == GameLogic.Shared.GameMode.Training;
        if (isTraining)
        {
            _trainingOverlay.SetLocalMode(true);
            _trainingOverlay.Show();
        }
    }

    private void OnLocalPlayerEliminated(PlayerEliminatedMessage msg)
    {
        if (msg.EliminatedPlayerId == LocalGameNode.LocalPlayerId)
        {
            _renderer.EnterSpectatorMode();
            _spectatorOverlay.Show();
        }
    }

    private void OnLocalGameOver(GameOverMessage msg)
    {
        _gamePhase = GamePhase.GameOver;
        _spectatorOverlay.Hide();
        _scoreboard.Hide();
        _gameOverScreen.ShowResult(
            LocalGameNode.LocalPlayerId, msg.WinnerPlayerId, msg.WinnerTeamId,
            msg.Leaderboard ?? [], _lastTeamScores, _currentMode);

        // Clean up local game
        _localGameNode?.QueueFree();
        _localGameNode = null;
    }

    private void OnGameOverRestart()
    {
        _gameOverScreen.Visible = false;
        if (_network.IsConnected())
        {
            // Remote mode: return to menu, reconnect flow is complex
            OnGameOverMenu();
        }
        else
        {
            // Solo mode: relaunch same mode and nickname
            StartLocalGame(_pendingMode, _pendingNickname);
        }
    }

    private void OnGameOverMenu()
    {
        _gameOverScreen.Visible = false;
        _hud.Hide();
        _renderer.Hide();
        _gamePhase = GamePhase.MainMenu;
        _mainMenuScreen.Show();
    }

    // ── Remote connection ────────────────────────────────────────────────────

    private void StartRemoteConnection(string address, int port, string? roomCode)
    {
        _pendingRoomCode = roomCode;
        _gamePhase = GamePhase.Connecting;

        _loginScreen.Show();
        _loginScreen.StartConnecting();

        var error = _network.Connect(address, port);
        if (error != Error.Ok)
            GD.PrintErr($"[ClientNode] Failed to connect to {address}:{port}: {error}");
        else
            GD.Print($"[ClientNode] Connecting to {address}:{port}");
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
        _gamePhase = GamePhase.MainMenu;
        GD.Print("[ClientNode] Disconnected from server");
    }

    private void OnTrainingRequested()
    {
        if (!_network.IsConnected()) return;

        _trainingMode = true;
        string guestNick = $"Joueur{_localPlayerId}";
        _network.SendJoinTraining(new JoinTrainingRequest(guestNick, _pendingRoomCode));
    }

    private void OnGameStateFullReceived(GameStateFull state)
    {
        _currentMode = state.Mode;
        _lastLeaderboard = state.Players ?? [];
        _lastTeamScores = state.TeamScores ?? [];

        if (!_trainingMode || _authenticated) return;

        _authenticated = true;
        _gamePhase = GamePhase.InGame;
        _loginScreen.Hide();
        _trainingOverlay.Show();
        GD.Print("[ClientNode] Training mode active");
    }

    private void OnJoinRankedRequested()
    {
        _trainingMode = false;
        _authenticated = false;
        _eliminated = false;
        _spectating = false;
        _gamePhase = GamePhase.MainMenu;
        _trainingOverlay.Hide();

        // Clean up local game if running
        if (_localGameNode != null)
        {
            _localGameNode.QueueFree();
            _localGameNode = null;
        }

        _mainMenuScreen.Show();
    }

    private void OpenPauseMenu()
    {
        if (_localGameNode != null)
            _localGameNode.Running = false;
        _pauseMenu.Show();
    }

    private void OnPauseMenuResume()
    {
        _pauseMenu.Hide();
        if (_localGameNode != null)
            _localGameNode.Running = true;
    }

    private void OnQuitRequested()
    {
        _pauseMenu.Hide();
        _hud.Hide();
        _renderer.Hide();

        if (_localGameNode != null)
        {
            _localGameNode.QueueFree();
            _localGameNode = null;
        }

        _gamePhase = GamePhase.MainMenu;
        _soloModeScreen.Show();
    }

    private void OnLoginRequested(string username, string password)
    {
        if (!_network.IsConnected()) return;
        _network.SendLogin(new LoginRequest(username, password, _pendingRoomCode));
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
            _loginScreen.ShowError(response.ErrorMessage);
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
        _scoreboard.Hide();
        _gameOverScreen.ShowResult(
            _localPlayerId, msg.WinnerPlayerId, msg.WinnerTeamId,
            msg.Leaderboard ?? [], _lastTeamScores, _currentMode);
    }

    private void OnPendingReportsFound(string[] paths)
    {
        if (paths.Length > 0)
            _crashReportScreen.ShowCrash(paths[0]);
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

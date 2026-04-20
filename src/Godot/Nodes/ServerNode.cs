using Godot;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using BattleTank.GameLogic.Network;
using BattleTank.GameLogic.Rules;
using BattleTank.Godot.Network;
using BattleTank.Godot.Persistence;

namespace BattleTank.Godot.Nodes;

/// <summary>
/// Root node for the dedicated server. Add to the server scene.
/// Wires ServerNetworkManager → GameRoomNode.
/// </summary>
public partial class ServerNode : Node
{
    [Export] public int Port { get; set; } = 4242;
    [Export] public int MaxClients { get; set; } = 10;
    [Export] public string DbPath { get; set; } = "battle_tank.db";

    private ServerNetworkManager _network = null!;
    private GameRoomNode _room = null!;
    private BattleTankDbContext _db = null!;
    private string _adminPassword = "";
    private string _serverName = "";
    private int? _adminPeerId;

    public override void _Ready()
    {
        var loggerFactory = NullLoggerFactory.Instance;

        _adminPassword = ReadArg("--admin-password=")
            ?? System.Environment.GetEnvironmentVariable("ADMIN_PASSWORD")
            ?? "";
        _serverName = ReadArg("--server-name=")
            ?? System.Environment.GetEnvironmentVariable("SERVER_NAME")
            ?? System.Net.Dns.GetHostName();

        _db = new BattleTankDbContext(DbPath);
        _db.Database.EnsureCreated();

        var repository = new PlayerRepository(_db);
        var leaderboard = new LeaderboardService(_db);

        _network = new ServerNetworkManager();
        _network.Initialize(loggerFactory.CreateLogger<ServerNetworkManager>());
        _network.PlayerDisconnected += OnAdminDisconnected;
        _network.AdminLoginReceived += OnAdminLoginReceived;
        _network.ServerConfigReceived += OnServerConfigReceived;
        _network.ServerStatusRequested += OnServerStatusRequested;
        AddChild(_network);

        _room = new GameRoomNode();
        _room.Initialize(
            _network,
            loggerFactory.CreateLogger<GameRoomNode>(),
            loggerFactory.CreateLogger<GameRoom>(),
            repository,
            leaderboard,
            botFillCount: 0);
        AddChild(_room);

        var error = _network.Start(Port, MaxClients);
        if (error != Error.Ok)
            GD.PrintErr($"[ServerNode] Failed to start server: {error}");
        else
            GD.Print($"[ServerNode] Server listening on port {Port}, name={_serverName}, DB: {DbPath}");
    }

    private void OnAdminDisconnected(int peerId)
    {
        if (_adminPeerId == peerId)
        {
            _adminPeerId = null;
            GD.Print("[ServerNode] Admin disconnected");
        }
    }

    private void OnAdminLoginReceived(int peerId, AdminLoginRequest request)
    {
        GD.Print($"[ServerNode] Admin login attempt from peer {peerId}: received='{request.AdminPassword}' expected='{_adminPassword}'");
        if (string.IsNullOrEmpty(_adminPassword) || request.AdminPassword != _adminPassword)
        {
            var fail = new AdminLoginResponse(false, "Mot de passe admin incorrect");
            _network.SendToPlayerReliable(peerId, new NetworkMessage(
                MessageType.AdminLoginResponse, GameStateSerializer.Serialize(fail)));
            GD.PrintErr($"[ServerNode] Admin login failed from peer {peerId}");
            return;
        }

        _adminPeerId = peerId;
        var ok = new AdminLoginResponse(true);
        _network.SendToPlayerReliable(peerId, new NetworkMessage(
            MessageType.AdminLoginResponse, GameStateSerializer.Serialize(ok)));
        GD.Print($"[ServerNode] Admin authenticated: peer {peerId}");
    }

    private void OnServerConfigReceived(int peerId, ServerConfigRequest request)
    {
        if (_adminPeerId != peerId)
        {
            var denied = new ServerConfigResponse(false, "Non autorisé");
            _network.SendToPlayerReliable(peerId, new NetworkMessage(
                MessageType.ServerConfigResponse, GameStateSerializer.Serialize(denied)));
            return;
        }

        bool ok = _room.Reconfigure(request.Mode, request.DurationSeconds, request.ScoreToWin, request.FriendlyFire, request.RoomCode, request.BotFillCount);
        var resp = ok
            ? new ServerConfigResponse(true)
            : new ServerConfigResponse(false, "Impossible de reconfigurer pendant une partie en cours");
        _network.SendToPlayerReliable(peerId, new NetworkMessage(
            MessageType.ServerConfigResponse, GameStateSerializer.Serialize(resp)));
    }

    private void OnServerStatusRequested(int peerId, ServerStatusRequest _)
    {
        var (mode, phase, duration, score, ff, nicknames) = _room.GetStatus();
        var resp = new ServerStatusResponse(
            mode, phase, duration, score, ff,
            nicknames.Length, nicknames,
            !string.IsNullOrEmpty(_room.RoomCode),
            _serverName);
        _network.SendToPlayerReliable(peerId, new NetworkMessage(
            MessageType.ServerStatusResponse, GameStateSerializer.Serialize(resp)));
    }

    private static string? ReadArg(string prefix)
    {
        foreach (var arg in OS.GetCmdlineUserArgs())
            if (arg.StartsWith(prefix))
                return arg[prefix.Length..];
        return null;
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
        {
            _network?.Stop();
            _db?.Dispose();
            GetTree().Quit();
        }
    }

    public override void _ExitTree()
    {
        _network?.Stop();
        _db?.Dispose();
    }
}

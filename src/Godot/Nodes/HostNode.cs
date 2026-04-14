using System;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Godot;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using BattleTank.GameLogic.Rules;
using BattleTank.GameLogic.Shared;
using BattleTank.Godot.Network;
using BattleTank.Godot.Persistence;

namespace BattleTank.Godot.Nodes;

/// <summary>
/// Hosts an in-process dedicated server alongside the local client.
/// Starts ServerNetworkManager + GameRoomNode, broadcasts via LanAnnouncer,
/// then fires ServerReady so ClientNode can connect in loopback.
/// </summary>
public partial class HostNode : Node
{
    public event Action<string, int>? ServerReady;

    private ServerNetworkManager _serverNetwork = null!;
    private GameRoomNode _room = null!;
    private LanAnnouncer _announcer = null!;
    private BattleTankDbContext? _db;

    public void Initialize(string gameName, int port, string? roomCode)
    {
        var loggerFactory = NullLoggerFactory.Instance;

        string dbPath = System.IO.Path.Combine(
            OS.GetUserDataDir(), "battle_tank_host.db");

        _db = new BattleTankDbContext(dbPath);
        _db.Database.EnsureCreated();

        var repository = new PlayerRepository(_db);
        var leaderboard = new LeaderboardService(_db);

        _serverNetwork = new ServerNetworkManager();
        _serverNetwork.Initialize(NullLogger<ServerNetworkManager>.Instance);
        AddChild(_serverNetwork);

        _room = new GameRoomNode();
        _room.RoomCode = roomCode;
        _room.Initialize(
            _serverNetwork,
            NullLogger<GameRoomNode>.Instance,
            NullLogger<GameRoom>.Instance,
            repository,
            leaderboard);
        AddChild(_room);

        var error = _serverNetwork.Start(port, Constants.MaxPlayersPerRoom);
        if (error != Error.Ok)
        {
            GD.PrintErr($"[HostNode] Failed to start server on port {port}: {error}");
            return;
        }

        string localIp = GetLocalIpAddress();

        _announcer = new LanAnnouncer();
        AddChild(_announcer);
        _announcer.Start(new ServerAnnouncement(
            localIp, port, gameName,
            Players: 0,
            Mode: "BattleRoyale",
            HasCode: !string.IsNullOrEmpty(roomCode),
            AppVersion: Constants.GameVersion));

        GD.Print($"[HostNode] Server started on {localIp}:{port}, roomCode={(roomCode ?? "none")}");
        ServerReady?.Invoke(localIp, port);
    }

    public override void _ExitTree()
    {
        _announcer?.Stop();
        _serverNetwork?.Stop();
        _db?.Dispose();
    }

    private static string GetLocalIpAddress()
    {
        foreach (var iface in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (iface.OperationalStatus != OperationalStatus.Up) continue;
            if (iface.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

            foreach (var addr in iface.GetIPProperties().UnicastAddresses)
            {
                if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                    return addr.Address.ToString();
            }
        }
        return "127.0.0.1";
    }
}

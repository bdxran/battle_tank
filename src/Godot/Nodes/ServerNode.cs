using Godot;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using BattleTank.GameLogic.Rules;
using BattleTank.Godot.Network;

namespace BattleTank.Godot.Nodes;

/// <summary>
/// Root node for the dedicated server. Add to the server scene.
/// Wires ServerNetworkManager → GameRoomNode.
/// </summary>
public partial class ServerNode : Node
{
    [Export] public int Port { get; set; } = 4242;
    [Export] public int MaxClients { get; set; } = 10;

    private ServerNetworkManager _network = null!;
    private GameRoomNode _room = null!;

    public override void _Ready()
    {
        var loggerFactory = NullLoggerFactory.Instance;

        _network = new ServerNetworkManager();
        _network.Initialize(loggerFactory.CreateLogger<ServerNetworkManager>());
        AddChild(_network);

        _room = new GameRoomNode();
        _room.Initialize(_network,
            loggerFactory.CreateLogger<GameRoomNode>(),
            loggerFactory.CreateLogger<GameRoom>());
        AddChild(_room);

        var error = _network.Start(Port, MaxClients);
        if (error != Error.Ok)
            GD.PrintErr($"[ServerNode] Failed to start server: {error}");
        else
            GD.Print($"[ServerNode] Server listening on port {Port}");
    }

    public override void _ExitTree()
    {
        _network?.Stop();
    }
}

using Godot;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using BattleTank.GameLogic.Network;
using BattleTank.GameLogic.Shared;
using BattleTank.Godot.Network;
using BattleTank.Godot.Renderer;
using BattleTank.Godot.UI;

namespace BattleTank.Godot.Nodes;

/// <summary>
/// Root node for the game client. Add to the client scene.
/// Wires ClientNetworkManager → GameRenderer + HudNode.
/// Reads keyboard input and sends PlayerInput every frame.
/// </summary>
public partial class ClientNode : Node
{
    [Export] public string ServerAddress { get; set; } = "127.0.0.1";
    [Export] public int ServerPort { get; set; } = Constants.ServerPort;

    private ClientNetworkManager _network = null!;
    private GameRenderer _renderer = null!;
    private HudNode _hud = null!;
    private uint _inputSequence;

    public override void _Ready()
    {
        var loggerFactory = NullLoggerFactory.Instance;

        _network = new ClientNetworkManager();
        _network.Initialize(loggerFactory.CreateLogger<ClientNetworkManager>());
        AddChild(_network);

        _hud = new HudNode();
        AddChild(_hud);

        _renderer = new GameRenderer();
        AddChild(_renderer);

        _network.ConnectedToServer += OnConnected;
        _network.DisconnectedFromServer += OnDisconnected;

        var error = _network.Connect(ServerAddress, ServerPort);
        if (error != Error.Ok)
            GD.PrintErr($"[ClientNode] Failed to connect: {error}");
        else
            GD.Print($"[ClientNode] Connecting to {ServerAddress}:{ServerPort}");
    }

    public override void _Process(double delta)
    {
        if (!_network.IsConnected()) return;

        var flags = ReadInput();
        if (flags != InputFlags.None)
        {
            _inputSequence++;
            _network.SendInput(new PlayerInput(
                Multiplayer.GetUniqueId(),
                flags,
                _inputSequence));
        }
    }

    public override void _ExitTree()
    {
        _network?.Disconnect();
    }

    private void OnConnected()
    {
        int localId = Multiplayer.GetUniqueId();
        GD.Print($"[ClientNode] Connected as peer {localId}");
        _renderer.Initialize(_network, _hud, localId);
    }

    private void OnDisconnected()
    {
        GD.Print("[ClientNode] Disconnected from server");
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

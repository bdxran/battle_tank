using System;
using Godot;
using Microsoft.Extensions.Logging;
using BattleTank.GameLogic.Network;

namespace BattleTank.Godot.Network;

public partial class ClientNetworkManager : Node
{
    private ILogger<ClientNetworkManager> _logger = null!;

    private ENetMultiplayerPeer _peer = null!;
    private bool _connected;

    public event Action? ConnectedToServer;
    public event Action? DisconnectedFromServer;
    public event Action<GameStateFull>? GameStateFullReceived;
    public event Action<GameStateDelta>? GameStateDeltaReceived;

    public void Initialize(ILogger<ClientNetworkManager> logger)
    {
        _logger = logger;
    }

    public Error Connect(string address, int port)
    {
        _peer = new ENetMultiplayerPeer();
        var error = _peer.CreateClient(address, port);
        if (error != Error.Ok)
        {
            _logger.LogError("Failed to connect to {Address}:{Port}: {Error}", address, port, error);
            return error;
        }

        Multiplayer.MultiplayerPeer = _peer;
        Multiplayer.ConnectedToServer += OnConnectedToServer;
        Multiplayer.ServerDisconnected += OnServerDisconnected;

        _logger.LogInformation("Connecting to {Address}:{Port}", address, port);
        return Error.Ok;
    }

    public void Disconnect()
    {
        Multiplayer.ConnectedToServer -= OnConnectedToServer;
        Multiplayer.ServerDisconnected -= OnServerDisconnected;
        _peer?.Close();
        _logger.LogInformation("Disconnected from server");
    }

    public void SendInput(PlayerInput input)
    {
        var data = GameStateSerializer.Serialize(input);
        var payload = BuildPayload(MessageType.PlayerInput, data);
        RpcId(1, MethodName.ReceiveMessage, payload);
    }

    public bool IsConnected() => _connected;

    private void OnConnectedToServer()
    {
        _connected = true;
        _logger.LogInformation("Connected to server as peer {PeerId}", Multiplayer.GetUniqueId());
        ConnectedToServer?.Invoke();
    }

    private void OnServerDisconnected()
    {
        _connected = false;
        _logger.LogWarning("Disconnected from server");
        DisconnectedFromServer?.Invoke();
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    private void ReceiveMessage(byte[] payload)
    {
        if (!TryParseMessage(payload, out var message))
        {
            _logger.LogWarning("Received invalid message from server");
            return;
        }

        try
        {
            switch (message!.Type)
            {
                case MessageType.GameStateFull:
                    var full = GameStateSerializer.Deserialize<GameStateFull>(message.Payload);
                    GameStateFullReceived?.Invoke(full);
                    break;

                case MessageType.GameStateDelta:
                    var delta = GameStateSerializer.Deserialize<GameStateDelta>(message.Payload);
                    GameStateDeltaReceived?.Invoke(delta);
                    break;

                default:
                    _logger.LogDebug("Unhandled message type {Type}", message.Type);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize message type {Type}", message!.Type);
        }
    }

    private static byte[] BuildPayload(MessageType type, byte[] data)
    {
        var payload = new byte[1 + data.Length];
        payload[0] = (byte)type;
        Buffer.BlockCopy(data, 0, payload, 1, data.Length);
        return payload;
    }

    private static bool TryParseMessage(byte[] payload, out NetworkMessage? message)
    {
        if (payload.Length < 1)
        {
            message = null;
            return false;
        }

        var type = (MessageType)payload[0];
        var data = new byte[payload.Length - 1];
        Buffer.BlockCopy(payload, 1, data, 0, data.Length);
        message = new NetworkMessage(type, data);
        return true;
    }
}

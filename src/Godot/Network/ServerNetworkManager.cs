using System;
using Godot;
using Microsoft.Extensions.Logging;
using BattleTank.GameLogic.Network;
using BattleTank.GameLogic.Shared;

namespace BattleTank.Godot.Network;

public partial class ServerNetworkManager : Node
{
    private ILogger<ServerNetworkManager> _logger = null!;

    private ENetMultiplayerPeer _peer = null!;

    public event Action<int>? PlayerConnected;
    public event Action<int>? PlayerDisconnected;
    public event Action<int, PlayerInput>? InputReceived;

    public void Initialize(ILogger<ServerNetworkManager> logger)
    {
        _logger = logger;
    }

    public Error Start(int port = Constants.ServerPort, int maxClients = Constants.MaxPlayersPerRoom)
    {
        _peer = new ENetMultiplayerPeer();
        var error = _peer.CreateServer(port, maxClients);
        if (error != Error.Ok)
        {
            _logger.LogError("Failed to start ENet server on port {Port}: {Error}", port, error);
            return error;
        }

        Multiplayer.MultiplayerPeer = _peer;
        Multiplayer.PeerConnected += OnPeerConnected;
        Multiplayer.PeerDisconnected += OnPeerDisconnected;

        _logger.LogInformation("Server started on port {Port} (max {MaxClients} clients)", port, maxClients);
        return Error.Ok;
    }

    public void Stop()
    {
        Multiplayer.PeerConnected -= OnPeerConnected;
        Multiplayer.PeerDisconnected -= OnPeerDisconnected;
        _peer?.Close();
        _logger.LogInformation("Server stopped");
    }

    public void SendToPlayer(int peerId, NetworkMessage message)
    {
        var payload = BuildPayload(message);
        RpcId(peerId, MethodName.ReceiveMessage, payload);
    }

    public void Broadcast(NetworkMessage message)
    {
        var payload = BuildPayload(message);
        Rpc(MethodName.ReceiveMessage, payload);
    }

    private void OnPeerConnected(long id)
    {
        int playerId = (int)id;
        _logger.LogInformation("Player {PlayerId} connected", playerId);
        PlayerConnected?.Invoke(playerId);
    }

    private void OnPeerDisconnected(long id)
    {
        int playerId = (int)id;
        _logger.LogWarning("Player {PlayerId} disconnected", playerId);
        PlayerDisconnected?.Invoke(playerId);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    private void ReceiveMessage(byte[] payload)
    {
        int senderId = Multiplayer.GetRemoteSenderId();
        if (!TryParseMessage(payload, out var message))
        {
            _logger.LogWarning("Invalid message from player {PlayerId}", senderId);
            return;
        }

        if (message!.Type == MessageType.PlayerInput)
        {
            try
            {
                var input = GameStateSerializer.Deserialize<PlayerInput>(message.Payload);
                InputReceived?.Invoke(senderId, input);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize PlayerInput from {PlayerId}", senderId);
            }
        }
    }

    private static byte[] BuildPayload(NetworkMessage message)
    {
        var payload = new byte[1 + message.Payload.Length];
        payload[0] = (byte)message.Type;
        Buffer.BlockCopy(message.Payload, 0, payload, 1, message.Payload.Length);
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

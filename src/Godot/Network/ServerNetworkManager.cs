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

    private readonly System.Collections.Generic.Dictionary<int, ulong> _lastInputTick = new();
    private const ulong MinInputIntervalMs = 40; // max 25 msg/s

    public event Action<int>? PlayerConnected;
    public event Action<int>? PlayerDisconnected;
    public event Action<int, PlayerInput>? InputReceived;
    public event Action<int, LoginRequest>? LoginReceived;
    public event Action<int, RegisterRequest>? RegisterReceived;
    public event Action<int, GameMode>? LeaderboardRequested;
    public event Action<int, string>? JoinTrainingReceived;

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

    /// <summary>Returns round-trip time in ms for the given peer, or -1 if unavailable.</summary>
    public int GetPeerRtt(int peerId)
    {
        if (_peer is null) return -1;
        try
        {
            return (int)_peer.GetPeer(peerId).GetStatistic(ENetPacketPeer.PeerStatistic.RoundTripTime);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not read RTT for peer {PeerId}", peerId);
            return -1;
        }
    }

    public void DisconnectPeer(int peerId)
    {
        _peer?.DisconnectPeer(peerId);
    }

    public void SendToPlayer(int peerId, NetworkMessage message)
    {
        var payload = BuildPayload(message);
        RpcId(peerId, MethodName.ReceiveMessage, payload);
    }

    public void SendToPlayerReliable(int peerId, NetworkMessage message)
    {
        var payload = BuildPayload(message);
        RpcId(peerId, MethodName.ReceiveReliableMessage, payload);
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
            ulong now = Time.GetTicksMsec();
            if (_lastInputTick.TryGetValue(senderId, out ulong last) && now - last < MinInputIntervalMs)
                return;
            _lastInputTick[senderId] = now;

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

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceiveReliableMessage(byte[] payload)
    {
        int senderId = Multiplayer.GetRemoteSenderId();
        if (!TryParseMessage(payload, out var message))
        {
            _logger.LogWarning("Invalid reliable message from player {PlayerId}", senderId);
            return;
        }

        try
        {
            switch (message!.Type)
            {
                case MessageType.LoginRequest:
                    var login = GameStateSerializer.Deserialize<LoginRequest>(message.Payload);
                    LoginReceived?.Invoke(senderId, login);
                    break;

                case MessageType.RegisterRequest:
                    var register = GameStateSerializer.Deserialize<RegisterRequest>(message.Payload);
                    RegisterReceived?.Invoke(senderId, register);
                    break;

                case MessageType.LeaderboardRequest:
                    byte rawMode = message.Payload[0];
                    if (!Enum.IsDefined(typeof(GameMode), (int)rawMode))
                    {
                        _logger.LogWarning("Invalid GameMode byte {Raw} from peer {PeerId}", rawMode, senderId);
                        return;
                    }
                    LeaderboardRequested?.Invoke(senderId, (GameMode)rawMode);
                    break;

                case MessageType.JoinTraining:
                    var joinTraining = GameStateSerializer.Deserialize<JoinTrainingRequest>(message.Payload);
                    JoinTrainingReceived?.Invoke(senderId, joinTraining.Nickname);
                    break;

                default:
                    _logger.LogDebug("Unhandled reliable message type {Type} from {PlayerId}", message.Type, senderId);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize reliable message type {Type} from {PlayerId}", message!.Type, senderId);
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

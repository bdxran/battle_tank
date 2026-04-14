using System;
using Godot;
using Microsoft.Extensions.Logging;
using BattleTank.GameLogic.Network;
using BattleTank.GameLogic.Shared;

namespace BattleTank.Godot.Network;

public partial class ClientNetworkManager : Node, IGameStateProvider
{
    private ILogger<ClientNetworkManager> _logger = null!;

    private ENetMultiplayerPeer _peer = null!;
    private bool _connected;

    public event Action? ConnectedToServer;
    public event Action? DisconnectedFromServer;
    public event Action<GameStateFull>? GameStateFullReceived;
    public event Action<GameStateDelta>? GameStateDeltaReceived;
    public event Action<PlayerEliminatedMessage>? PlayerEliminated;
    public event Action<GameOverMessage>? GameOver;
    public event Action<LoginResponse>? LoginResponseReceived;
    public event Action<RegisterResponse>? RegisterResponseReceived;
    public event Action<LeaderboardResponse>? LeaderboardResponseReceived;

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

    public void SendLogin(LoginRequest request)
    {
        var payload = BuildPayload(MessageType.LoginRequest, GameStateSerializer.Serialize(request));
        RpcId(1, MethodName.ReceiveReliableMessage, payload);
    }

    public void SendRegister(RegisterRequest request)
    {
        var payload = BuildPayload(MessageType.RegisterRequest, GameStateSerializer.Serialize(request));
        RpcId(1, MethodName.ReceiveReliableMessage, payload);
    }

    public void SendJoinTraining(JoinTrainingRequest request)
    {
        var payload = BuildPayload(MessageType.JoinTraining, GameStateSerializer.Serialize(request));
        RpcId(1, MethodName.ReceiveReliableMessage, payload);
    }

    public void RequestLeaderboard(GameMode mode)
    {
        var payload = BuildPayload(MessageType.LeaderboardRequest, [(byte)mode]);
        RpcId(1, MethodName.ReceiveReliableMessage, payload);
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

                case MessageType.PlayerEliminated:
                    var elim = GameStateSerializer.Deserialize<PlayerEliminatedMessage>(message.Payload);
                    PlayerEliminated?.Invoke(elim);
                    break;

                case MessageType.GameOver:
                    var gameOver = GameStateSerializer.Deserialize<GameOverMessage>(message.Payload);
                    GameOver?.Invoke(gameOver);
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

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceiveReliableMessage(byte[] payload)
    {
        if (!TryParseMessage(payload, out var message))
        {
            _logger.LogWarning("Received invalid reliable message from server");
            return;
        }

        try
        {
            switch (message!.Type)
            {
                case MessageType.LoginResponse:
                    var loginResp = GameStateSerializer.Deserialize<LoginResponse>(message.Payload);
                    LoginResponseReceived?.Invoke(loginResp);
                    break;

                case MessageType.RegisterResponse:
                    var regResp = GameStateSerializer.Deserialize<RegisterResponse>(message.Payload);
                    RegisterResponseReceived?.Invoke(regResp);
                    break;

                case MessageType.LeaderboardResponse:
                    var lbResp = GameStateSerializer.Deserialize<LeaderboardResponse>(message.Payload);
                    LeaderboardResponseReceived?.Invoke(lbResp);
                    break;

                default:
                    _logger.LogDebug("Unhandled reliable message type {Type}", message.Type);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize reliable message type {Type}", message!.Type);
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

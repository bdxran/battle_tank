using System;
using BattleTank.GameLogic.Shared;
using MessagePack;

namespace BattleTank.GameLogic.Network;

public enum MessageType : byte
{
    PlayerInput = 0x01,
    GameStateDelta = 0x10,
    GameStateFull = 0x11,
    PlayerJoined = 0x20,
    PlayerEliminated = 0x21,
    GameOver = 0x22,
    Countdown = 0x23,
    ZoneUpdate = 0x30,
    LoginRequest = 0x40,
    LoginResponse = 0x41,
    RegisterRequest = 0x42,
    RegisterResponse = 0x43,
    LeaderboardRequest = 0x44,
    LeaderboardResponse = 0x45,
    JoinTraining = 0x50,
    Error = 0xFF,
}

[Flags]
public enum InputFlags : byte
{
    None = 0,
    MoveForward = 1 << 0,
    MoveBackward = 1 << 1,
    RotateLeft = 1 << 2,
    RotateRight = 1 << 3,
    Fire = 1 << 4,
}

public record NetworkMessage(MessageType Type, byte[] Payload);

[MessagePackObject]
public record PlayerInput(
    [property: Key(0)] int PlayerId,
    [property: Key(1)] InputFlags Flags,
    [property: Key(2)] uint SequenceNumber
);

[MessagePackObject]
public record GameStateFull(
    [property: Key(0)] uint SequenceNumber,
    [property: Key(1)] TankSnapshot[] Tanks,
    [property: Key(2)] BulletSnapshot[] Bullets,
    [property: Key(3)] GamePhase Phase,
    [property: Key(4)] ZoneSnapshot Zone,
    [property: Key(5)] PlayerInfo[] Players,
    [property: Key(6)] int CountdownSecondsRemaining,
    [property: Key(7)] PowerupSnapshot[] Powerups,
    [property: Key(8)] ControlPointSnapshot[] ControlPoints,
    [property: Key(9)] GameMode Mode = GameMode.BattleRoyale,
    [property: Key(10)] int TicksRemaining = 0,
    [property: Key(11)] int[] TeamScores = null!
);

[MessagePackObject]
public record GameStateDelta(
    [property: Key(0)] uint SequenceNumber,
    [property: Key(1)] uint LastAckedInput,
    [property: Key(2)] TankSnapshot[] Tanks,
    [property: Key(3)] BulletSnapshot[] Bullets,
    [property: Key(4)] ZoneSnapshot Zone,
    [property: Key(5)] PowerupSnapshot[] Powerups,
    [property: Key(6)] ControlPointSnapshot[] ControlPoints,
    [property: Key(7)] int TicksRemaining = 0,
    [property: Key(8)] int[] TeamScores = null!
);

[MessagePackObject]
public record PlayerJoinedMessage(
    [property: Key(0)] int PlayerId,
    [property: Key(1)] string PlayerName
);

[MessagePackObject]
public record PlayerEliminatedMessage(
    [property: Key(0)] int EliminatedPlayerId,
    [property: Key(1)] int KillerPlayerId
);

[MessagePackObject]
public record GameOverMessage(
    [property: Key(0)] int WinnerPlayerId,
    [property: Key(1)] PlayerInfo[] Leaderboard,
    [property: Key(2)] int WinnerTeamId = -1
);

[MessagePackObject]
public record CountdownMessage(
    [property: Key(0)] int SecondsRemaining
);

[MessagePackObject]
public record ZoneUpdateMessage(
    [property: Key(0)] float CenterX,
    [property: Key(1)] float CenterY,
    [property: Key(2)] float Radius,
    [property: Key(3)] float DamagePerSecond
);

[MessagePackObject]
public record LoginRequest(
    [property: Key(0)] string Username,
    [property: Key(1)] string Password,
    [property: Key(2)] string? RoomCode = null
);

[MessagePackObject]
public record LoginResponse(
    [property: Key(0)] bool Success,
    [property: Key(1)] int AccountId,
    [property: Key(2)] string Nickname,
    [property: Key(3)] string AvatarSeed,
    [property: Key(4)] string ErrorMessage = ""
);

[MessagePackObject]
public record RegisterRequest(
    [property: Key(0)] string Username,
    [property: Key(1)] string Password
);

[MessagePackObject]
public record RegisterResponse(
    [property: Key(0)] bool Success,
    [property: Key(1)] int AccountId,
    [property: Key(2)] string ErrorMessage = ""
);

[MessagePackObject]
public record LeaderboardEntryMessage(
    [property: Key(0)] int AccountId,
    [property: Key(1)] string Username,
    [property: Key(2)] int Wins,
    [property: Key(3)] int Kills,
    [property: Key(4)] int GamesPlayed
);

[MessagePackObject]
public record LeaderboardResponse(
    [property: Key(0)] string Mode,
    [property: Key(1)] LeaderboardEntryMessage[] Entries
);

[MessagePackObject]
public record JoinTrainingRequest(
    [property: Key(0)] string Nickname,
    [property: Key(1)] string? RoomCode = null
);

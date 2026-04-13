using MessagePack;

namespace BattleTank.GameLogic.Shared;

public enum GamePhase : byte
{
    WaitingForPlayers = 0,
    InProgress = 1,
    GameOver = 2,
}

[MessagePackObject]
public record TankSnapshot(
    [property: Key(0)] int Id,
    [property: Key(1)] float X,
    [property: Key(2)] float Y,
    [property: Key(3)] float Rotation,
    [property: Key(4)] int Health
);

[MessagePackObject]
public record BulletSnapshot(
    [property: Key(0)] int Id,
    [property: Key(1)] float X,
    [property: Key(2)] float Y,
    [property: Key(3)] float DirectionX,
    [property: Key(4)] float DirectionY,
    [property: Key(5)] int OwnerId
);

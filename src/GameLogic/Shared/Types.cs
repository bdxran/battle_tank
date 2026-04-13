using MessagePack;

namespace BattleTank.GameLogic.Shared;

public enum GamePhase : byte
{
    WaitingForPlayers = 0,
    InProgress = 1,
    GameOver = 2,
    Lobby = 3,
}

public enum GameMode : byte
{
    BattleRoyale = 0,
    Teams = 1,
    Deathmatch = 2,
    CaptureZone = 3,
}

public enum PowerupType : byte
{
    ExtraAmmo = 0,
    Shield = 1,
    SpeedBoost = 2,
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
public record ZoneSnapshot(
    [property: Key(0)] float CenterX,
    [property: Key(1)] float CenterY,
    [property: Key(2)] float Radius,
    [property: Key(3)] float DamagePerSecond
);

public record WallData(float X, float Y, float Width, float Height)
{
    public float Right => X + Width;
    public float Bottom => Y + Height;
}

[MessagePackObject]
public record BulletSnapshot(
    [property: Key(0)] int Id,
    [property: Key(1)] float X,
    [property: Key(2)] float Y,
    [property: Key(3)] float DirectionX,
    [property: Key(4)] float DirectionY,
    [property: Key(5)] int OwnerId
);

[MessagePackObject]
public record PowerupSnapshot(
    [property: Key(0)] int Id,
    [property: Key(1)] float X,
    [property: Key(2)] float Y,
    [property: Key(3)] int Type
);

[MessagePackObject]
public record ControlPointSnapshot(
    [property: Key(0)] int Id,
    [property: Key(1)] float X,
    [property: Key(2)] float Y,
    [property: Key(3)] float Radius,
    [property: Key(4)] int? ControllingTeamId,
    [property: Key(5)] float CaptureProgress
);

[MessagePackObject]
public record PlayerInfo(
    [property: Key(0)] int Id,
    [property: Key(1)] string Nickname,
    [property: Key(2)] int Kills,
    [property: Key(3)] int TeamId = -1
);

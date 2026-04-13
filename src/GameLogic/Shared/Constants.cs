namespace BattleTank.GameLogic.Shared;

public static class Constants
{
    public const int TickRate = 20;
    public const int MaxPlayersPerRoom = 10;
    public const int MinPlayersToStart = 2;
    public const int ServerPort = 4242;
    public const float ZoneShrinkInterval = 30f;

    public const int TankMaxHealth = 100;
    public const float TankMoveSpeed = 100f;    // pixels/second
    public const float TankRotationSpeed = 90f; // degrees/second

    public const int BulletDamage = 25;
    public const float BulletSpeed = 300f; // pixels/second
}

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
    public const float BulletMaxRange = 600f; // pixels

    public const float TankRadius = 20f;  // pixels, for collision
    public const float BulletRadius = 5f; // pixels, for collision

    public const float MapWidth = 1000f;
    public const float MapHeight = 1000f;
}

namespace BattleTank.GameLogic.Shared;

internal static class Constants
{
    internal const int TickRate = 20;
    internal const int MaxPlayersPerRoom = 10;
    internal const int MinPlayersToStart = 2;
    internal const int ServerPort = 4242;
    internal const float ZoneShrinkInterval = 30f;

    internal const int TankMaxHealth = 100;
    internal const float TankMoveSpeed = 100f;    // pixels/second
    internal const float TankRotationSpeed = 90f; // degrees/second

    internal const int BulletDamage = 25;
    internal const float BulletSpeed = 300f; // pixels/second
}

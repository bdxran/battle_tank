namespace BattleTank.GameLogic.Shared;

public static class Constants
{
    public const string GameVersion = "0.0.23";

    public const int TickRate = 20;
    public const int MaxPlayersPerRoom = 10;
    public const int MinPlayersToStart = 2;
    public const int ServerPort = 4242;
    public const float ZoneShrinkInterval = 30f;
    public const float ZoneActivationDelay = 15f;  // seconds before zone appears in BR

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

    public const float ZoneInitialRadius = 450f;  // pixels
    public const float ZoneMinRadius = 50f;
    public const float ZoneShrinkAmount = 80f;     // pixels per shrink step
    public const float ZoneDamagePerSecond = 10f;

    public const int LobbyCountdownTicks = 60;     // 3s at 20 TPS

    public const int MaxBulletsInFlight = 200;          // safety cap (10 players × ~20 bullets max)
    public const uint PowerupSpawnIntervalTicks = 200; // 10s at 20 TPS
    public const uint SpeedBoostDurationTicks = 100;   // 5s at 20 TPS
    public const int ShieldHealAmount = 25;
    public const float PowerupRadius = 15f;

    public const int RespawnInvincibilityTicks = 60; // 3s at 20 TPS

    // Deathmatch
    public const int DeathmatchDurationTicks = 3600;   // 3 min at 20 TPS
    public const int DeathmatchRespawnDelayTicks = 60; // 3s at 20 TPS

    // Capture Zone
    public const int CaptureZoneScoreToWin = 1200;          // ~60s avec 1 zone, ~20s avec 3 zones
    public const int CaptureZoneDurationTicks = 4800;       // 4 min at 20 TPS
    public const int CaptureZoneRespawnDelayTicks = 120;    // 6s at 20 TPS
    public const float ControlPointRadius = 80f;
    public const float CaptureRatePerSecond = 10f;     // capture progress rate (%/s per controlling team)
}

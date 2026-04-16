using System.Numerics;
using BattleTank.GameLogic.Shared;

namespace BattleTank.GameLogic.Rules;

public record GameOverResult(int? WinnerPlayerId, int? WinnerTeamId);

public interface IBattleRules
{
    /// <summary>Game mode identifier used for state broadcasting and UI display.</summary>
    GameMode Mode { get; }

    /// <summary>When true, bullets can damage players on the same team.</summary>
    bool IsFriendlyFireEnabled { get; }

    /// <summary>When true, GameRoom ticks the shrinking zone and applies out-of-zone damage.</summary>
    bool UseShrinkingZone { get; }

    /// <summary>When true, GameRoom spawns and processes powerup pickups.</summary>
    bool UsesPowerups { get; }

    /// <summary>Minimum number of players required to transition from WaitingForPlayers to Lobby.</summary>
    int MinPlayersToStart { get; }

    /// <summary>Minimum ticks between two shots for a single player.</summary>
    uint FireCooldownTicks { get; }

    /// <summary>Ticks remaining in the current match. 0 if this mode has no timer.</summary>
    int TicksRemaining { get; }

    /// <summary>Called once by GameRoom after construction to allow rules to populate ControlPoints etc.</summary>
    void Initialize(GameRoomState state);

    /// <summary>Returns the spawn position for a player being added.</summary>
    Vector2 GetSpawnPoint(int playerId, GameRoomState state);

    /// <summary>Called when a player is added. Rules assign teams and initialize per-player state.</summary>
    void OnPlayerAdded(int playerId, GameRoomState state);

    /// <summary>Called when a player is eliminated. Rules handle scoring and respawn queuing.</summary>
    void OnElimination(int eliminatedId, int killerId, uint currentTick, GameRoomState state);

    /// <summary>Called each tick during InProgress phase. Rules handle timers, zone capture, etc.</summary>
    void OnTick(uint currentTick, float deltaTime, GameRoomState state);

    /// <summary>Returns a GameOverResult when the win condition is met, or null to continue.</summary>
    GameOverResult? CheckWinCondition(GameRoomState state);

    /// <summary>Returns the leaderboard sorted according to mode-specific scoring.</summary>
    PlayerInfo[] GetLeaderboard(GameRoomState state);
}

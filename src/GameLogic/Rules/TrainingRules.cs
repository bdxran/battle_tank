using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using BattleTank.GameLogic.Shared;

namespace BattleTank.GameLogic.Rules;

/// <summary>
/// Training mode rules: starts immediately with 1 player, no shrinking zone,
/// bots respawn indefinitely, no win condition.
/// </summary>
public class TrainingRules : IBattleRules
{
    private static readonly Vector2[] SpawnPoints =
    [
        new(100, 100), new(900, 100), new(100, 900), new(900, 900),
        new(500, 100), new(500, 900), new(100, 500), new(900, 500),
        new(300, 300), new(700, 700),
    ];

    public GameMode Mode => GameMode.Training;
    public bool IsFriendlyFireEnabled => true;
    public bool UseShrinkingZone => false;
    public bool UsesPowerups => true;
    public int MinPlayersToStart => 1;

    public void Initialize(GameRoomState state) { }

    public Vector2 GetSpawnPoint(int playerId, GameRoomState state)
    {
        int index = state.Tanks.Count % SpawnPoints.Length;
        return SpawnPoints[index];
    }

    public void OnPlayerAdded(int playerId, GameRoomState state) { }

    public void OnElimination(int eliminatedId, int killerId, uint currentTick, GameRoomState state)
    {
        // Bots respawn quickly; human player respawns too so training is uninterrupted
        var spawnIndex = state.Tanks.Count % SpawnPoints.Length;
        var spawnPos = SpawnPoints[spawnIndex];
        uint respawnTick = currentTick + Constants.DeathmatchRespawnDelayTicks;
        state.RespawnQueue.Enqueue((eliminatedId, respawnTick, spawnPos));
    }

    public void OnTick(uint currentTick, float deltaTime, GameRoomState state) { }

    public GameOverResult? CheckWinCondition(GameRoomState state) => null; // Training never ends

    public PlayerInfo[] GetLeaderboard(GameRoomState state)
    {
        return state.Tanks.Keys
            .Select(id =>
            {
                var nick = state.PlayerNicknames.TryGetValue(id, out var n) ? n : $"Tank{id}";
                var kills = state.PlayerKills.TryGetValue(id, out var k) ? k : 0;
                return new PlayerInfo(id, nick, kills);
            })
            .ToArray();
    }
}

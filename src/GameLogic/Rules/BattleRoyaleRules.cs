using System.Collections.Generic;
using System.Numerics;
using BattleTank.GameLogic.Shared;

namespace BattleTank.GameLogic.Rules;

public class BattleRoyaleRules : IBattleRules
{
    private static readonly Vector2[] SpawnPoints =
    [
        new(100, 100), new(900, 100), new(100, 900), new(900, 900),
        new(500, 100), new(500, 900), new(100, 500), new(900, 500),
        new(300, 300), new(700, 700),
    ];

    public GameMode Mode => GameMode.BattleRoyale;
    public bool IsFriendlyFireEnabled => true;
    public bool UseShrinkingZone => true;
    public bool UsesPowerups => true;

    public void Initialize(GameRoomState state) { }

    public Vector2 GetSpawnPoint(int playerId, GameRoomState state)
    {
        int index = state.Tanks.Count % SpawnPoints.Length;
        return SpawnPoints[index];
    }

    public void OnPlayerAdded(int playerId, GameRoomState state)
    {
        state.PlayerTeams[playerId] = -1;
    }

    public void OnElimination(int eliminatedId, int killerId, uint currentTick, GameRoomState state)
    {
        if (killerId >= 0 && state.PlayerKills.ContainsKey(killerId))
            state.PlayerKills[killerId]++;
    }

    public void OnTick(uint currentTick, float deltaTime, GameRoomState state) { }

    public GameOverResult? CheckWinCondition(GameRoomState state)
    {
        int aliveCount = 0;
        int lastAliveId = -1;

        foreach (var (id, tank) in state.Tanks)
        {
            if (tank.IsAlive)
            {
                aliveCount++;
                lastAliveId = id;
            }
        }

        if (aliveCount <= 1)
            return new GameOverResult(lastAliveId >= 0 ? lastAliveId : null, null);

        return null;
    }

    public PlayerInfo[] GetLeaderboard(GameRoomState state)
    {
        var infos = new List<PlayerInfo>(state.PlayerKills.Count);
        foreach (var (id, kills) in state.PlayerKills)
        {
            var nickname = state.PlayerNicknames.TryGetValue(id, out var n) ? n : $"Tank{id}";
            infos.Add(new PlayerInfo(id, nickname, kills));
        }
        infos.Sort((a, b) => b.Kills.CompareTo(a.Kills));
        return infos.ToArray();
    }
}

using System;
using System.Collections.Generic;
using System.Numerics;
using BattleTank.GameLogic.Shared;

namespace BattleTank.GameLogic.Rules;

public class DeathmatchRules : IBattleRules
{
    private static readonly Vector2[] SpawnPoints =
    [
        new(100, 100), new(900, 100), new(100, 900), new(900, 900),
        new(500, 100), new(500, 900), new(100, 500), new(900, 500),
        new(300, 300), new(700, 700),
    ];

    private int _ticksRemaining;
    private bool _timeUp;

    public GameMode Mode => GameMode.Deathmatch;
    public bool IsFriendlyFireEnabled => true;
    public bool UseShrinkingZone => false;
    public bool UsesPowerups => true;
    public int MinPlayersToStart => Constants.MinPlayersToStart;
    public uint FireCooldownTicks => 10;

    public void Initialize(GameRoomState state)
    {
        _ticksRemaining = Constants.DeathmatchDurationTicks;
        _timeUp = false;
    }

    public Vector2 GetSpawnPoint(int playerId, GameRoomState state)
    {
        return SpawnPoints[Math.Abs(playerId) % SpawnPoints.Length];
    }

    public void OnPlayerAdded(int playerId, GameRoomState state)
    {
        state.PlayerTeams[playerId] = -1;
    }

    public void OnElimination(int eliminatedId, int killerId, uint currentTick, GameRoomState state)
    {
        if (killerId >= 0 && state.PlayerKills.ContainsKey(killerId))
            state.PlayerKills[killerId]++;

        var spawnPos = GetSpawnPoint(eliminatedId, state);
        state.RespawnQueue.Enqueue((eliminatedId, currentTick + (uint)Constants.DeathmatchRespawnDelayTicks, spawnPos));
    }

    public void OnTick(uint currentTick, float deltaTime, GameRoomState state)
    {
        if (_ticksRemaining > 0)
        {
            _ticksRemaining--;
            if (_ticksRemaining == 0)
                _timeUp = true;
        }
    }

    public GameOverResult? CheckWinCondition(GameRoomState state)
    {
        if (!_timeUp)
            return null;

        // Player with most kills wins
        int bestId = -1;
        int bestKills = -1;
        foreach (var (id, kills) in state.PlayerKills)
        {
            if (kills > bestKills)
            {
                bestKills = kills;
                bestId = id;
            }
        }

        return new GameOverResult(bestId >= 0 ? bestId : null, null);
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

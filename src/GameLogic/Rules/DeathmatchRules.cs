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

    private readonly int _configuredDurationTicks;
    private int _ticksRemaining;
    private bool _timeUp;

    public DeathmatchRules(int durationSeconds = 180)
    {
        _configuredDurationTicks = durationSeconds * Constants.TickRate;
    }

    public GameMode Mode => GameMode.Deathmatch;
    public bool IsFriendlyFireEnabled => true;
    public bool UseShrinkingZone => false;
    public bool UsesPowerups => true;
    public int MinPlayersToStart => Constants.MinPlayersToStart;
    public uint FireCooldownTicks => 10;
    public int TicksRemaining => _ticksRemaining;

    public void Initialize(GameRoomState state)
    {
        _ticksRemaining = _configuredDurationTicks;
        _timeUp = false;
    }

    public Vector2 GetSpawnPoint(int playerId, GameRoomState state)
    {
        return SafestSpawnPoint(playerId, state);
    }

    public void OnPlayerAdded(int playerId, GameRoomState state)
    {
        state.PlayerTeams[playerId] = -1;
        state.PlayerDeaths[playerId] = 0;
        state.PlayerAssists[playerId] = 0;
    }

    public void OnElimination(int eliminatedId, int killerId, uint currentTick, GameRoomState state)
    {
        if (state.PlayerKills.ContainsKey(killerId))
            state.PlayerKills[killerId]++;

        if (state.PlayerDeaths.ContainsKey(eliminatedId))
            state.PlayerDeaths[eliminatedId]++;

        state.RespawnQueue.Enqueue((eliminatedId, currentTick + (uint)Constants.DeathmatchRespawnDelayTicks));
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
            int deaths = state.PlayerDeaths.TryGetValue(id, out var d) ? d : 0;
            int assists = state.PlayerAssists.TryGetValue(id, out var a) ? a : 0;
            infos.Add(new PlayerInfo(id, nickname, kills, -1, deaths, assists));
        }
        infos.Sort((a, b) => b.Kills.CompareTo(a.Kills));
        return infos.ToArray();
    }

    private static Vector2 SafestSpawnPoint(int playerId, GameRoomState state)
    {
        Vector2 best = SpawnPoints[0];
        float bestMinDist = -1f;

        foreach (var candidate in SpawnPoints)
        {
            float minDist = float.MaxValue;
            foreach (var (id, tank) in state.Tanks)
            {
                if (!tank.IsAlive || id == playerId) continue;
                float dx = tank.Position.X - candidate.X;
                float dy = tank.Position.Y - candidate.Y;
                float d = dx * dx + dy * dy;
                if (d < minDist) minDist = d;
            }
            if (minDist == float.MaxValue) minDist = 0f;
            if (minDist > bestMinDist)
            {
                bestMinDist = minDist;
                best = candidate;
            }
        }
        return best;
    }
}

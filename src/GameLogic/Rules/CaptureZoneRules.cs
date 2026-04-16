using System;
using System.Collections.Generic;
using System.Numerics;
using BattleTank.GameLogic.Entities;
using BattleTank.GameLogic.Shared;

namespace BattleTank.GameLogic.Rules;

public class CaptureZoneRules : IBattleRules
{
    private static readonly Vector2[] SpawnPoints =
    [
        new(100, 100), new(900, 100), new(100, 900), new(900, 900),
        new(500, 100), new(500, 900), new(100, 500), new(900, 500),
        new(300, 300), new(700, 700),
    ];

    private static readonly Vector2[] ControlPointPositions =
    [
        new(500, 500), // center
        new(250, 250), // top-left
        new(750, 750), // bottom-right
    ];

    private readonly int _configuredDurationTicks;
    private readonly int _configuredScoreToWin;
    private int _ticksRemaining;
    private bool _gameOver;
    private GameOverResult? _result;
    private int?[] _previousControllingTeam = [];

    public CaptureZoneRules(int durationSeconds = 240, int scoreToWin = 1200)
    {
        _configuredDurationTicks = durationSeconds * Constants.TickRate;
        _configuredScoreToWin = scoreToWin;
    }

    public GameMode Mode => GameMode.CaptureZone;
    public bool IsFriendlyFireEnabled => false;
    public bool UseShrinkingZone => false;
    public bool UsesPowerups => false;
    public int MinPlayersToStart => Constants.MinPlayersToStart;
    public uint FireCooldownTicks => 10;
    public int TicksRemaining => _ticksRemaining;

    public void Initialize(GameRoomState state)
    {
        _ticksRemaining = _configuredDurationTicks;
        _gameOver = false;
        _result = null;

        state.ControlPoints.Clear();
        for (int i = 0; i < ControlPointPositions.Length; i++)
            state.ControlPoints.Add(new ControlPoint(i, ControlPointPositions[i], Constants.ControlPointRadius));

        _previousControllingTeam = new int?[ControlPointPositions.Length];
    }

    public Vector2 GetSpawnPoint(int playerId, GameRoomState state)
    {
        return SafestSpawnPoint(playerId, state);
    }

    public void OnPlayerAdded(int playerId, GameRoomState state)
    {
        int team0Count = 0;
        int team1Count = 0;
        foreach (var (_, t) in state.PlayerTeams)
        {
            if (t == 0) team0Count++;
            else if (t == 1) team1Count++;
        }

        int assignedTeam = team0Count <= team1Count ? 0 : 1;
        state.PlayerTeams[playerId] = assignedTeam;
        state.PlayerDeaths[playerId] = 0;
        state.PlayerAssists[playerId] = 0;
        state.PlayerZoneCaptured[playerId] = 0;

        if (!state.TeamScores.ContainsKey(0)) state.TeamScores[0] = 0;
        if (!state.TeamScores.ContainsKey(1)) state.TeamScores[1] = 0;
    }

    public void OnElimination(int eliminatedId, int killerId, uint currentTick, GameRoomState state)
    {
        if (state.PlayerKills.ContainsKey(killerId))
            state.PlayerKills[killerId]++;

        if (state.PlayerDeaths.ContainsKey(eliminatedId))
            state.PlayerDeaths[eliminatedId]++;

        state.RespawnQueue.Enqueue((eliminatedId, currentTick + (uint)Constants.CaptureZoneRespawnDelayTicks));
    }

    public void OnTick(uint currentTick, float deltaTime, GameRoomState state)
    {
        if (_gameOver) return;

        foreach (var cp in state.ControlPoints)
        {
            int? prevTeam = _previousControllingTeam[cp.Id];
            int? scoringTeam = cp.Tick(state.Tanks, deltaTime);

            if (cp.ControllingTeamId.HasValue && cp.ControllingTeamId != prevTeam)
                AwardZoneCaptureToPlayersInZone(cp, cp.ControllingTeamId.Value, state);

            _previousControllingTeam[cp.Id] = cp.ControllingTeamId;

            if (scoringTeam.HasValue && state.TeamScores.ContainsKey(scoringTeam.Value))
            {
                int team = scoringTeam.Value;
                state.TeamScores[team]++;

                if (state.TeamScores[team] >= _configuredScoreToWin)
                {
                    _gameOver = true;
                    _result = BuildResult(scoringTeam.Value, state);
                    return;
                }
            }
        }

        if (_ticksRemaining > 0)
        {
            _ticksRemaining--;
            if (_ticksRemaining == 0)
            {
                _gameOver = true;
                _result = BuildResultFromScores(state);
            }
        }
    }

    public GameOverResult? CheckWinCondition(GameRoomState state)
    {
        return _result;
    }

    public PlayerInfo[] GetLeaderboard(GameRoomState state)
    {
        var infos = new List<PlayerInfo>(state.PlayerKills.Count);
        foreach (var (id, kills) in state.PlayerKills)
        {
            var nickname = state.PlayerNicknames.TryGetValue(id, out var n) ? n : $"Tank{id}";
            int teamId = state.PlayerTeams.TryGetValue(id, out var t) ? t : -1;
            int deaths = state.PlayerDeaths.TryGetValue(id, out var d) ? d : 0;
            int assists = state.PlayerAssists.TryGetValue(id, out var a) ? a : 0;
            int zoneCaps = state.PlayerZoneCaptured.TryGetValue(id, out var z) ? z : 0;
            infos.Add(new PlayerInfo(id, nickname, kills, teamId, deaths, assists, zoneCaps));
        }
        infos.Sort((a, b) =>
        {
            int scoreA = state.TeamScores.TryGetValue(a.TeamId, out var sa) ? sa : 0;
            int scoreB = state.TeamScores.TryGetValue(b.TeamId, out var sb) ? sb : 0;
            if (scoreA != scoreB) return scoreB.CompareTo(scoreA);
            return b.Kills.CompareTo(a.Kills);
        });
        return infos.ToArray();
    }

    private static void AwardZoneCaptureToPlayersInZone(ControlPoint cp, int team, GameRoomState state)
    {
        foreach (var (id, tank) in state.Tanks)
        {
            if (!tank.IsAlive) continue;
            if (!state.PlayerTeams.TryGetValue(id, out int playerTeam) || playerTeam != team) continue;
            float dx = tank.Position.X - cp.Position.X;
            float dy = tank.Position.Y - cp.Position.Y;
            if (dx * dx + dy * dy <= cp.Radius * cp.Radius)
                state.PlayerZoneCaptured[id]++;
        }
    }

    private GameOverResult BuildResult(int winnerTeamId, GameRoomState state)
    {
        int? winnerPlayerId = null;
        foreach (var (id, tank) in state.Tanks)
        {
            if (tank.IsAlive && state.PlayerTeams.TryGetValue(id, out int t) && t == winnerTeamId)
            {
                winnerPlayerId = id;
                break;
            }
        }
        return new GameOverResult(winnerPlayerId, winnerTeamId);
    }

    private GameOverResult BuildResultFromScores(GameRoomState state)
    {
        int bestTeam = -1;
        int bestScore = -1;
        foreach (var (teamId, score) in state.TeamScores)
        {
            if (score > bestScore)
            {
                bestScore = score;
                bestTeam = teamId;
            }
        }
        return bestTeam >= 0 ? BuildResult(bestTeam, state) : new GameOverResult(null, null);
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

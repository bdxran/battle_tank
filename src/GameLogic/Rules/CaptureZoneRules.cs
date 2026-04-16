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

    private int _ticksRemaining;
    private bool _gameOver;
    private GameOverResult? _result;

    public GameMode Mode => GameMode.CaptureZone;
    public bool IsFriendlyFireEnabled => true;
    public bool UseShrinkingZone => false;
    public bool UsesPowerups => false;
    public int MinPlayersToStart => Constants.MinPlayersToStart;
    public uint FireCooldownTicks => 10;

    public void Initialize(GameRoomState state)
    {
        _ticksRemaining = Constants.CaptureZoneDurationTicks;
        _gameOver = false;
        _result = null;

        state.ControlPoints.Clear();
        for (int i = 0; i < ControlPointPositions.Length; i++)
            state.ControlPoints.Add(new ControlPoint(i, ControlPointPositions[i], Constants.ControlPointRadius));
    }

    public Vector2 GetSpawnPoint(int playerId, GameRoomState state)
    {
        return SpawnPoints[Math.Abs(playerId) % SpawnPoints.Length];
    }

    public void OnPlayerAdded(int playerId, GameRoomState state)
    {
        // Assign teams round-robin
        int team0Count = 0;
        int team1Count = 0;
        foreach (var (_, t) in state.PlayerTeams)
        {
            if (t == 0) team0Count++;
            else if (t == 1) team1Count++;
        }

        int assignedTeam = team0Count <= team1Count ? 0 : 1;
        state.PlayerTeams[playerId] = assignedTeam;

        if (!state.TeamScores.ContainsKey(0)) state.TeamScores[0] = 0;
        if (!state.TeamScores.ContainsKey(1)) state.TeamScores[1] = 0;
    }

    public void OnElimination(int eliminatedId, int killerId, uint currentTick, GameRoomState state)
    {
        if (killerId >= 0 && state.PlayerKills.ContainsKey(killerId))
            state.PlayerKills[killerId]++;
    }

    public void OnTick(uint currentTick, float deltaTime, GameRoomState state)
    {
        if (_gameOver) return;

        // Each tick a team controls a zone → +1 point (integer, no float rounding)
        foreach (var cp in state.ControlPoints)
        {
            int? scoringTeam = cp.Tick(state.Tanks, deltaTime);
            if (scoringTeam.HasValue && state.TeamScores.ContainsKey(scoringTeam.Value))
            {
                int team = scoringTeam.Value;
                state.TeamScores[team]++;

                if (state.TeamScores[team] >= Constants.CaptureZoneScoreToWin)
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
            infos.Add(new PlayerInfo(id, nickname, kills, teamId));
        }
        // Sort by team score descending, then kills descending
        infos.Sort((a, b) =>
        {
            int scoreA = state.TeamScores.TryGetValue(a.TeamId, out var sa) ? sa : 0;
            int scoreB = state.TeamScores.TryGetValue(b.TeamId, out var sb) ? sb : 0;
            if (scoreA != scoreB) return scoreB.CompareTo(scoreA);
            return b.Kills.CompareTo(a.Kills);
        });
        return infos.ToArray();
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
}

using System.Collections.Generic;
using System.Numerics;
using BattleTank.GameLogic.Shared;

namespace BattleTank.GameLogic.Rules;

public class TeamsRules : IBattleRules
{
    // Team 0 spawns on left/top, Team 1 on right/bottom
    private static readonly Vector2[] Team0Spawns =
    [
        new(100, 100), new(100, 500), new(100, 900), new(300, 300), new(300, 700),
    ];

    private static readonly Vector2[] Team1Spawns =
    [
        new(900, 100), new(900, 500), new(900, 900), new(700, 300), new(700, 700),
    ];

    private readonly Dictionary<int, int> _teamSpawnCounters = new() { [0] = 0, [1] = 0 };

    public GameMode Mode => GameMode.Teams;
    public bool IsFriendlyFireEnabled => false;
    public bool UseShrinkingZone => false;
    public bool UsesPowerups => true;
    public int MinPlayersToStart => Constants.MinPlayersToStart;
    public uint FireCooldownTicks => 10;

    public void Initialize(GameRoomState state) { }

    public Vector2 GetSpawnPoint(int playerId, GameRoomState state)
    {
        if (!state.PlayerTeams.TryGetValue(playerId, out int teamId) || teamId < 0)
            teamId = 0;

        var spawns = teamId == 0 ? Team0Spawns : Team1Spawns;
        int idx = _teamSpawnCounters[teamId] % spawns.Length;
        _teamSpawnCounters[teamId]++;
        return spawns[idx];
    }

    public void OnPlayerAdded(int playerId, GameRoomState state)
    {
        // Assign teams round-robin: count players already on each team
        int team0Count = 0;
        int team1Count = 0;
        foreach (var (_, t) in state.PlayerTeams)
        {
            if (t == 0) team0Count++;
            else if (t == 1) team1Count++;
        }

        int assignedTeam = team0Count <= team1Count ? 0 : 1;
        state.PlayerTeams[playerId] = assignedTeam;

        if (!state.TeamScores.ContainsKey(assignedTeam))
            state.TeamScores[assignedTeam] = 0;
    }

    public void OnElimination(int eliminatedId, int killerId, uint currentTick, GameRoomState state)
    {
        if (killerId >= 0 && state.PlayerKills.ContainsKey(killerId))
            state.PlayerKills[killerId]++;
    }

    public void OnTick(uint currentTick, float deltaTime, GameRoomState state) { }

    public GameOverResult? CheckWinCondition(GameRoomState state)
    {
        var aliveTeams = new HashSet<int>();

        foreach (var (id, tank) in state.Tanks)
        {
            if (tank.IsAlive && state.PlayerTeams.TryGetValue(id, out int teamId) && teamId >= 0)
                aliveTeams.Add(teamId);
        }

        if (aliveTeams.Count <= 1)
        {
            int? winnerTeam = aliveTeams.Count == 1 ? aliveTeams.GetEnumerator().Current : null;
            // Find any surviving player from the winning team as the representative winner
            int? winnerPlayerId = null;
            if (winnerTeam.HasValue)
            {
                foreach (var (id, tank) in state.Tanks)
                {
                    if (tank.IsAlive && state.PlayerTeams.TryGetValue(id, out int t) && t == winnerTeam)
                    {
                        winnerPlayerId = id;
                        break;
                    }
                }
            }
            return new GameOverResult(winnerPlayerId, winnerTeam);
        }

        return null;
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
        // Sort by team, then kills descending
        infos.Sort((a, b) =>
        {
            if (a.TeamId != b.TeamId) return a.TeamId.CompareTo(b.TeamId);
            return b.Kills.CompareTo(a.Kills);
        });
        return infos.ToArray();
    }
}

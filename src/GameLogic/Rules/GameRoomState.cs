using System.Collections.Generic;
using BattleTank.GameLogic.Entities;

namespace BattleTank.GameLogic.Rules;

/// <summary>
/// Shared context passed to IBattleRules. Holds references to GameRoom's internal collections.
/// Rules may mutate PlayerKills, PlayerTeams, TeamScores, and RespawnQueue.
/// </summary>
public class GameRoomState
{
    public IReadOnlyDictionary<int, TankEntity> Tanks { get; }
    public IReadOnlyDictionary<int, string> PlayerNicknames { get; }
    public Dictionary<int, int> PlayerKills { get; }
    public Dictionary<int, int> PlayerTeams { get; }
    public Dictionary<int, int> TeamScores { get; }
    public Queue<(int PlayerId, uint RespawnTick)> RespawnQueue { get; }
    public List<ControlPoint> ControlPoints { get; }

    public GameRoomState(
        IReadOnlyDictionary<int, TankEntity> tanks,
        IReadOnlyDictionary<int, string> playerNicknames,
        Dictionary<int, int> playerKills,
        Dictionary<int, int> playerTeams,
        Dictionary<int, int> teamScores,
        Queue<(int, uint)> respawnQueue,
        List<ControlPoint> controlPoints)
    {
        Tanks = tanks;
        PlayerNicknames = playerNicknames;
        PlayerKills = playerKills;
        PlayerTeams = playerTeams;
        TeamScores = teamScores;
        RespawnQueue = respawnQueue;
        ControlPoints = controlPoints;
    }
}

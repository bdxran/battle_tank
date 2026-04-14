using System.Threading.Tasks;
using BattleTank.GameLogic.Shared;

namespace BattleTank.GameLogic.Persistence;

public record LeaderboardEntry(int AccountId, string Username, int Wins, int Kills, int GamesPlayed);

public interface ILeaderboardService
{
    Task<LeaderboardEntry[]> GetLeaderboardAsync(GameMode mode, int limit = 20);
}

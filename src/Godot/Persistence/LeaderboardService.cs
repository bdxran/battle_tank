using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BattleTank.GameLogic.Persistence;
using BattleTank.GameLogic.Shared;

namespace BattleTank.Godot.Persistence;

public class LeaderboardService : ILeaderboardService
{
    private readonly BattleTankDbContext _db;

    public LeaderboardService(BattleTankDbContext db)
    {
        _db = db;
    }

    public async Task<LeaderboardEntry[]> GetLeaderboardAsync(GameMode mode, int limit = 20)
    {
        var modeKey = mode.ToString();
        return await _db.PlayerStats
            .Where(s => s.Mode == modeKey)
            .Join(_db.PlayerAccounts,
                  s => s.AccountId,
                  a => a.AccountId,
                  (s, a) => new LeaderboardEntry(a.AccountId, a.Username, s.Wins, s.Kills, s.GamesPlayed))
            .OrderByDescending(e => e.Wins)
            .ThenByDescending(e => e.Kills)
            .Take(limit)
            .ToArrayAsync();
    }
}

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BattleTank.GameLogic.Persistence;
using BattleTank.GameLogic.Persistence.Models;
using BattleTank.GameLogic.Shared;

namespace BattleTank.Godot.Persistence;

public class PlayerRepository : IPlayerRepository
{
    private readonly BattleTankDbContext _db;

    public PlayerRepository(BattleTankDbContext db)
    {
        _db = db;
    }

    public Task<PlayerAccount?> FindByUsernameAsync(string username)
        => _db.PlayerAccounts.FirstOrDefaultAsync(p => p.Username == username);

    public async Task<PlayerAccount> CreateAccountAsync(string username, string passwordHash, string avatarSeed)
    {
        var account = new PlayerAccount
        {
            Username = username,
            PasswordHash = passwordHash,
            AvatarSeed = avatarSeed,
            CreatedAt = DateTime.UtcNow,
        };
        _db.PlayerAccounts.Add(account);
        await _db.SaveChangesAsync();
        return account;
    }

    public async Task UpdateStatsAsync(int accountId, GameMode mode, bool won, int kills, int durationSeconds)
    {
        var modeKey = mode.ToString();
        var stats = await _db.PlayerStats.FirstOrDefaultAsync(s => s.AccountId == accountId && s.Mode == modeKey);

        if (stats == null)
        {
            stats = new PlayerStats { AccountId = accountId, Mode = modeKey };
            _db.PlayerStats.Add(stats);
        }

        stats.GamesPlayed++;
        stats.Kills += kills;
        if (!won) stats.Deaths++;
        if (won) stats.Wins++;
        stats.PlaytimeSeconds += durationSeconds;

        _db.GameRecords.Add(new GameRecord
        {
            AccountId = accountId,
            Mode = modeKey,
            Won = won,
            Kills = kills,
            DurationSeconds = durationSeconds,
            PlayedAt = DateTime.UtcNow,
        });

        await _db.SaveChangesAsync();
    }

    public async Task<PlayerStats[]> GetStatsAsync(int accountId)
        => await _db.PlayerStats.Where(s => s.AccountId == accountId).ToArrayAsync();
}

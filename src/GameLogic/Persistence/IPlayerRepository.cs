using System.Threading.Tasks;
using BattleTank.GameLogic.Persistence.Models;
using BattleTank.GameLogic.Shared;

namespace BattleTank.GameLogic.Persistence;

public interface IPlayerRepository
{
    Task<PlayerAccount?> FindByUsernameAsync(string username);
    Task<PlayerAccount> CreateAccountAsync(string username, string passwordHash, string avatarSeed);
    Task UpdateStatsAsync(int accountId, GameMode mode, bool won, int kills, int durationSeconds);
    Task<PlayerStats[]> GetStatsAsync(int accountId);
}

namespace BattleTank.GameLogic.Persistence.Models;

public class PlayerStats
{
    public int StatsId { get; set; }
    public int AccountId { get; set; }
    public string Mode { get; set; } = string.Empty;
    public int Kills { get; set; }
    public int Deaths { get; set; }
    public int Wins { get; set; }
    public int GamesPlayed { get; set; }
    public long PlaytimeSeconds { get; set; }
}

using System;

namespace BattleTank.GameLogic.Persistence.Models;

public class GameRecord
{
    public int GameRecordId { get; set; }
    public int AccountId { get; set; }
    public string Mode { get; set; } = string.Empty;
    public bool Won { get; set; }
    public int Kills { get; set; }
    public int DurationSeconds { get; set; }
    public DateTime PlayedAt { get; set; }
}

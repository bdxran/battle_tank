using System;

namespace BattleTank.GameLogic.Persistence.Models;

public class PlayerAccount
{
    public int AccountId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string AvatarSeed { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

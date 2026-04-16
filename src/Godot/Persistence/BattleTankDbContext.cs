using Microsoft.EntityFrameworkCore;
using BattleTank.GameLogic.Persistence.Models;

namespace BattleTank.Godot.Persistence;

public class BattleTankDbContext : DbContext
{
    private readonly string _dbPath;

    public BattleTankDbContext(string dbPath = "battle_tank.db")
    {
        _dbPath = dbPath;
    }

    public DbSet<PlayerAccount> PlayerAccounts { get; set; } = null!;
    public DbSet<PlayerStats> PlayerStats { get; set; } = null!;
    public DbSet<GameRecord> GameRecords { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"Data Source={_dbPath}");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PlayerAccount>()
            .HasKey(p => p.AccountId);

        modelBuilder.Entity<PlayerAccount>()
            .HasIndex(p => p.Username)
            .IsUnique();

        modelBuilder.Entity<PlayerStats>()
            .HasKey(p => p.StatsId);

        modelBuilder.Entity<PlayerStats>()
            .HasIndex(p => new { p.AccountId, p.Mode })
            .IsUnique();

        modelBuilder.Entity<GameRecord>()
            .HasKey(p => p.GameRecordId);
    }
}

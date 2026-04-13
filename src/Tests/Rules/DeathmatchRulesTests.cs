using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using BattleTank.GameLogic.Network;
using BattleTank.GameLogic.Rules;
using BattleTank.GameLogic.Shared;

namespace BattleTank.Tests.Rules;

[TestFixture]
public class DeathmatchRulesTests
{
    private GameRoom CreateRoom() => new(NullLogger<GameRoom>.Instance, new DeathmatchRules());

    private static void AdvanceThroughLobby(GameRoom room)
    {
        float dt = 1f / Constants.TickRate;
        for (int i = 0; i <= Constants.LobbyCountdownTicks; i++)
            room.Tick(dt);
    }

    [Test]
    public void Mode_IsDeathmatch()
    {
        var room = CreateRoom();
        var state = room.GetFullState();
        Assert.That(state.Mode, Is.EqualTo(GameMode.Deathmatch));
    }

    [Test]
    public void TimerExpiry_EndsGame()
    {
        var room = CreateRoom();
        room.AddPlayer(1);
        room.AddPlayer(2);
        AdvanceThroughLobby(room);

        Assert.That(room.Phase, Is.EqualTo(GamePhase.InProgress));

        float dt = 1f / Constants.TickRate;
        for (int i = 0; i <= Constants.DeathmatchDurationTicks; i++)
            room.Tick(dt);

        Assert.That(room.Phase, Is.EqualTo(GamePhase.GameOver));
    }

    [Test]
    public void TimerExpiry_PlayerWithMostKills_Wins()
    {
        var room = CreateRoom();
        var r1 = room.AddPlayer(1, "Killer");
        var r2 = room.AddPlayer(2, "Victim");
        AdvanceThroughLobby(room);

        // Give player 1 a kill by shooting player 2
        r1.Value.SetPosition(new System.Numerics.Vector2(300f, 300f));
        r2.Value.SetPosition(new System.Numerics.Vector2(300f, 270f));

        float dt = 1f / Constants.TickRate;
        room.ApplyInput(1, new PlayerInput(1, InputFlags.Fire, 1));
        for (int t = 0; t < 50; t++)
            room.Tick(dt);

        // Advance to end of deathmatch timer
        for (int i = 0; i <= Constants.DeathmatchDurationTicks; i++)
            room.Tick(dt);

        Assert.That(room.Phase, Is.EqualTo(GamePhase.GameOver));
        Assert.That(room.WinnerId, Is.EqualTo(1));
    }

    [Test]
    public void EliminatedPlayer_Respawns_AfterDelay()
    {
        var room = CreateRoom();
        var r1 = room.AddPlayer(1, "Shooter");
        var r2 = room.AddPlayer(2, "Target");
        AdvanceThroughLobby(room);

        // Kill player 2 quickly
        r1.Value.SetPosition(new System.Numerics.Vector2(300f, 300f));
        r2.Value.SetPosition(new System.Numerics.Vector2(300f, 270f));

        float dt = 1f / Constants.TickRate;
        room.ApplyInput(1, new PlayerInput(1, InputFlags.Fire, 1));
        // Fire 4 shots worth (40 ticks at cooldown 10)
        for (int t = 0; t < 50; t++)
            room.Tick(dt);

        // Player 2 should be dead
        Assert.That(r2.Value.IsAlive, Is.False);

        // Advance through respawn delay
        for (int t = 0; t <= Constants.DeathmatchRespawnDelayTicks; t++)
            room.Tick(dt);

        // Player 2 should be alive again
        Assert.That(r2.Value.IsAlive, Is.True);
        Assert.That(r2.Value.Health, Is.EqualTo(Constants.TankMaxHealth));
    }

    [Test]
    public void EliminatedPlayer_GameDoesNotEnd_InDeathmatch()
    {
        var room = CreateRoom();
        var r1 = room.AddPlayer(1, "Shooter");
        var r2 = room.AddPlayer(2, "Target");
        AdvanceThroughLobby(room);

        r1.Value.SetPosition(new System.Numerics.Vector2(300f, 300f));
        r2.Value.SetPosition(new System.Numerics.Vector2(300f, 270f));

        float dt = 1f / Constants.TickRate;
        room.ApplyInput(1, new PlayerInput(1, InputFlags.Fire, 1));
        for (int t = 0; t < 50; t++)
            room.Tick(dt);

        // Even though p2 is dead, game should still be InProgress (deathmatch has respawns)
        Assert.That(room.Phase, Is.EqualTo(GamePhase.InProgress));
    }

    [Test]
    public void GetLeaderboard_SortedByKillsDescending()
    {
        var room = CreateRoom();
        room.AddPlayer(1, "Alpha");
        room.AddPlayer(2, "Beta");
        AdvanceThroughLobby(room);

        var lb = room.GetLeaderboard();
        for (int i = 0; i < lb.Length - 1; i++)
            Assert.That(lb[i].Kills, Is.GreaterThanOrEqualTo(lb[i + 1].Kills));
    }
}

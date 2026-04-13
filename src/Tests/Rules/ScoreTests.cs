using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using BattleTank.GameLogic.Network;
using BattleTank.GameLogic.Rules;
using BattleTank.GameLogic.Shared;

namespace BattleTank.Tests.Rules;

[TestFixture]
public class ScoreTests
{
    private GameRoom CreateRoom() => new(NullLogger<GameRoom>.Instance);

    private static void AdvanceThroughLobby(GameRoom room)
    {
        float dt = 1f / Constants.TickRate;
        for (int i = 0; i <= Constants.LobbyCountdownTicks; i++)
            room.Tick(dt);
    }

    [Test]
    public void Kill_IncrementsKillerScore()
    {
        var room = CreateRoom();
        var result1 = room.AddPlayer(1, "Killer");
        var result2 = room.AddPlayer(2, "Victim");
        AdvanceThroughLobby(room);

        // Place both tanks inside the safe zone (center 500,500 radius 450) to avoid zone damage
        // Tank 1 at (300,300) facing up (rotation=0), forward = (0,-1)
        // Tank 2 at (300,270) — 30px directly in front, within bullet range, no walls nearby
        result1.Value.SetPosition(new System.Numerics.Vector2(300f, 300f));
        result2.Value.SetPosition(new System.Numerics.Vector2(300f, 270f));

        // Fire 4 shots to kill (25 dmg each, 100 HP total), fire stays in input buffer
        float dt = 1f / Constants.TickRate;
        room.ApplyInput(1, new PlayerInput(1, InputFlags.Fire, 1));
        for (int t = 0; t < 50; t++) // 50 ticks — enough for 4 shots at cooldown 10
            room.Tick(dt);

        var leaderboard = room.GetLeaderboard();
        var killer = System.Array.Find(leaderboard, p => p.Id == 1);
        Assert.That(killer, Is.Not.Null);
        Assert.That(killer!.Kills, Is.EqualTo(1));
    }

    [Test]
    public void GetLeaderboard_SortedByKillsDescending()
    {
        var room = CreateRoom();
        room.AddPlayer(1, "Alpha");
        room.AddPlayer(2, "Beta");
        AdvanceThroughLobby(room);

        var leaderboard = room.GetLeaderboard();

        Assert.That(leaderboard.Length, Is.EqualTo(2));
        for (int i = 0; i < leaderboard.Length - 1; i++)
            Assert.That(leaderboard[i].Kills, Is.GreaterThanOrEqualTo(leaderboard[i + 1].Kills));
    }

    [Test]
    public void GetLeaderboard_ContainsNicknames()
    {
        var room = CreateRoom();
        room.AddPlayer(1, "AlphaPlayer");
        room.AddPlayer(2, "BetaPlayer");
        AdvanceThroughLobby(room);

        var leaderboard = room.GetLeaderboard();
        var nicknames = System.Array.ConvertAll(leaderboard, p => p.Nickname);

        Assert.That(nicknames, Does.Contain("AlphaPlayer"));
        Assert.That(nicknames, Does.Contain("BetaPlayer"));
    }

    [Test]
    public void AddPlayer_WithNickname_StoredCorrectly()
    {
        var room = CreateRoom();
        room.AddPlayer(42, "TestNick");

        Assert.That(room.PlayerNicknames[42], Is.EqualTo("TestNick"));
    }

    [Test]
    public void AddPlayer_WithoutNickname_UsesDefaultName()
    {
        var room = CreateRoom();
        room.AddPlayer(7);

        Assert.That(room.PlayerNicknames[7], Is.EqualTo("Tank7"));
    }

    [Test]
    public void GameStateFull_ContainsPlayerInfos()
    {
        var room = CreateRoom();
        room.AddPlayer(1, "Alice");
        room.AddPlayer(2, "Bob");

        var state = room.GetFullState();
        Assert.That(state.Players.Length, Is.EqualTo(2));
        var names = System.Array.ConvertAll(state.Players, p => p.Nickname);
        Assert.That(names, Does.Contain("Alice"));
        Assert.That(names, Does.Contain("Bob"));
    }
}

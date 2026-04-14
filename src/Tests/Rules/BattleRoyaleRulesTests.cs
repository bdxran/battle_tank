using System.Numerics;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using BattleTank.GameLogic.Network;
using BattleTank.GameLogic.Rules;
using BattleTank.GameLogic.Shared;

namespace BattleTank.Tests.Rules;

[TestFixture]
public class BattleRoyaleRulesTests
{
    private GameRoom CreateRoom() => new(NullLogger<GameRoom>.Instance, new BattleRoyaleRules());

    private static void AdvanceThroughLobby(GameRoom room)
    {
        float dt = 1f / Constants.TickRate;
        for (int i = 0; i <= Constants.LobbyCountdownTicks; i++)
            room.Tick(dt);
    }

    [Test]
    public void Mode_IsBattleRoyale()
    {
        var room = CreateRoom();
        var state = room.GetFullState();
        Assert.That(state.Mode, Is.EqualTo(GameMode.BattleRoyale));
    }

    [Test]
    public void OnPlayerAdded_SetsTeamToMinusOne()
    {
        var room = CreateRoom();
        room.AddPlayer(1, "Tank1");
        var state = room.GetFullState();
        Assert.That(state.Players[0].TeamId, Is.EqualTo(-1));
    }

    [Test]
    public void GetSpawnPoint_ReturnsPointInMapBounds()
    {
        var room = CreateRoom();
        room.AddPlayer(1, "Tank1");
        room.AddPlayer(2, "Tank2");

        // Spawn points are within the 100–900 range used in BattleRoyaleRules
        var full = room.GetFullState();
        foreach (var tank in full.Tanks)
        {
            Assert.That(tank.X, Is.InRange(0f, 1000f));
            Assert.That(tank.Y, Is.InRange(0f, 1000f));
        }
    }

    [Test]
    public void OnElimination_IncrementsKillerKills()
    {
        var room = CreateRoom();
        var r1 = room.AddPlayer(1, "Shooter");
        var r2 = room.AddPlayer(2, "Target");
        AdvanceThroughLobby(room);

        r1.Value.SetPosition(new Vector2(300f, 300f));
        r2.Value.SetPosition(new Vector2(300f, 270f));

        float dt = 1f / Constants.TickRate;
        room.ApplyInput(1, new PlayerInput(1, InputFlags.Fire, 1));
        for (int t = 0; t < 50; t++)
            room.Tick(dt);

        var lb = room.GetLeaderboard();
        var shooter = System.Array.Find(lb, p => p.Id == 1);
        Assert.That(shooter, Is.Not.Null);
        Assert.That(shooter!.Kills, Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public void CheckWinCondition_MultipleAlive_GameContinues()
    {
        var room = CreateRoom();
        room.AddPlayer(1, "Tank1");
        room.AddPlayer(2, "Tank2");
        AdvanceThroughLobby(room);

        Assert.That(room.Phase, Is.EqualTo(GamePhase.InProgress));
    }

    [Test]
    public void CheckWinCondition_OneAlive_EndsGame()
    {
        var room = CreateRoom();
        var r1 = room.AddPlayer(1, "Shooter");
        var r2 = room.AddPlayer(2, "Target");
        AdvanceThroughLobby(room);

        r1.Value.SetPosition(new Vector2(300f, 300f));
        r2.Value.SetPosition(new Vector2(300f, 270f));

        float dt = 1f / Constants.TickRate;
        room.ApplyInput(1, new PlayerInput(1, InputFlags.Fire, 1));
        for (int t = 0; t < 50; t++)
            room.Tick(dt);

        Assert.That(room.Phase, Is.EqualTo(GamePhase.GameOver));
        Assert.That(room.WinnerId, Is.EqualTo(1));
    }

    [Test]
    public void GetLeaderboard_SortedByKillsDescending()
    {
        var room = CreateRoom();
        room.AddPlayer(1, "Alpha");
        room.AddPlayer(2, "Beta");
        room.AddPlayer(3, "Gamma");
        AdvanceThroughLobby(room);

        var lb = room.GetLeaderboard();
        for (int i = 0; i < lb.Length - 1; i++)
            Assert.That(lb[i].Kills, Is.GreaterThanOrEqualTo(lb[i + 1].Kills));
    }
}

using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using BattleTank.GameLogic.Rules;
using BattleTank.GameLogic.Shared;

namespace BattleTank.Tests.Rules;

[TestFixture]
public class CaptureZoneRulesTests
{
    private GameRoom CreateRoom() => new(NullLogger<GameRoom>.Instance, new CaptureZoneRules());

    private static void AdvanceThroughLobby(GameRoom room)
    {
        float dt = 1f / Constants.TickRate;
        for (int i = 0; i <= Constants.LobbyCountdownTicks; i++)
            room.Tick(dt);
    }

    [Test]
    public void Mode_IsCaptureZone()
    {
        var room = CreateRoom();
        var state = room.GetFullState();
        Assert.That(state.Mode, Is.EqualTo(GameMode.CaptureZone));
    }

    [Test]
    public void GetFullState_HasThreeControlPoints()
    {
        var room = CreateRoom();
        room.AddPlayer(1);
        room.AddPlayer(2);

        var state = room.GetFullState();
        Assert.That(state.ControlPoints, Is.Not.Null);
        Assert.That(state.ControlPoints!.Length, Is.EqualTo(3));
    }

    [Test]
    public void ControlPoints_InitiallyNeutral()
    {
        var room = CreateRoom();
        room.AddPlayer(1);
        room.AddPlayer(2);

        var state = room.GetFullState();
        foreach (var cp in state.ControlPoints!)
        {
            Assert.That(cp.ControllingTeamId, Is.Null);
            Assert.That(cp.CaptureProgress, Is.EqualTo(0f));
        }
    }

    [Test]
    public void TimerExpiry_EndsGame()
    {
        var room = CreateRoom();
        room.AddPlayer(1);
        room.AddPlayer(2);
        AdvanceThroughLobby(room);

        float dt = 1f / Constants.TickRate;
        for (int i = 0; i <= Constants.CaptureZoneDurationTicks; i++)
            room.Tick(dt);

        Assert.That(room.Phase, Is.EqualTo(GamePhase.GameOver));
    }

    [Test]
    public void TeamInsideZone_AccumulatesScore()
    {
        var room = CreateRoom();
        var r1 = room.AddPlayer(1, "T0");
        var r2 = room.AddPlayer(2, "T1");
        AdvanceThroughLobby(room);

        // Place p1 (team 0) inside the center control point (500,500)
        r1.Value.SetPosition(new System.Numerics.Vector2(500f, 500f));
        // Place p2 (team 1) far away
        r2.Value.SetPosition(new System.Numerics.Vector2(100f, 100f));

        float dt = 1f / Constants.TickRate;
        // Tick enough to capture the zone and score some points
        // CaptureRatePerSecond=10, to reach CaptureProgress=1 we need 100/10=10 seconds
        // Actually CaptureProgress rate = CaptureRatePerSecond / 100 per second = 0.1 per second
        // So 10 seconds = 200 ticks
        for (int t = 0; t < Constants.TickRate * 13; t++)
            room.Tick(dt);

        var scores = room.TeamScores;
        // Team 0 should have scored something
        Assert.That(scores.ContainsKey(0), Is.True);
        Assert.That(scores[0], Is.GreaterThan(0));
        // Team 1 should have 0 points
        Assert.That(scores.TryGetValue(1, out int s1) ? s1 : 0, Is.EqualTo(0));
    }

    [Test]
    public void GetLeaderboard_HigherTeamScoreFirst()
    {
        var room = CreateRoom();
        var r1 = room.AddPlayer(1, "T0");
        var r2 = room.AddPlayer(2, "T1");
        AdvanceThroughLobby(room);

        // Put p1 (team 0) on center point, p2 far away
        r1.Value.SetPosition(new System.Numerics.Vector2(500f, 500f));
        r2.Value.SetPosition(new System.Numerics.Vector2(100f, 100f));

        float dt = 1f / Constants.TickRate;
        for (int t = 0; t < Constants.TickRate * 13; t++)
            room.Tick(dt);

        var lb = room.GetLeaderboard();
        // p1 (team 0, has score) should come before p2 (team 1, no score)
        Assert.That(lb[0].Id, Is.EqualTo(1));
    }

    [Test]
    public void Reset_ClearsControlPoints()
    {
        var room = CreateRoom();
        room.AddPlayer(1);
        room.AddPlayer(2);
        AdvanceThroughLobby(room);

        room.Reset();

        // After reset and re-initialize, control points are back to 3 neutral
        room.AddPlayer(1);
        room.AddPlayer(2);
        var state = room.GetFullState();
        Assert.That(state.ControlPoints!.Length, Is.EqualTo(3));
        foreach (var cp in state.ControlPoints!)
            Assert.That(cp.ControllingTeamId, Is.Null);
    }
}

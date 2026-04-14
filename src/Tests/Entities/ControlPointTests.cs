using System.Collections.Generic;
using System.Numerics;
using NUnit.Framework;
using BattleTank.GameLogic.Entities;
using BattleTank.GameLogic.Shared;

namespace BattleTank.Tests.Entities;

[TestFixture]
public class ControlPointTests
{
    private static TankEntity MakeTank(int id, int teamId, float x, float y)
    {
        var tank = new TankEntity(id, new Vector2(x, y));
        tank.TeamId = teamId;
        return tank;
    }

    [Test]
    public void InitialState_IsNeutral()
    {
        var cp = new ControlPoint(0, new Vector2(500, 500), 80f);

        Assert.That(cp.ControllingTeamId, Is.Null);
        Assert.That(cp.CaptureProgress, Is.EqualTo(0f));
    }

    [Test]
    public void Tick_WithTeamInsideZone_IncreasesProgress()
    {
        var cp = new ControlPoint(0, new Vector2(500, 500), 80f);
        var tanks = new Dictionary<int, TankEntity>
        {
            [1] = MakeTank(1, 0, 500f, 500f),
        };

        cp.Tick(tanks, 1f / Constants.TickRate);

        Assert.That(cp.CaptureProgress, Is.GreaterThan(0f));
    }

    [Test]
    public void Tick_WhenContested_ProgressDoesNotIncrease()
    {
        var cp = new ControlPoint(0, new Vector2(500, 500), 80f);
        var tanks = new Dictionary<int, TankEntity>
        {
            [1] = MakeTank(1, 0, 500f, 500f),
            [2] = MakeTank(2, 1, 500f, 510f),
        };

        cp.Tick(tanks, 1f / Constants.TickRate);

        Assert.That(cp.CaptureProgress, Is.EqualTo(0f));
        Assert.That(cp.ControllingTeamId, Is.Null);
    }

    [Test]
    public void Tick_FullCapture_SetsControllingTeam()
    {
        var cp = new ControlPoint(0, new Vector2(500, 500), 80f);
        var tanks = new Dictionary<int, TankEntity>
        {
            [1] = MakeTank(1, 0, 500f, 500f),
        };

        // Tick enough for full capture: CaptureRatePerSecond/100 = 0.1/s → need 10s = 200 ticks; use 13s for margin
        for (int i = 0; i < Constants.TickRate * 13; i++)
            cp.Tick(tanks, 1f / Constants.TickRate);

        Assert.That(cp.ControllingTeamId, Is.EqualTo(0));
        Assert.That(cp.CaptureProgress, Is.EqualTo(1f));
    }

    [Test]
    public void Tick_ControlledZone_ReturnsControllingTeam()
    {
        var cp = new ControlPoint(0, new Vector2(500, 500), 80f);
        var tanks = new Dictionary<int, TankEntity>
        {
            [1] = MakeTank(1, 0, 500f, 500f),
        };

        // Fully capture: need 10s = 200 ticks; use 13s for margin
        for (int i = 0; i < Constants.TickRate * 13; i++)
            cp.Tick(tanks, 1f / Constants.TickRate);

        // Even with no one in zone, controlling team still "scores" (holds the point)
        var emptyTanks = new Dictionary<int, TankEntity>();
        int? scorer = cp.Tick(emptyTanks, 1f / Constants.TickRate);

        Assert.That(scorer, Is.EqualTo(0));
    }

    [Test]
    public void GetSnapshot_ReflectsCurrentState()
    {
        var cp = new ControlPoint(1, new Vector2(300, 400), 80f);
        var snap = cp.GetSnapshot();

        Assert.That(snap.Id, Is.EqualTo(1));
        Assert.That(snap.X, Is.EqualTo(300f));
        Assert.That(snap.Y, Is.EqualTo(400f));
        Assert.That(snap.Radius, Is.EqualTo(80f));
        Assert.That(snap.ControllingTeamId, Is.Null);
        Assert.That(snap.CaptureProgress, Is.EqualTo(0f));
    }

    [Test]
    public void Tick_DeadTank_DoesNotCapture()
    {
        var cp = new ControlPoint(0, new Vector2(500, 500), 80f);
        var tank = MakeTank(1, 0, 500f, 500f);
        tank.TakeDamage(Constants.TankMaxHealth); // kill it
        var tanks = new Dictionary<int, TankEntity> { [1] = tank };

        cp.Tick(tanks, 1f / Constants.TickRate);

        Assert.That(cp.CaptureProgress, Is.EqualTo(0f));
    }
}

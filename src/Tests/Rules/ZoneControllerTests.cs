using System.Numerics;
using NUnit.Framework;
using BattleTank.GameLogic.Entities;
using BattleTank.GameLogic.Rules;
using BattleTank.GameLogic.Shared;

namespace BattleTank.Tests.Rules;

[TestFixture]
public class ZoneControllerTests
{
    [Test]
    public void Tick_BeforeShrinkInterval_RadiusUnchanged()
    {
        var zone = new ZoneController();
        zone.Tick(Constants.ZoneShrinkInterval - 1f, []);
        Assert.That(zone.GetSnapshot().Radius, Is.EqualTo(Constants.ZoneInitialRadius));
    }

    [Test]
    public void Tick_AfterShrinkInterval_RadiusDecreases()
    {
        var zone = new ZoneController();
        zone.Tick(Constants.ZoneShrinkInterval, []);
        Assert.That(zone.GetSnapshot().Radius, Is.EqualTo(Constants.ZoneInitialRadius - Constants.ZoneShrinkAmount));
    }

    [Test]
    public void Tick_RadiusNeverBelowMinRadius()
    {
        var zone = new ZoneController();
        // Shrink many times
        for (int i = 0; i < 20; i++)
            zone.Tick(Constants.ZoneShrinkInterval, []);
        Assert.That(zone.GetSnapshot().Radius, Is.GreaterThanOrEqualTo(Constants.ZoneMinRadius));
    }

    [Test]
    public void Tick_TankOutsideZone_TakesDamage()
    {
        var zone = new ZoneController();
        // Place tank far outside zone
        var tank = new TankEntity(1, new Vector2(10000f, 10000f));
        zone.Tick(0.1f, [tank]);
        Assert.That(tank.Health, Is.LessThan(Constants.TankMaxHealth));
    }

    [Test]
    public void Tick_TankInsideZone_NoDamage()
    {
        var zone = new ZoneController();
        // Place tank at center (always inside)
        var tank = new TankEntity(1, new Vector2(Constants.MapWidth / 2f, Constants.MapHeight / 2f));
        zone.Tick(1f, [tank]);
        Assert.That(tank.Health, Is.EqualTo(Constants.TankMaxHealth));
    }

    [Test]
    public void Reset_RadiusBackToInitial()
    {
        var zone = new ZoneController();
        zone.Tick(Constants.ZoneShrinkInterval, []);
        zone.Reset();
        Assert.That(zone.GetSnapshot().Radius, Is.EqualTo(Constants.ZoneInitialRadius));
    }
}

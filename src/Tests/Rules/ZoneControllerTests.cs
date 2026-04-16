using System;
using System.Numerics;
using NUnit.Framework;
using BattleTank.GameLogic.Entities;
using BattleTank.GameLogic.Rules;
using BattleTank.GameLogic.Shared;

namespace BattleTank.Tests.Rules;

[TestFixture]
public class ZoneControllerTests
{
    private static ZoneController ActiveZone() => new(activationDelay: 0f);

    [Test]
    public void Tick_BeforeShrinkInterval_RadiusUnchanged()
    {
        var zone = ActiveZone();
        zone.Tick(Constants.ZoneShrinkInterval - 1f, []);
        Assert.That(zone.GetSnapshot().Radius, Is.EqualTo(Constants.ZoneInitialRadius));
    }

    [Test]
    public void Tick_AfterShrinkInterval_RadiusDecreases()
    {
        var zone = ActiveZone();
        zone.Tick(Constants.ZoneShrinkInterval, []);
        Assert.That(zone.GetSnapshot().Radius, Is.EqualTo(Constants.ZoneInitialRadius - Constants.ZoneShrinkAmount));
    }

    [Test]
    public void Tick_RadiusNeverBelowMinRadius()
    {
        var zone = ActiveZone();
        // Shrink many times — (450-50)/80 = 5 steps to reach min; use 4× that to be safe
        int maxShrinks = (int)Math.Ceiling((Constants.ZoneInitialRadius - Constants.ZoneMinRadius) / Constants.ZoneShrinkAmount) * 4;
        for (int i = 0; i < maxShrinks; i++)
            zone.Tick(Constants.ZoneShrinkInterval, []);
        Assert.That(zone.GetSnapshot().Radius, Is.GreaterThanOrEqualTo(Constants.ZoneMinRadius));
    }

    [Test]
    public void Tick_TankOutsideZone_TakesDamage()
    {
        var zone = ActiveZone();
        var tank = new TankEntity(1, new Vector2(10000f, 10000f));
        zone.Tick(0.1f, [tank]);
        Assert.That(tank.Health, Is.LessThan(Constants.TankMaxHealth));
    }

    [Test]
    public void Tick_TankInsideZone_NoDamage()
    {
        var zone = ActiveZone();
        var tank = new TankEntity(1, new Vector2(Constants.MapWidth / 2f, Constants.MapHeight / 2f));
        zone.Tick(1f, [tank]);
        Assert.That(tank.Health, Is.EqualTo(Constants.TankMaxHealth));
    }

    [Test]
    public void Reset_RadiusBackToInitial()
    {
        var zone = ActiveZone();
        zone.Tick(Constants.ZoneShrinkInterval, []);
        zone.Reset();
        Assert.That(zone.GetSnapshot().Radius, Is.EqualTo(Constants.ZoneInitialRadius));
    }

    [Test]
    public void Tick_DuringActivationDelay_SnapshotRadiusIsZero()
    {
        var zone = new ZoneController(activationDelay: 10f);
        zone.Tick(5f, []);
        Assert.That(zone.GetSnapshot().Radius, Is.EqualTo(0f));
    }

    [Test]
    public void Tick_DuringActivationDelay_TankOutsideZone_NoDamage()
    {
        var zone = new ZoneController(activationDelay: 10f);
        var tank = new TankEntity(1, new Vector2(10000f, 10000f));
        zone.Tick(5f, [tank]);
        Assert.That(tank.Health, Is.EqualTo(Constants.TankMaxHealth));
    }

    [Test]
    public void Tick_AfterActivationDelay_SnapshotRadiusIsInitial()
    {
        var zone = new ZoneController(activationDelay: 10f);
        zone.Tick(10.1f, []);
        Assert.That(zone.GetSnapshot().Radius, Is.EqualTo(Constants.ZoneInitialRadius));
    }

    [Test]
    public void Reset_AfterActivation_ReturnsToInactiveState()
    {
        var zone = new ZoneController(activationDelay: 10f);
        zone.Tick(15f, []);
        zone.Reset();
        Assert.That(zone.GetSnapshot().Radius, Is.EqualTo(0f));
    }
}

using System.Numerics;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using BattleTank.GameLogic.Entities;
using BattleTank.GameLogic.Network;
using BattleTank.GameLogic.Rules;
using BattleTank.GameLogic.Shared;

namespace BattleTank.Tests.Entities;

[TestFixture]
public class PowerupEntityTests
{
    [Test]
    public void PickUp_SetsIsPickedUp()
    {
        var powerup = new PowerupEntity(1, new Vector2(100, 100), PowerupType.ExtraAmmo);
        Assert.That(powerup.IsPickedUp, Is.False);

        powerup.PickUp();
        Assert.That(powerup.IsPickedUp, Is.True);
    }

    [Test]
    public void GetSnapshot_ReturnsCorrectData()
    {
        var powerup = new PowerupEntity(5, new Vector2(200f, 300f), PowerupType.Shield);
        var snapshot = powerup.GetSnapshot();

        Assert.That(snapshot.Id, Is.EqualTo(5));
        Assert.That(snapshot.X, Is.EqualTo(200f));
        Assert.That(snapshot.Y, Is.EqualTo(300f));
        Assert.That(snapshot.Type, Is.EqualTo((int)PowerupType.Shield));
    }

    [Test]
    public void GameStateDelta_PowerupsFieldIsAlwaysPresent()
    {
        var room = new GameRoom(NullLogger<GameRoom>.Instance);
        room.AddPlayer(1);
        room.AddPlayer(2);

        var delta = room.GetDeltaState(0);
        Assert.That(delta.Powerups, Is.Not.Null);
    }

    [Test]
    public void GameStateFull_PowerupsFieldIsAlwaysPresent()
    {
        var room = new GameRoom(NullLogger<GameRoom>.Instance);
        room.AddPlayer(1);
        room.AddPlayer(2);

        var full = room.GetFullState();
        Assert.That(full.Powerups, Is.Not.Null);
    }

    [Test]
    public void TankEntity_Heal_IncreasesHealth()
    {
        var tank = new TankEntity(1, new Vector2(100, 100));
        tank.TakeDamage(50);
        Assert.That(tank.Health, Is.EqualTo(50));

        tank.Heal(25);
        Assert.That(tank.Health, Is.EqualTo(75));
    }

    [Test]
    public void TankEntity_Heal_CappedAtMaxHealth()
    {
        var tank = new TankEntity(1, new Vector2(100, 100));
        tank.Heal(50); // already at max

        Assert.That(tank.Health, Is.EqualTo(Constants.TankMaxHealth));
    }

    [Test]
    public void TankEntity_SpeedBoost_DoublesSpeed()
    {
        var tank = new TankEntity(1, new Vector2(100, 100));
        Assert.That(tank.SpeedMultiplier, Is.EqualTo(1f));

        tank.ApplySpeedBoost(100);
        Assert.That(tank.SpeedMultiplier, Is.EqualTo(2f));
    }

    [Test]
    public void TankEntity_SpeedBoost_ExpiresAfterDuration()
    {
        var tank = new TankEntity(1, new Vector2(100, 100));
        tank.ApplySpeedBoost(50);
        Assert.That(tank.SpeedMultiplier, Is.EqualTo(2f));

        tank.TickSpeedBoost(50); // at expiry tick
        Assert.That(tank.SpeedMultiplier, Is.EqualTo(1f));
    }

    [Test]
    public void TankEntity_SpeedBoost_StillActiveBeforeExpiry()
    {
        var tank = new TankEntity(1, new Vector2(100, 100));
        tank.ApplySpeedBoost(50);

        tank.TickSpeedBoost(49); // before expiry
        Assert.That(tank.SpeedMultiplier, Is.EqualTo(2f));
    }
}

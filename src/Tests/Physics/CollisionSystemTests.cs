using System.Numerics;
using NUnit.Framework;
using BattleTank.GameLogic.Entities;
using BattleTank.GameLogic.Physics;
using BattleTank.GameLogic.Shared;

namespace BattleTank.Tests.Physics;

[TestFixture]
public class CollisionSystemTests
{
    [Test]
    public void BulletHitsTank_WhenClose_ReturnsTrue()
    {
        var tank = new TankEntity(1, new Vector2(500, 500));
        var bullet = new BulletEntity(1, 2, new Vector2(500, 500), Vector2.UnitX);
        Assert.That(CollisionSystem.BulletHitsTank(bullet, tank), Is.True);
    }

    [Test]
    public void BulletHitsTank_WhenFar_ReturnsFalse()
    {
        var tank = new TankEntity(1, new Vector2(500, 500));
        var bullet = new BulletEntity(1, 2, new Vector2(600, 600), Vector2.UnitX);
        Assert.That(CollisionSystem.BulletHitsTank(bullet, tank), Is.False);
    }

    [Test]
    public void BulletHitsTank_SameOwner_ReturnsFalse()
    {
        var tank = new TankEntity(1, new Vector2(500, 500));
        var bullet = new BulletEntity(1, 1, new Vector2(500, 500), Vector2.UnitX);
        Assert.That(CollisionSystem.BulletHitsTank(bullet, tank), Is.False);
    }

    [Test]
    public void BulletHitsTank_DeadBullet_ReturnsFalse()
    {
        var tank = new TankEntity(1, new Vector2(500, 500));
        var bullet = new BulletEntity(1, 2, new Vector2(500, 500), Vector2.UnitX);
        bullet.Kill();
        Assert.That(CollisionSystem.BulletHitsTank(bullet, tank), Is.False);
    }

    [Test]
    public void IsOutOfBounds_InsideMap_ReturnsFalse()
    {
        var bullet = new BulletEntity(1, 1, new Vector2(500, 500), Vector2.UnitX);
        Assert.That(CollisionSystem.IsOutOfBounds(bullet), Is.False);
    }

    [Test]
    public void IsOutOfBounds_OutsideMap_ReturnsTrue()
    {
        var bullet = new BulletEntity(1, 1, new Vector2(-1, 500), Vector2.UnitX);
        Assert.That(CollisionSystem.IsOutOfBounds(bullet), Is.True);
    }
}

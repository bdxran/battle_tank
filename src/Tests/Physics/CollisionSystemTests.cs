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

    [TestCase(-1f, 500f)]
    [TestCase(1001f, 500f)]
    [TestCase(500f, -1f)]
    [TestCase(500f, 1001f)]
    public void IsOutOfBounds_VariousOutOfBoundsPositions_ReturnsTrue(float x, float y)
    {
        var bullet = new BulletEntity(1, 1, new Vector2(x, y), Vector2.UnitX);
        Assert.That(CollisionSystem.IsOutOfBounds(bullet), Is.True);
    }

    [TestCase(1f, 500f)]
    [TestCase(999f, 500f)]
    [TestCase(500f, 1f)]
    [TestCase(500f, 999f)]
    public void IsOutOfBounds_InsideBoundaries_ReturnsFalse(float x, float y)
    {
        var bullet = new BulletEntity(1, 1, new Vector2(x, y), Vector2.UnitX);
        Assert.That(CollisionSystem.IsOutOfBounds(bullet), Is.False);
    }

    [TestCase(500f, 500f, true)]   // exact overlap
    [TestCase(520f, 500f, true)]   // within combined radius (TankRadius=20 + BulletRadius=5 = 25)
    [TestCase(530f, 500f, false)]  // just outside combined radius
    [TestCase(600f, 600f, false)]  // far away
    public void BulletHitsTank_VariousDistances(float bx, float by, bool expected)
    {
        var tank = new TankEntity(1, new Vector2(500f, 500f));
        var bullet = new BulletEntity(1, 2, new Vector2(bx, by), Vector2.UnitX);
        Assert.That(CollisionSystem.BulletHitsTank(bullet, tank), Is.EqualTo(expected));
    }
}

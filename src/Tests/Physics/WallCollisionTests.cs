using System.Numerics;
using NUnit.Framework;
using BattleTank.GameLogic.Entities;
using BattleTank.GameLogic.Physics;
using BattleTank.GameLogic.Shared;

namespace BattleTank.Tests.Physics;

[TestFixture]
public class WallCollisionTests
{
    private static readonly WallData TestWall = new(400f, 400f, 100f, 100f);

    [Test]
    public void BulletHitsWall_WhenInside_ReturnsTrue()
    {
        var bullet = new BulletEntity(1, 1, new Vector2(450f, 450f), Vector2.UnitX);
        Assert.That(CollisionSystem.BulletHitsWall(bullet, TestWall), Is.True);
    }

    [Test]
    public void BulletHitsWall_WhenOutside_ReturnsFalse()
    {
        var bullet = new BulletEntity(1, 1, new Vector2(100f, 100f), Vector2.UnitX);
        Assert.That(CollisionSystem.BulletHitsWall(bullet, TestWall), Is.False);
    }

    [Test]
    public void BulletHitsWall_DeadBullet_ReturnsFalse()
    {
        var bullet = new BulletEntity(1, 1, new Vector2(450f, 450f), Vector2.UnitX);
        bullet.Kill();
        Assert.That(CollisionSystem.BulletHitsWall(bullet, TestWall), Is.False);
    }

    [Test]
    public void ResolveTankWallCollision_TankInside_PushedOut()
    {
        // Tank centered on wall's left edge — should be pushed left
        var tank = new TankEntity(1, new Vector2(405f, 450f));
        bool hit = CollisionSystem.ResolveTankWallCollision(tank, TestWall);
        Assert.That(hit, Is.True);
        Assert.That(tank.Position.X, Is.LessThan(405f));
    }

    [Test]
    public void ResolveTankWallCollision_TankFarAway_NoChange()
    {
        var tank = new TankEntity(1, new Vector2(100f, 100f));
        bool hit = CollisionSystem.ResolveTankWallCollision(tank, TestWall);
        Assert.That(hit, Is.False);
        Assert.That(tank.Position, Is.EqualTo(new Vector2(100f, 100f)));
    }

    [Test]
    public void ClampTankToMap_TankOutsideBounds_Clamped()
    {
        var tank = new TankEntity(1, new Vector2(-50f, 1100f));
        CollisionSystem.ClampTankToMap(tank);
        Assert.That(tank.Position.X, Is.GreaterThanOrEqualTo(Constants.TankRadius));
        Assert.That(tank.Position.Y, Is.LessThanOrEqualTo(Constants.MapHeight - Constants.TankRadius));
    }
}

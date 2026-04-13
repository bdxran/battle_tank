using System.Numerics;
using NUnit.Framework;
using BattleTank.GameLogic.Entities;
using BattleTank.GameLogic.Shared;

namespace BattleTank.Tests.Entities;

[TestFixture]
public class BulletEntityTests
{
    [Test]
    public void Tick_MovesInDirection()
    {
        var bullet = new BulletEntity(1, 10, new Vector2(500, 500), Vector2.UnitX);
        bullet.Tick(1f);
        Assert.That(bullet.Position.X, Is.GreaterThan(500f));
        Assert.That(bullet.Position.Y, Is.EqualTo(500f).Within(0.001f));
    }

    [Test]
    public void Tick_KillsWhenMaxRangeReached()
    {
        var bullet = new BulletEntity(1, 10, new Vector2(0, 0), Vector2.UnitX);
        float timeToTravel = Constants.BulletMaxRange / Constants.BulletSpeed;
        bullet.Tick(timeToTravel);
        Assert.That(bullet.IsAlive, Is.False);
    }

    [Test]
    public void Kill_SetsIsAliveToFalse()
    {
        var bullet = new BulletEntity(1, 10, new Vector2(0, 0), Vector2.UnitX);
        bullet.Kill();
        Assert.That(bullet.IsAlive, Is.False);
    }

    [Test]
    public void GetSnapshot_ReturnsCorrectData()
    {
        var bullet = new BulletEntity(5, 42, new Vector2(100, 200), Vector2.UnitY);
        var snap = bullet.GetSnapshot();
        Assert.That(snap.Id, Is.EqualTo(5));
        Assert.That(snap.OwnerId, Is.EqualTo(42));
        Assert.That(snap.X, Is.EqualTo(100f).Within(0.001f));
        Assert.That(snap.Y, Is.EqualTo(200f).Within(0.001f));
    }
}

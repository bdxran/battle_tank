using System.Numerics;
using BattleTank.GameLogic.Entities;
using BattleTank.GameLogic.Network;
using BattleTank.GameLogic.Shared;
using FluentAssertions;
using NUnit.Framework;

namespace BattleTank.Tests.Entities;

[TestFixture]
public class TankEntityTests
{
    [Test]
    public void NewTank_HasFullHealth()
    {
        var tank = new TankEntity(1, Vector2.Zero);

        tank.Health.Should().Be(Constants.TankMaxHealth);
        tank.IsAlive.Should().BeTrue();
    }

    [Test]
    public void TakeDamage_ReducesHealth()
    {
        var tank = new TankEntity(1, Vector2.Zero);

        tank.TakeDamage(Constants.BulletDamage);

        tank.Health.Should().Be(Constants.TankMaxHealth - Constants.BulletDamage);
    }

    [Test]
    public void TakeDamage_WhenLethal_KillsTank()
    {
        var tank = new TankEntity(1, Vector2.Zero);

        tank.TakeDamage(Constants.TankMaxHealth);

        tank.IsAlive.Should().BeFalse();
        tank.Health.Should().Be(0);
    }

    [Test]
    public void TakeDamage_CannotGoBelowZero()
    {
        var tank = new TankEntity(1, Vector2.Zero);

        tank.TakeDamage(Constants.TankMaxHealth * 2);

        tank.Health.Should().Be(0);
    }

    [Test]
    public void TakeDamage_WhenAlreadyDead_IsIgnored()
    {
        var tank = new TankEntity(1, Vector2.Zero);
        tank.TakeDamage(Constants.TankMaxHealth);

        tank.TakeDamage(Constants.BulletDamage * 2);

        tank.Health.Should().Be(0);
    }

    [Test]
    public void ApplyInput_MoveForward_MovesInFacingDirection()
    {
        var tank = new TankEntity(1, Vector2.Zero);
        // Rotation 0 = facing up → negative Y

        tank.ApplyInput(InputFlags.MoveForward, 1f);

        tank.Position.Y.Should().BeLessThan(0f);
        tank.Position.X.Should().BeApproximately(0f, 0.001f);
    }

    [Test]
    public void ApplyInput_MoveBackward_MovesOppositeToFacingDirection()
    {
        var tank = new TankEntity(1, Vector2.Zero);

        tank.ApplyInput(InputFlags.MoveBackward, 1f);

        tank.Position.Y.Should().BeGreaterThan(0f);
    }

    [Test]
    public void ApplyInput_RotateRight_IncreasesRotation()
    {
        var tank = new TankEntity(1, Vector2.Zero);

        tank.ApplyInput(InputFlags.RotateRight, 1f);

        tank.Rotation.Should().BeGreaterThan(0f);
    }

    [Test]
    public void ApplyInput_RotateLeft_DecreasesRotation_WrapsAround()
    {
        var tank = new TankEntity(1, Vector2.Zero);

        tank.ApplyInput(InputFlags.RotateLeft, 1f);

        // Should wrap around to 270° instead of negative
        tank.Rotation.Should().BeApproximately(270f, 0.001f);
    }

    [Test]
    public void ApplyInput_WhenDead_DoesNotMove()
    {
        var startPosition = new Vector2(100f, 100f);
        var tank = new TankEntity(1, startPosition);
        tank.TakeDamage(Constants.TankMaxHealth);

        tank.ApplyInput(InputFlags.MoveForward, 1f);

        tank.Position.Should().Be(startPosition);
    }

    [Test]
    public void GetSnapshot_ReflectsCurrentState()
    {
        var tank = new TankEntity(42, new Vector2(50f, 75f));

        var snapshot = tank.GetSnapshot();

        snapshot.Id.Should().Be(42);
        snapshot.X.Should().Be(50f);
        snapshot.Y.Should().Be(75f);
        snapshot.Rotation.Should().Be(0f);
        snapshot.Health.Should().Be(Constants.TankMaxHealth);
    }

    [Test]
    public void GetSnapshot_AfterDamage_ReflectsReducedHealth()
    {
        var tank = new TankEntity(1, Vector2.Zero);
        tank.TakeDamage(Constants.BulletDamage);

        var snapshot = tank.GetSnapshot();

        snapshot.Health.Should().Be(Constants.TankMaxHealth - Constants.BulletDamage);
    }
}

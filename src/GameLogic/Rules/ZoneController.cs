using System;
using System.Collections.Generic;
using System.Numerics;
using BattleTank.GameLogic.Entities;
using BattleTank.GameLogic.Shared;

namespace BattleTank.GameLogic.Rules;

public class ZoneController
{
    private readonly float _centerX;
    private readonly float _centerY;
    private float _currentRadius;
    private float _timeSinceLastShrink;

    public ZoneController()
    {
        _centerX = Constants.MapWidth / 2f;
        _centerY = Constants.MapHeight / 2f;
        _currentRadius = Constants.ZoneInitialRadius;
    }

    public void Tick(float deltaTime, IEnumerable<TankEntity> tanks)
    {
        _timeSinceLastShrink += deltaTime;

        if (_timeSinceLastShrink >= Constants.ZoneShrinkInterval && _currentRadius > Constants.ZoneMinRadius)
        {
            _currentRadius = MathF.Max(Constants.ZoneMinRadius, _currentRadius - Constants.ZoneShrinkAmount);
            _timeSinceLastShrink = 0f;
        }

        int damage = (int)(Constants.ZoneDamagePerSecond * deltaTime);
        if (damage <= 0) damage = 1;

        foreach (var tank in tanks)
        {
            if (!tank.IsAlive) continue;
            if (!IsInsideZone(tank.Position))
                tank.TakeDamage(damage);
        }
    }

    public void Reset()
    {
        _currentRadius = Constants.ZoneInitialRadius;
        _timeSinceLastShrink = 0f;
    }

    public bool IsInsideZone(Vector2 position)
    {
        float dx = position.X - _centerX;
        float dy = position.Y - _centerY;
        return dx * dx + dy * dy <= _currentRadius * _currentRadius;
    }

    public ZoneSnapshot GetSnapshot() =>
        new(_centerX, _centerY, _currentRadius, Constants.ZoneDamagePerSecond);
}

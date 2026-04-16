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
    private readonly float _activationDelay;
    private float _currentRadius;
    private float _timeSinceLastShrink;
    private float _timeBeforeActivation;
    private bool _isActive;

    public ZoneController(float activationDelay = Constants.ZoneActivationDelay)
    {
        _centerX = Constants.MapWidth / 2f;
        _centerY = Constants.MapHeight / 2f;
        _currentRadius = Constants.ZoneInitialRadius;
        _activationDelay = activationDelay;
        _timeBeforeActivation = activationDelay;
        _isActive = activationDelay <= 0f;
    }

    public void Tick(float deltaTime, IEnumerable<TankEntity> tanks)
    {
        if (!_isActive)
        {
            _timeBeforeActivation -= deltaTime;
            if (_timeBeforeActivation <= 0f)
                _isActive = true;
            return;
        }

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
        _timeBeforeActivation = _activationDelay;
        _isActive = _activationDelay <= 0f;
    }

    public bool IsInsideZone(Vector2 position)
    {
        float dx = position.X - _centerX;
        float dy = position.Y - _centerY;
        return dx * dx + dy * dy <= _currentRadius * _currentRadius;
    }

    public ZoneSnapshot GetSnapshot() =>
        new(_centerX, _centerY, _isActive ? _currentRadius : 0f, Constants.ZoneDamagePerSecond);
}

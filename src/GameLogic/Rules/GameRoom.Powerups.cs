using Microsoft.Extensions.Logging;
using BattleTank.GameLogic.Entities;
using BattleTank.GameLogic.Shared;

namespace BattleTank.GameLogic.Rules;

public partial class GameRoom
{
    private void TickPowerups()
    {
        if (_currentTick - _lastPowerupSpawnTick >= Constants.PowerupSpawnIntervalTicks)
        {
            _lastPowerupSpawnTick = _currentTick;
            var spawnPos = PowerupSpawnPoints[_nextPowerupId % PowerupSpawnPoints.Length];
            var type = (PowerupType)_random.Next(3);
            _powerups.Add(new PowerupEntity(_nextPowerupId++, spawnPos, type));
        }

        float pickupDist = Constants.PowerupRadius + Constants.TankRadius;
        for (int i = 0; i < _powerups.Count; i++)
        {
            var powerup = _powerups[i];
            if (powerup.IsPickedUp) continue;

            foreach (var tank in _tanks.Values)
            {
                if (!tank.IsAlive) continue;

                float dx = tank.Position.X - powerup.Position.X;
                float dy = tank.Position.Y - powerup.Position.Y;

                if (dx * dx + dy * dy < pickupDist * pickupDist)
                {
                    powerup.PickUp();
                    ApplyPowerup(tank, powerup.Type);
                    _logger.LogDebug("Player {Id} picked up {Type}", tank.Id, powerup.Type);
                    break;
                }
            }
        }

        for (int i = _powerups.Count - 1; i >= 0; i--)
        {
            if (_powerups[i].IsPickedUp)
                _powerups.RemoveAt(i);
        }
    }

    private void ApplyPowerup(TankEntity tank, PowerupType type)
    {
        switch (type)
        {
            case PowerupType.ExtraAmmo:
                _playerSessions[tank.Id].LastFireTick = _currentTick >= _rules.FireCooldownTicks
                    ? _currentTick - _rules.FireCooldownTicks + 1
                    : 0;
                break;
            case PowerupType.Shield:
                tank.Heal(Constants.ShieldHealAmount);
                break;
            case PowerupType.SpeedBoost:
                tank.ApplySpeedBoost(_currentTick + Constants.SpeedBoostDurationTicks);
                break;
        }
    }
}

using System;
using System.Numerics;
using Microsoft.Extensions.Logging;
using BattleTank.GameLogic.Entities;
using BattleTank.GameLogic.Physics;
using BattleTank.GameLogic.Shared;

namespace BattleTank.GameLogic.Rules;

public partial class GameRoom
{
    private void TryFire(PlayerSession session, TankEntity tank)
    {
        if (_currentTick - session.LastFireTick < _rules.FireCooldownTicks)
            return;

        session.LastFireTick = _currentTick;

        float radians = tank.Rotation * MathF.PI / 180f;
        var direction = new Vector2(MathF.Sin(radians), -MathF.Cos(radians));
        var spawnPos = tank.Position + direction * (Constants.TankRadius + Constants.BulletRadius + 1f);

        if (_bullets.Count < Constants.MaxBulletsInFlight)
            _bullets.Add(new BulletEntity(_nextBulletId++, tank.Id, spawnPos, direction));
    }

    private void TickBullets(float deltaTime)
    {
        for (int i = 0; i < _bullets.Count; i++)
        {
            var bullet = _bullets[i];
            if (!bullet.IsAlive) continue;

            bullet.Tick(deltaTime);

            if (CollisionSystem.IsOutOfBounds(bullet))
            {
                bullet.Kill();
                continue;
            }

            bool hitWall = false;
            foreach (var wall in MapLayout.Walls)
            {
                if (CollisionSystem.BulletHitsWall(bullet, wall))
                {
                    bullet.Kill();
                    hitWall = true;
                    break;
                }
            }
            if (hitWall) continue;

            foreach (var tank in _tanks.Values)
            {
                if (!CollisionSystem.BulletHitsTank(bullet, tank))
                    continue;

                // Friendly fire check
                if (!_rules.IsFriendlyFireEnabled && tank.TeamId >= 0)
                {
                    bool sameTeam = _tanks.TryGetValue(bullet.OwnerId, out var shooter)
                        && shooter.TeamId == tank.TeamId;
                    if (sameTeam) continue;
                }

                bool wasAlive = tank.IsAlive;
                tank.TakeDamage(Constants.BulletDamage);
                bullet.Kill();
                _logger.LogDebug("Bullet {BulletId} hit tank {TankId}", bullet.Id, tank.Id);

                if (wasAlive && !tank.IsAlive)
                {
                    _pendingEliminations.Add(new Elimination(tank.Id, bullet.OwnerId));
                    _rules.OnElimination(tank.Id, bullet.OwnerId, _currentTick, _state);
                }

                break;
            }
        }

        for (int i = _bullets.Count - 1; i >= 0; i--)
        {
            if (!_bullets[i].IsAlive)
                _bullets.RemoveAt(i);
        }
    }
}

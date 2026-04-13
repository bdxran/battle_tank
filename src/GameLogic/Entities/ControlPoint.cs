using System;
using System.Collections.Generic;
using System.Numerics;
using BattleTank.GameLogic.Shared;

namespace BattleTank.GameLogic.Entities;

public class ControlPoint
{
    private int? _capturingTeamId;
    private float _captureProgress; // 0..1 towards _capturingTeamId

    public int Id { get; }
    public Vector2 Position { get; }
    public float Radius { get; }
    public int? ControllingTeamId { get; private set; }
    public float CaptureProgress => _captureProgress;

    public ControlPoint(int id, Vector2 position, float radius)
    {
        Id = id;
        Position = position;
        Radius = radius;
    }

    /// <summary>
    /// Ticks capture logic. Returns the teamId that scored this tick (if any controlled), or null.
    /// </summary>
    public int? Tick(IReadOnlyDictionary<int, TankEntity> tanks, float deltaTime)
    {
        int? teamInZone = GetDominantTeam(tanks);

        if (teamInZone == null)
            return ControllingTeamId; // no one capturing, controlling team still scores

        if (ControllingTeamId == teamInZone)
        {
            // Already controlled by this team — full score, no progress change
            _captureProgress = 1f;
            return ControllingTeamId;
        }

        if (_capturingTeamId != teamInZone)
        {
            // Different team entering — start over from current progress against new team
            _capturingTeamId = teamInZone;
            _captureProgress = 0f;
            ControllingTeamId = null;
        }

        float captureRate = Constants.CaptureRatePerSecond / 100f; // 100% = full capture
        _captureProgress = Math.Min(1f, _captureProgress + captureRate * deltaTime);

        if (_captureProgress >= 1f)
            ControllingTeamId = _capturingTeamId;

        return ControllingTeamId;
    }

    public ControlPointSnapshot GetSnapshot() =>
        new(Id, Position.X, Position.Y, Radius, ControllingTeamId, _captureProgress);

    private int? GetDominantTeam(IReadOnlyDictionary<int, TankEntity> tanks)
    {
        int? presentTeam = null;
        bool contested = false;

        foreach (var tank in tanks.Values)
        {
            if (!tank.IsAlive || tank.TeamId < 0) continue;

            float dx = tank.Position.X - Position.X;
            float dy = tank.Position.Y - Position.Y;
            float distSq = dx * dx + dy * dy;

            if (distSq > Radius * Radius) continue;

            if (presentTeam == null)
            {
                presentTeam = tank.TeamId;
            }
            else if (presentTeam != tank.TeamId)
            {
                contested = true;
                break;
            }
        }

        return contested ? null : presentTeam;
    }
}

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
        float captureRate = Constants.CaptureRatePerSecond / 100f;

        if (teamInZone == null)
            return ControllingTeamId; // contested or empty — state unchanged

        if (teamInZone == _capturingTeamId || _capturingTeamId == null)
        {
            // Same team continues (or zone was neutral)
            if (_capturingTeamId == null)
                _capturingTeamId = teamInZone;

            if (ControllingTeamId == teamInZone)
            {
                _captureProgress = 1f;
                return ControllingTeamId;
            }

            _captureProgress = Math.Min(1f, _captureProgress + captureRate * deltaTime);
            if (_captureProgress >= 1f)
                ControllingTeamId = _capturingTeamId;
        }
        else
        {
            // Opposing team — push back progress instead of resetting
            _captureProgress = Math.Max(0f, _captureProgress - captureRate * deltaTime);
            if (_captureProgress <= 0f)
            {
                ControllingTeamId = null;
                _capturingTeamId = teamInZone; // new team takes over from 0
            }
        }

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

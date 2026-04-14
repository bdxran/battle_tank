using Godot;
using BattleTank.GameLogic.Shared;

namespace BattleTank.Godot.Nodes;

/// <summary>
/// Draws capture zone circles on the main map.
/// Colors reflect which team controls each zone.
/// </summary>
public partial class ControlPointsNode : Node2D
{
    // Neutral zone
    private static readonly Color NeutralFill = new(0.9f, 0.9f, 0.2f, 0.08f);
    private static readonly Color NeutralBorder = new(0.9f, 0.9f, 0.2f, 0.7f);

    // Team 0 — blue
    private static readonly Color Team0Fill = new(0.2f, 0.4f, 1f, 0.12f);
    private static readonly Color Team0Border = new(0.2f, 0.4f, 1f, 0.9f);

    // Team 1 — red
    private static readonly Color Team1Fill = new(1f, 0.2f, 0.2f, 0.12f);
    private static readonly Color Team1Border = new(1f, 0.2f, 0.2f, 0.9f);

    private static readonly Color ProgressBarBg = new(0f, 0f, 0f, 0.4f);

    private ControlPointSnapshot[] _points = [];

    public void UpdateFrom(ControlPointSnapshot[] points)
    {
        _points = points;
        QueueRedraw();
    }

    public override void _Draw()
    {
        foreach (var cp in _points)
        {
            var center = new Vector2(cp.X, cp.Y);
            var (fill, border) = TeamColors(cp.ControllingTeamId);

            DrawCircle(center, cp.Radius, fill);
            DrawArc(center, cp.Radius, 0f, Mathf.Tau, 64, border, 2f);

            // Capture progress arc (only when not fully controlled)
            if (cp.CaptureProgress > 0f && cp.CaptureProgress < 1f)
            {
                float angle = cp.CaptureProgress * Mathf.Tau;
                DrawArc(center, cp.Radius - 4f, -Mathf.Pi / 2f, -Mathf.Pi / 2f + angle, 48, border, 4f);
            }
        }
    }

    private static (Color fill, Color border) TeamColors(int? teamId) => teamId switch
    {
        0 => (Team0Fill, Team0Border),
        1 => (Team1Fill, Team1Border),
        _ => (NeutralFill, NeutralBorder),
    };
}

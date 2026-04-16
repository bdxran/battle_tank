using System.Collections.Generic;
using Godot;
using BattleTank.GameLogic.Shared;

namespace BattleTank.Godot.UI;

public partial class HudNode : CanvasLayer
{
    private Label _healthLabel = null!;
    private Label _aliveLabel = null!;
    private Label _timerLabel = null!;
    private Label _scoreLabel = null!;
    private MinimapNode _minimap = null!;

    public void Initialize(int localPlayerId)
    {
        _minimap.Initialize(localPlayerId);
    }

    public override void _Ready()
    {
        var container = new VBoxContainer();
        container.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        container.Position = new Vector2(16, 16);
        AddChild(container);

        _healthLabel = new Label { Text = "HP: 100" };
        _aliveLabel = new Label { Text = "Alive: -" };
        _timerLabel = new Label { Text = "" };
        _scoreLabel = new Label { Text = "" };

        container.AddChild(_healthLabel);
        container.AddChild(_aliveLabel);
        container.AddChild(_timerLabel);
        container.AddChild(_scoreLabel);

        _minimap = new MinimapNode();
        AddChild(_minimap);
    }

    public void UpdateHealth(int health)
    {
        _healthLabel.Text = $"HP: {health}";
    }

    public void UpdateAliveCount(int count)
    {
        _aliveLabel.Text = $"Alive: {count}";
    }

    public void UpdateTimer(int ticksRemaining)
    {
        if (ticksRemaining <= 0) { _timerLabel.Text = ""; return; }
        int secs = ticksRemaining / GameLogic.Shared.Constants.TickRate;
        _timerLabel.Text = $"{secs / 60:D2}:{secs % 60:D2}";
    }

    public void UpdateScore(int[] teamScores, int localPlayerKills, int localPlayerDeaths, GameLogic.Shared.GameMode mode)
    {
        _scoreLabel.Text = mode switch
        {
            GameLogic.Shared.GameMode.Deathmatch => $"Kills: {localPlayerKills}",
            GameLogic.Shared.GameMode.CaptureZone when teamScores is { Length: >= 2 }
                => $"Bleu {teamScores[0]}  -  Rouge {teamScores[1]}\nK/D: {localPlayerKills}/{localPlayerDeaths}",
            _ => ""
        };
    }

    public void SetTeamInfo(int localTeamId, Dictionary<int, int> playerTeamMap)
    {
        _minimap.SetTeamInfo(localTeamId, playerTeamMap);
    }

    public void UpdateMinimap(TankSnapshot[] tanks, ZoneSnapshot zone, ControlPointSnapshot[] controlPoints)
    {
        _minimap.UpdateFrom(tanks, zone, controlPoints);
    }
}

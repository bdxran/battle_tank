using Godot;
using BattleTank.GameLogic.Shared;

namespace BattleTank.Godot.UI;

public partial class ScoreboardOverlay : CanvasLayer
{
    private GridContainer _grid = null!;

    public override void _Ready()
    {
        Visible = false;

        var bg = new ColorRect
        {
            Color = new Color(0, 0, 0, 0.7f),
        };
        bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(bg);

        var center = new CenterContainer();
        center.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(center);

        var panel = new PanelContainer();
        center.AddChild(panel);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 8);
        panel.AddChild(vbox);

        var title = new Label
        {
            Text = "SCORES",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        vbox.AddChild(title);

        _grid = new GridContainer { Columns = 5 };
        vbox.AddChild(_grid);
    }

    public void UpdateFrom(PlayerInfo[] players, int[] teamScores, GameMode mode)
    {
        bool showZones = mode == GameMode.CaptureZone;
        _grid.Columns = showZones ? 6 : 5;

        foreach (Node child in _grid.GetChildren())
            child.QueueFree();

        AddHeader(showZones);

        bool grouped = (mode == GameMode.CaptureZone || mode == GameMode.Teams)
                       && teamScores is { Length: >= 2 };

        if (grouped)
        {
            for (int team = 0; team <= 1; team++)
            {
                string teamScore = teamScores != null && teamScores.Length > team
                    ? teamScores[team].ToString()
                    : "0";
                string teamName = team == 0 ? "Equipe Bleue" : "Equipe Rouge";
                AddSeparatorRow($"{teamName}  [{teamScore} pts]", showZones);

                foreach (var p in players)
                {
                    if (p.TeamId == team)
                        AddPlayerRow(p, showZones);
                }
            }
        }
        else
        {
            foreach (var p in players)
                AddPlayerRow(p, showZones);
        }
    }

    private void AddHeader(bool showZones)
    {
        AddCell("Joueur");
        AddCell("Kills");
        AddCell("Assists");
        AddCell("Morts");
        AddCell("Ratio");
        if (showZones) AddCell("Zones");
    }

    private void AddSeparatorRow(string label, bool showZones)
    {
        var lbl = new Label
        {
            Text = label,
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        _grid.AddChild(lbl);
        int extraCols = showZones ? 4 : 3;
        for (int i = 0; i < extraCols; i++)
            _grid.AddChild(new Label { Text = "" });
    }

    private void AddPlayerRow(PlayerInfo p, bool showZones)
    {
        float ratio = p.Deaths == 0 ? p.Kills : (float)p.Kills / p.Deaths;
        AddCell(p.Nickname);
        AddCell(p.Kills.ToString());
        AddCell(p.Assists.ToString());
        AddCell(p.Deaths.ToString());
        AddCell(ratio.ToString("F1"));
        if (showZones) AddCell(p.ZoneCaptures.ToString());
    }

    private void AddCell(string text)
    {
        _grid.AddChild(new Label
        {
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Center,
        });
    }
}

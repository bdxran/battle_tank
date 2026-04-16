using System;
using Godot;
using BattleTank.GameLogic.Shared;

namespace BattleTank.Godot.UI;

public partial class GameOverScreen : CanvasLayer
{
    private Label _titleLabel = null!;
    private Label _subtitleLabel = null!;
    private GridContainer _scoreGrid = null!;
    private Button _restartButton = null!;
    private Button _menuButton = null!;

    public event Action? RestartRequested;
    public event Action? MenuRequested;

    public override void _Ready()
    {
        Visible = false;

        var center = new CenterContainer();
        center.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(center);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 16);
        center.AddChild(vbox);

        _titleLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Text = "GAME OVER"
        };

        _subtitleLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Text = ""
        };

        _scoreGrid = new GridContainer { Columns = 4, Visible = false };

        var btnContainer = new HBoxContainer();
        btnContainer.AddThemeConstantOverride("separation", 12);
        btnContainer.Alignment = BoxContainer.AlignmentMode.Center;

        _restartButton = new Button { Text = "Rejouer" };
        _restartButton.Pressed += () => RestartRequested?.Invoke();

        _menuButton = new Button { Text = "Menu principal" };
        _menuButton.Pressed += () => MenuRequested?.Invoke();

        btnContainer.AddChild(_restartButton);
        btnContainer.AddChild(_menuButton);

        vbox.AddChild(_titleLabel);
        vbox.AddChild(_subtitleLabel);
        vbox.AddChild(_scoreGrid);
        vbox.AddChild(btnContainer);
    }

    public void ShowResult(int localPlayerId, int winnerId, int winnerTeamId,
        PlayerInfo[] leaderboard, int[] teamScores, GameMode mode)
    {
        _titleLabel.Text = BuildTitle(localPlayerId, winnerId, winnerTeamId, mode);
        _subtitleLabel.Text = "";

        BuildScoreGrid(leaderboard, teamScores, mode);
        _scoreGrid.Visible = leaderboard.Length > 0;

        Visible = true;
    }

    public void ShowWin(int localPlayerId, int winnerId)
    {
        _titleLabel.Text = winnerId == localPlayerId ? "VICTOIRE !" : "DÉFAITE";
        _subtitleLabel.Text = winnerId == -1
            ? "Aucun survivant."
            : winnerId == localPlayerId
                ? "Vous êtes le dernier tank en vie."
                : $"Joueur {winnerId} remporte la partie.";
        _scoreGrid.Visible = false;
        Visible = true;
    }

    public void ShowEliminated(int killerId)
    {
        _titleLabel.Text = "ÉLIMINÉ";
        _subtitleLabel.Text = killerId == -1
            ? "Éliminé par la zone."
            : $"Éliminé par le Joueur {killerId}.";
        _scoreGrid.Visible = false;
        Visible = true;
    }

    private static string BuildTitle(int localPlayerId, int winnerId, int winnerTeamId, GameMode mode)
    {
        if (mode is GameMode.CaptureZone or GameMode.Teams)
        {
            if (winnerTeamId < 0) return "MATCH NUL";
            string teamName = winnerTeamId == 0 ? "Equipe Bleue" : "Equipe Rouge";
            return $"{teamName} remporte la partie !";
        }
        return winnerId == localPlayerId ? "VICTOIRE !" : "DÉFAITE";
    }

    private void BuildScoreGrid(PlayerInfo[] leaderboard, int[] teamScores, GameMode mode)
    {
        foreach (Node child in _scoreGrid.GetChildren())
            child.QueueFree();

        AddGridCell("Joueur"); AddGridCell("Kills"); AddGridCell("Morts"); AddGridCell("Ratio");

        bool grouped = (mode == GameMode.CaptureZone || mode == GameMode.Teams)
                       && teamScores is { Length: >= 2 };

        if (grouped)
        {
            for (int team = 0; team <= 1; team++)
            {
                string score = teamScores != null && teamScores.Length > team
                    ? teamScores[team].ToString() : "0";
                string name = team == 0 ? "Equipe Bleue" : "Equipe Rouge";
                AddGridCell($"{name} [{score} pts]");
                for (int i = 0; i < 3; i++) AddGridCell("");
                foreach (var p in leaderboard)
                    if (p.TeamId == team) AddPlayerRow(p);
            }
        }
        else
        {
            foreach (var p in leaderboard)
                AddPlayerRow(p);
        }
    }

    private void AddPlayerRow(PlayerInfo p)
    {
        float ratio = p.Deaths == 0 ? p.Kills : (float)p.Kills / p.Deaths;
        AddGridCell(p.Nickname);
        AddGridCell(p.Kills.ToString());
        AddGridCell(p.Deaths.ToString());
        AddGridCell(ratio.ToString("F1"));
    }

    private void AddGridCell(string text)
    {
        _scoreGrid.AddChild(new Label
        {
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Center,
        });
    }
}

using System;
using Godot;
using BattleTank.GameLogic.Shared;

namespace BattleTank.Godot.UI;

/// <summary>
/// Configuration screen before starting an in-process server.
/// Lets the host set game name, port, optional room code, game mode, and mode-specific parameters.
/// </summary>
public partial class HostSetupScreen : CanvasLayer
{
    public event Action<string, int, string?, GameMode, int, int>? HostConfigured;
    public event Action? BackRequested;

    private LineEdit _nameField = null!;
    private LineEdit _portField = null!;
    private LineEdit _codeField = null!;
    private Label _statusLabel = null!;

    // Mode selection
    private GameMode _selectedMode = GameMode.BattleRoyale;
    private readonly Button[] _modeButtons = new Button[4];

    // Mode-specific params
    private VBoxContainer _paramsPanel = null!;
    private LineEdit _durationField = null!;
    private LineEdit _scoreField = null!;
    private Label _durationLabel = null!;
    private Label _scoreLabel = null!;

    public override void _Ready()
    {
        var root = new Control();
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);

        var center = new CenterContainer();
        center.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        root.AddChild(center);

        var panel = new PanelContainer();
        panel.CustomMinimumSize = new Vector2(360, 0);
        center.AddChild(panel);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 10);
        panel.AddChild(vbox);

        var title = new Label { Text = "Héberger une partie" };
        title.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(title);

        _nameField = new LineEdit { PlaceholderText = "Nom de la partie", Text = "Ma partie" };
        vbox.AddChild(_nameField);

        _portField = new LineEdit { PlaceholderText = "Port", Text = Constants.ServerPort.ToString() };
        vbox.AddChild(_portField);

        _codeField = new LineEdit { PlaceholderText = "Code (optionnel)" };
        vbox.AddChild(_codeField);

        // Mode selection
        var modeSep = new Label { Text = "Mode de jeu :" };
        vbox.AddChild(modeSep);

        var modeGrid = new GridContainer();
        modeGrid.Columns = 2;
        vbox.AddChild(modeGrid);

        (GameMode mode, string label)[] modes =
        [
            (GameMode.BattleRoyale, "Battle Royale"),
            (GameMode.Deathmatch,   "Deathmatch"),
            (GameMode.Teams,        "Équipes"),
            (GameMode.CaptureZone,  "Capture de zone"),
        ];

        for (int i = 0; i < modes.Length; i++)
        {
            var (m, lbl) = modes[i];
            var btn = new Button { Text = lbl, ToggleMode = true };
            btn.ButtonPressed = (m == _selectedMode);
            var captured = m;
            btn.Pressed += () => SelectMode(captured);
            _modeButtons[i] = btn;
            modeGrid.AddChild(btn);
        }

        // Mode-specific params panel
        _paramsPanel = new VBoxContainer();
        _paramsPanel.AddThemeConstantOverride("separation", 6);
        vbox.AddChild(_paramsPanel);

        _durationLabel = new Label { Text = "Durée (minutes) :" };
        _paramsPanel.AddChild(_durationLabel);
        _durationField = new LineEdit { Text = "3" };
        _paramsPanel.AddChild(_durationField);

        _scoreLabel = new Label { Text = "Score cible :" };
        _paramsPanel.AddChild(_scoreLabel);
        _scoreField = new LineEdit { Text = "1200" };
        _paramsPanel.AddChild(_scoreField);

        _statusLabel = new Label { Text = "" };
        _statusLabel.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(_statusLabel);

        var btn2 = new Button { Text = "Héberger" };
        btn2.Pressed += OnHostPressed;
        vbox.AddChild(btn2);

        var back = new Button { Text = "← Retour" };
        back.Pressed += () => BackRequested?.Invoke();
        vbox.AddChild(back);

        RefreshParamsPanel();
    }

    public void ShowError(string message)
    {
        _statusLabel.Text = message;
    }

    private void SelectMode(GameMode mode)
    {
        _selectedMode = mode;
        GameMode[] modes = [GameMode.BattleRoyale, GameMode.Deathmatch, GameMode.Teams, GameMode.CaptureZone];
        for (int i = 0; i < _modeButtons.Length; i++)
            _modeButtons[i].ButtonPressed = (modes[i] == mode);
        RefreshParamsPanel();
    }

    private void RefreshParamsPanel()
    {
        bool hasDuration = _selectedMode is GameMode.Deathmatch or GameMode.CaptureZone;
        bool hasScore = _selectedMode is GameMode.CaptureZone;

        _durationLabel.Visible = hasDuration;
        _durationField.Visible = hasDuration;
        _scoreLabel.Visible = hasScore;
        _scoreField.Visible = hasScore;

        if (_selectedMode == GameMode.Deathmatch)
            _durationField.Text = "3";
        else if (_selectedMode == GameMode.CaptureZone)
            _durationField.Text = "4";
    }

    private void OnHostPressed()
    {
        string name = _nameField.Text.Trim();
        if (string.IsNullOrEmpty(name)) name = "Ma partie";

        if (!int.TryParse(_portField.Text.Trim(), out int port) || port < 1 || port > 65535)
        {
            _statusLabel.Text = "Port invalide";
            return;
        }

        int durationSeconds = 180;
        if (_selectedMode is GameMode.Deathmatch or GameMode.CaptureZone)
        {
            if (!int.TryParse(_durationField.Text.Trim(), out int durationMin) || durationMin < 1)
            {
                _statusLabel.Text = "Durée invalide";
                return;
            }
            durationSeconds = durationMin * 60;
        }

        int scoreToWin = 1200;
        if (_selectedMode == GameMode.CaptureZone)
        {
            if (!int.TryParse(_scoreField.Text.Trim(), out scoreToWin) || scoreToWin < 1)
            {
                _statusLabel.Text = "Score cible invalide";
                return;
            }
        }

        string? code = string.IsNullOrWhiteSpace(_codeField.Text) ? null : _codeField.Text.Trim();
        HostConfigured?.Invoke(name, port, code, _selectedMode, durationSeconds, scoreToWin);
    }
}

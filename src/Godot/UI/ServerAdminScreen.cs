using System;
using Godot;
using BattleTank.GameLogic.Shared;

namespace BattleTank.Godot.UI;

/// <summary>
/// Écran admin serveur dédié : connexion avec mot de passe admin, configuration du mode de jeu, puis jouer.
/// </summary>
public partial class ServerAdminScreen : CanvasLayer
{
    public event Action<string, int, string>? AdminConnectRequested; // address, port, adminPassword
    public event Action<GameMode, int, int, bool, string?, int>? ConfigApplyRequested; // mode, duration, score, friendlyFire, roomCode, botFillCount
    public event Action? PlayRequested;
    public event Action? BackRequested;
    public event Action? DisconnectRequested;

    // Connexion
    private LineEdit _addressField = null!;
    private LineEdit _portField = null!;
    private LineEdit _passwordField = null!;
    private Button _connectBtn = null!;
    private Label _statusLabel = null!;

    // Panneau config (visible après auth)
    private VBoxContainer _configPanel = null!;
    private GameMode _selectedMode = GameMode.BattleRoyale;
    private readonly Button[] _modeButtons = new Button[4];
    private LineEdit _durationField = null!;
    private LineEdit _scoreField = null!;
    private LineEdit _codeField = null!;
    private LineEdit _botField = null!;
    private Label _durationLabel = null!;
    private Label _scoreLabel = null!;
    private Button _applyBtn = null!;
    private Button _playBtn = null!;
    private bool _configApplied;

    public override void _Ready()
    {
        var root = new Control();
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);

        var center = new CenterContainer();
        center.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        root.AddChild(center);

        var panel = new PanelContainer();
        panel.CustomMinimumSize = new Vector2(400, 0);
        center.AddChild(panel);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 10);
        panel.AddChild(vbox);

        var title = new Label { Text = "Configurer le serveur dédié" };
        title.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(title);

        // ── Connexion ────────────────────────────────────────
        var connGroup = new VBoxContainer();
        connGroup.AddThemeConstantOverride("separation", 6);
        vbox.AddChild(connGroup);

        connGroup.AddChild(new Label { Text = "Adresse du serveur" });
        _addressField = new LineEdit { PlaceholderText = "127.0.0.1", Text = "127.0.0.1" };
        connGroup.AddChild(_addressField);

        connGroup.AddChild(new Label { Text = "Port" });
        _portField = new LineEdit { PlaceholderText = "4242", Text = "4242" };
        connGroup.AddChild(_portField);

        connGroup.AddChild(new Label { Text = "Mot de passe admin" });
        _passwordField = new LineEdit { PlaceholderText = "••••••••", Secret = true };
        connGroup.AddChild(_passwordField);

        _statusLabel = new Label { Text = "" };
        _statusLabel.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(_statusLabel);

        _connectBtn = new Button { Text = "Se connecter" };
        _connectBtn.Pressed += OnConnectPressed;
        vbox.AddChild(_connectBtn);

        // ── Panneau config (masqué jusqu'à auth) ────────────
        _configPanel = new VBoxContainer();
        _configPanel.AddThemeConstantOverride("separation", 8);
        _configPanel.Visible = false;
        vbox.AddChild(_configPanel);

        var sep = new HSeparator();
        _configPanel.AddChild(sep);

        _configPanel.AddChild(new Label { Text = "Mode de jeu" });

        var modeRow = new HBoxContainer();
        _configPanel.AddChild(modeRow);

        string[] modeLabels = ["Battle Royale", "Deathmatch", "Équipes", "Capture"];
        GameMode[] modes = [GameMode.BattleRoyale, GameMode.Deathmatch, GameMode.Teams, GameMode.CaptureZone];
        for (int i = 0; i < modeLabels.Length; i++)
        {
            var idx = i;
            var btn = new Button { Text = modeLabels[i], ToggleMode = true };
            btn.Pressed += () => SelectMode(modes[idx]);
            _modeButtons[i] = btn;
            modeRow.AddChild(btn);
        }

        // Durée
        var durationRow = new HBoxContainer();
        _configPanel.AddChild(durationRow);
        _durationLabel = new Label { Text = "Durée (min)" };
        _durationLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        durationRow.AddChild(_durationLabel);
        _durationField = new LineEdit { Text = "3", CustomMinimumSize = new Vector2(60, 0) };
        durationRow.AddChild(_durationField);

        // Score cible
        var scoreRow = new HBoxContainer();
        _configPanel.AddChild(scoreRow);
        _scoreLabel = new Label { Text = "Score cible" };
        _scoreLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        scoreRow.AddChild(_scoreLabel);
        _scoreField = new LineEdit { Text = "1200", CustomMinimumSize = new Vector2(60, 0) };
        scoreRow.AddChild(_scoreField);

        // Nombre de bots
        var botRow = new HBoxContainer();
        _configPanel.AddChild(botRow);
        var botLabel = new Label { Text = "Bots (0 = aucun)" };
        botLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        botRow.AddChild(botLabel);
        _botField = new LineEdit { Text = "0", CustomMinimumSize = new Vector2(60, 0) };
        botRow.AddChild(_botField);

        // Code de room
        var codeRow = new HBoxContainer();
        _configPanel.AddChild(codeRow);
        codeRow.AddChild(new Label { Text = "Code (optionnel)" });
        _codeField = new LineEdit { PlaceholderText = "aucun" };
        _codeField.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        codeRow.AddChild(_codeField);

        _applyBtn = new Button { Text = "Appliquer la configuration" };
        _applyBtn.Pressed += OnApplyPressed;
        _configPanel.AddChild(_applyBtn);

        _playBtn = new Button { Text = "Jouer sur ce serveur" };
        _playBtn.Pressed += () => PlayRequested?.Invoke();
        _playBtn.Visible = false;
        _configPanel.AddChild(_playBtn);

        var disconnectBtn = new Button { Text = "Déconnecter" };
        disconnectBtn.Pressed += () => { DisconnectRequested?.Invoke(); ResetToConnect(); };
        _configPanel.AddChild(disconnectBtn);

        SelectMode(GameMode.BattleRoyale);

        // ── Retour ───────────────────────────────────────────
        var backBtn = new Button { Text = "Retour" };
        backBtn.Pressed += () => BackRequested?.Invoke();
        vbox.AddChild(backBtn);
    }

    private void OnConnectPressed()
    {
        var address = _addressField.Text.Trim();
        if (!int.TryParse(_portField.Text.Trim(), out int port) || port < 1 || port > 65535)
        {
            ShowStatus("Port invalide");
            return;
        }
        var password = _passwordField.Text;
        if (string.IsNullOrWhiteSpace(password))
        {
            ShowStatus("Mot de passe requis");
            return;
        }

        _connectBtn.Disabled = true;
        ShowStatus("Connexion en cours…");
        AdminConnectRequested?.Invoke(address, port, password);
    }

    private void OnApplyPressed()
    {
        if (!int.TryParse(_durationField.Text.Trim(), out int dur) || dur < 1) dur = 3;
        if (!int.TryParse(_scoreField.Text.Trim(), out int score) || score < 1) score = 1200;
        if (!int.TryParse(_botField.Text.Trim(), out int bots) || bots < 0) bots = 0;
        string? roomCode = string.IsNullOrWhiteSpace(_codeField.Text) ? null : _codeField.Text.Trim();

        _applyBtn.Disabled = true;
        ShowStatus("Application en cours…");
        ConfigApplyRequested?.Invoke(_selectedMode, dur * 60, score, false, roomCode, bots);
    }

    private void SelectMode(GameMode mode)
    {
        _selectedMode = mode;
        GameMode[] modes = [GameMode.BattleRoyale, GameMode.Deathmatch, GameMode.Teams, GameMode.CaptureZone];
        for (int i = 0; i < _modeButtons.Length; i++)
            _modeButtons[i].ButtonPressed = modes[i] == mode;

        bool hasDuration = mode is GameMode.Deathmatch or GameMode.CaptureZone;
        bool hasScore = mode == GameMode.CaptureZone;
        _durationLabel.Visible = hasDuration;
        _durationField.Visible = hasDuration;
        _scoreLabel.Visible = hasScore;
        _scoreField.Visible = hasScore;
    }

    public void OnConnectFailed(string error)
    {
        _connectBtn.Disabled = false;
        ShowStatus($"Erreur : {error}");
    }

    public void OnAdminLoginResponse(bool success, string error)
    {
        _connectBtn.Disabled = false;
        if (!success)
        {
            ShowStatus($"Échec : {error}");
            return;
        }

        ShowStatus("Connecté en tant qu'admin");
        _configPanel.Visible = true;
    }

    public void OnConfigApplied(bool success, string error)
    {
        _applyBtn.Disabled = false;
        if (!success)
        {
            ShowStatus($"Erreur config : {error}");
            return;
        }

        _configApplied = true;
        _playBtn.Visible = true;
        ShowStatus("Configuration appliquée — serveur en attente de joueurs");
    }

    private void ResetToConnect()
    {
        _configPanel.Visible = false;
        _playBtn.Visible = false;
        _configApplied = false;
        _connectBtn.Disabled = false;
        ShowStatus("");
    }

    private void ShowStatus(string msg) => _statusLabel.Text = msg;
}

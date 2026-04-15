using System;
using Godot;

namespace BattleTank.Godot.UI;

/// <summary>
/// Overlay shown before a player is authenticated.
/// Provides username/password fields and Login / Register buttons.
/// </summary>
public partial class LoginScreen : CanvasLayer
{
    public event Action<string, string>? LoginRequested;
    public event Action<string, string>? RegisterRequested;
    public event Action? TrainingRequested;

    private LineEdit _usernameField = null!;
    private LineEdit _passwordField = null!;
    private Label _statusLabel = null!;
    private Button _loginButton = null!;
    private Button _registerButton = null!;
    private Button _trainingButton = null!;

    public override void _Ready()
    {
        var root = new Control();
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);

        var center = new CenterContainer();
        center.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        root.AddChild(center);

        var panel = new PanelContainer();
        panel.CustomMinimumSize = new Vector2(320, 220);
        center.AddChild(panel);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 10);
        panel.AddChild(vbox);

        var title = new Label { Text = "Battle Tank — Login" };
        title.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(title);

        _usernameField = new LineEdit { PlaceholderText = "Username" };
        vbox.AddChild(_usernameField);

        _passwordField = new LineEdit { PlaceholderText = "Password", Secret = true };
        vbox.AddChild(_passwordField);

        var buttonRow = new HBoxContainer();
        vbox.AddChild(buttonRow);

        _loginButton = new Button { Text = "Login", Disabled = true };
        _loginButton.Pressed += OnLoginPressed;
        buttonRow.AddChild(_loginButton);

        _registerButton = new Button { Text = "Register", Disabled = true };
        _registerButton.Pressed += OnRegisterPressed;
        buttonRow.AddChild(_registerButton);

        _trainingButton = new Button { Text = "Mode Entraînement", Disabled = true };
        _trainingButton.Pressed += OnTrainingPressed;
        vbox.AddChild(_trainingButton);

        _statusLabel = new Label { Text = "Connecting to server…" };
        _statusLabel.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(_statusLabel);
    }

    public void OnConnected()
    {
        _loginButton.Disabled = false;
        _registerButton.Disabled = false;
        _trainingButton.Disabled = false;
        _statusLabel.Text = "Connected. Enter credentials.";
    }

    public void ShowError(string message)
    {
        _statusLabel.Text = message;
        _loginButton.Disabled = false;
        _registerButton.Disabled = false;
    }

    private void OnLoginPressed()
    {
        var username = _usernameField.Text.Trim();
        var password = _passwordField.Text;
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            _statusLabel.Text = "Username and password required";
            return;
        }

        _loginButton.Disabled = true;
        _registerButton.Disabled = true;
        _statusLabel.Text = "Logging in…";
        LoginRequested?.Invoke(username, password);
    }

    private void OnTrainingPressed()
    {
        TrainingRequested?.Invoke();
    }

    private void OnRegisterPressed()
    {
        var username = _usernameField.Text.Trim();
        var password = _passwordField.Text;
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            _statusLabel.Text = "Username and password required";
            return;
        }

        _loginButton.Disabled = true;
        _registerButton.Disabled = true;
        _statusLabel.Text = "Registering…";
        RegisterRequested?.Invoke(username, password);
    }
}

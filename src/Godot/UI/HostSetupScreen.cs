using System;
using Godot;
using BattleTank.GameLogic.Shared;

namespace BattleTank.Godot.UI;

/// <summary>
/// Configuration screen before starting an in-process server.
/// Lets the host set the game name, port, and an optional room code.
/// </summary>
public partial class HostSetupScreen : CanvasLayer
{
    public event Action<string, int, string?>? HostConfigured;
    public event Action? BackRequested;

    private LineEdit _nameField = null!;
    private LineEdit _portField = null!;
    private LineEdit _codeField = null!;
    private Label _statusLabel = null!;

    public override void _Ready()
    {
        var panel = new PanelContainer();
        panel.SetAnchorsPreset(Control.LayoutPreset.Center);
        panel.CustomMinimumSize = new Vector2(320, 240);
        AddChild(panel);

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

        _statusLabel = new Label { Text = "" };
        _statusLabel.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(_statusLabel);

        var btn = new Button { Text = "Héberger" };
        btn.Pressed += OnHostPressed;
        vbox.AddChild(btn);

        var back = new Button { Text = "← Retour" };
        back.Pressed += () => BackRequested?.Invoke();
        vbox.AddChild(back);
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

        string? code = string.IsNullOrWhiteSpace(_codeField.Text) ? null : _codeField.Text.Trim();
        HostConfigured?.Invoke(name, port, code);
    }
}

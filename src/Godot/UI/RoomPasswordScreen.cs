using System;
using Godot;

namespace BattleTank.Godot.UI;

/// <summary>
/// Shown before LoginScreen when the target server has a room code.
/// </summary>
public partial class RoomPasswordScreen : CanvasLayer
{
    public event Action<string>? PasswordConfirmed;
    public event Action? BackRequested;

    private LineEdit _codeField = null!;
    private Label _statusLabel = null!;

    public override void _Ready()
    {
        var root = new Control();
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);

        var center = new CenterContainer();
        center.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        root.AddChild(center);

        var panel = new PanelContainer();
        panel.CustomMinimumSize = new Vector2(300, 160);
        center.AddChild(panel);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 10);
        panel.AddChild(vbox);

        var title = new Label { Text = "Code de la partie" };
        title.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(title);

        _codeField = new LineEdit { PlaceholderText = "Code" };
        vbox.AddChild(_codeField);

        _statusLabel = new Label { Text = "" };
        _statusLabel.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(_statusLabel);

        var row = new HBoxContainer();
        vbox.AddChild(row);

        var confirm = new Button { Text = "Rejoindre" };
        confirm.Pressed += OnConfirmPressed;
        row.AddChild(confirm);

        var back = new Button { Text = "← Retour" };
        back.Pressed += () => BackRequested?.Invoke();
        row.AddChild(back);
    }

    public void ShowError(string message) => _statusLabel.Text = message;

    private void OnConfirmPressed()
    {
        string code = _codeField.Text.Trim();
        if (string.IsNullOrEmpty(code))
        {
            _statusLabel.Text = "Code requis";
            return;
        }
        PasswordConfirmed?.Invoke(code);
    }
}

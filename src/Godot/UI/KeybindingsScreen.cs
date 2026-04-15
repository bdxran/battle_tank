using System;
using System.Collections.Generic;
using Godot;
using BattleTank.Godot.Settings;

namespace BattleTank.Godot.UI;

/// <summary>
/// Key remapping screen. Click a button to capture the next key press for that action.
/// </summary>
public partial class KeybindingsScreen : CanvasLayer
{
    public event Action? BackRequested;

    private string? _pendingAction;
    private Button? _pendingButton;
    private readonly Dictionary<string, Button> _buttons = new();

    public override void _Ready()
    {
        var root = new Control();
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);

        var center = new CenterContainer();
        center.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        root.AddChild(center);

        var panel = new PanelContainer();
        panel.CustomMinimumSize = new Vector2(400, 350);
        center.AddChild(panel);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 10);
        panel.AddChild(vbox);

        var title = new Label { Text = "Configuration des touches" };
        title.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(title);

        foreach (var (action, label, _) in InputSettings.Actions)
        {
            var row = new HBoxContainer();
            vbox.AddChild(row);

            var lbl = new Label { Text = label };
            lbl.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            row.AddChild(lbl);

            var btn = new Button();
            btn.CustomMinimumSize = new Vector2(140, 0);
            btn.Text = KeyLabel(InputSettings.GetCurrentKey(action));
            var capturedAction = action;
            btn.Pressed += () => StartCapture(capturedAction, btn);
            row.AddChild(btn);

            _buttons[action] = btn;
        }

        var separator = new HSeparator();
        vbox.AddChild(separator);

        var back = new Button { Text = "← Retour" };
        back.Pressed += () => BackRequested?.Invoke();
        vbox.AddChild(back);

        Hide();
    }

    public override void _Input(InputEvent @event)
    {
        if (_pendingAction is null || _pendingButton is null) return;
        if (@event is not InputEventKey keyEvent) return;
        if (!keyEvent.Pressed || keyEvent.Echo) return;

        var key = keyEvent.Keycode;
        if (key == Key.Escape)
        {
            // Cancel — restore previous label
            _pendingButton.Text = KeyLabel(InputSettings.GetCurrentKey(_pendingAction));
        }
        else
        {
            InputSettings.Rebind(_pendingAction, key);
            _pendingButton.Text = KeyLabel(key);
        }

        _pendingAction = null;
        _pendingButton = null;
        GetViewport().SetInputAsHandled();
    }

    private void StartCapture(string action, Button btn)
    {
        _pendingAction = action;
        _pendingButton = btn;
        btn.Text = "En attente…";
    }

    private static string KeyLabel(Key key) =>
        key == Key.None ? "—" : OS.GetKeycodeString(key);
}

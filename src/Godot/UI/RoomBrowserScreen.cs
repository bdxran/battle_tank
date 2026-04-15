using System;
using Godot;
using BattleTank.GameLogic.Shared;
using BattleTank.Godot.Network;

namespace BattleTank.Godot.UI;

/// <summary>
/// Displays discovered LAN servers and allows manual IP entry.
/// Handles room code prompting before joining.
/// </summary>
public partial class RoomBrowserScreen : CanvasLayer
{
    public event Action<string, int, string?>? JoinRequested;
    public event Action? BackRequested;

    private ItemList _serverList = null!;
    private LineEdit _ipField = null!;
    private LineEdit _portField = null!;
    private Button _joinBtn = null!;
    private Label _statusLabel = null!;

    private LanDiscovery _discovery = null!;
    private ServerAnnouncement[] _servers = [];
    private RoomPasswordScreen? _passwordScreen;
    private string _pendingAddress = "";
    private int _pendingPort;

    public override void _Ready()
    {
        _discovery = new LanDiscovery();
        _discovery.ServerListChanged += OnServerListChanged;
        AddChild(_discovery);
        _discovery.StartListening();

        var root = new Control();
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);

        var center = new CenterContainer();
        center.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        root.AddChild(center);

        var panel = new PanelContainer();
        panel.CustomMinimumSize = new Vector2(420, 340);
        center.AddChild(panel);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 8);
        panel.AddChild(vbox);

        var title = new Label { Text = "Rejoindre une partie" };
        title.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(title);

        var listLabel = new Label { Text = "Parties disponibles (LAN) :" };
        vbox.AddChild(listLabel);

        _serverList = new ItemList();
        _serverList.CustomMinimumSize = new Vector2(0, 120);
        _serverList.ItemSelected += OnServerSelected;
        vbox.AddChild(_serverList);

        var sep = new HSeparator();
        vbox.AddChild(sep);

        var manualLabel = new Label { Text = "Connexion manuelle :" };
        vbox.AddChild(manualLabel);

        var row = new HBoxContainer();
        vbox.AddChild(row);

        _ipField = new LineEdit { PlaceholderText = "IP (ex: 192.168.1.10)", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        row.AddChild(_ipField);

        _portField = new LineEdit { PlaceholderText = "Port", Text = Constants.ServerPort.ToString(), CustomMinimumSize = new Vector2(70, 0) };
        row.AddChild(_portField);

        _statusLabel = new Label { Text = "" };
        _statusLabel.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(_statusLabel);

        var btnRow = new HBoxContainer();
        vbox.AddChild(btnRow);

        _joinBtn = new Button { Text = "Rejoindre" };
        _joinBtn.Pressed += OnJoinPressed;
        btnRow.AddChild(_joinBtn);

        var back = new Button { Text = "← Retour" };
        back.Pressed += () => BackRequested?.Invoke();
        btnRow.AddChild(back);
    }

    private void OnServerListChanged(ServerAnnouncement[] servers)
    {
        _servers = servers;
        _serverList.Clear();
        foreach (var s in servers)
        {
            string codeTag = s.HasCode ? " [code]" : "";
            _serverList.AddItem($"{s.Name} — {s.Address}:{s.Port} ({s.Mode}, {s.Players} joueurs){codeTag}");
        }
    }

    private void OnServerSelected(long index)
    {
        if (index < 0 || index >= _servers.Length) return;
        var s = _servers[(int)index];
        _ipField.Text = s.Address;
        _portField.Text = s.Port.ToString();
    }

    private void OnJoinPressed()
    {
        string address = _ipField.Text.Trim();
        if (string.IsNullOrEmpty(address))
        {
            _statusLabel.Text = "Adresse requise";
            return;
        }

        if (!int.TryParse(_portField.Text.Trim(), out int port) || port < 1 || port > 65535)
        {
            _statusLabel.Text = "Port invalide";
            return;
        }

        // Check if selected server has a code
        bool hasCode = false;
        foreach (var s in _servers)
        {
            if (s.Address == address && s.Port == port)
            {
                hasCode = s.HasCode;
                break;
            }
        }

        if (hasCode)
        {
            ShowPasswordPrompt(address, port);
        }
        else
        {
            JoinRequested?.Invoke(address, port, null);
        }
    }

    private void ShowPasswordPrompt(string address, int port)
    {
        _pendingAddress = address;
        _pendingPort = port;

        _passwordScreen = new RoomPasswordScreen();
        _passwordScreen.PasswordConfirmed += OnPasswordConfirmed;
        _passwordScreen.BackRequested += OnPasswordBack;
        AddChild(_passwordScreen);
        _passwordScreen.Show();
    }

    private void OnPasswordConfirmed(string code)
    {
        CleanupPasswordScreen();
        JoinRequested?.Invoke(_pendingAddress, _pendingPort, code);
    }

    private void OnPasswordBack()
    {
        CleanupPasswordScreen();
    }

    private void CleanupPasswordScreen()
    {
        if (_passwordScreen is null) return;
        _passwordScreen.PasswordConfirmed -= OnPasswordConfirmed;
        _passwordScreen.BackRequested -= OnPasswordBack;
        _passwordScreen.QueueFree();
        _passwordScreen = null;
    }

    public override void _ExitTree()
    {
        _discovery.ServerListChanged -= OnServerListChanged;
    }
}

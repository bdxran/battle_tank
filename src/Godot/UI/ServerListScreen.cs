using System;
using System.Collections.Generic;
using Godot;
using BattleTank.GameLogic.Network;
using BattleTank.GameLogic.Shared;
using BattleTank.Godot.Persistence;

namespace BattleTank.Godot.UI;

/// <summary>
/// Écran de liste de serveurs sauvegardés. Permet d'ajouter, supprimer, consulter le statut et rejoindre un serveur.
/// </summary>
public partial class ServerListScreen : CanvasLayer
{
    public event Action<string, int, string?>? JoinRequested;  // address, port, roomCode
    public event Action<string, int>? StatusRequested;         // address, port (connect pour ping)
    public event Action? BackRequested;

    private readonly SavedServerRepository _repo = new();
    private List<SavedServer> _servers = [];

    private VBoxContainer _serverList = null!;
    private VBoxContainer _detailPanel = null!;
    private Label _detailName = null!;
    private Label _detailMode = null!;
    private Label _detailPlayers = null!;
    private Label _detailRules = null!;
    private Label _detailStatus = null!;
    private Button _joinBtn = null!;
    private Button _deleteBtn = null!;
    private int _selectedIndex = -1;
    private ServerStatusResponse? _selectedStatus;

    // Champs pour ajouter un serveur
    private LineEdit _addNameField = null!;
    private LineEdit _addAddressField = null!;
    private LineEdit _addPortField = null!;

    public override void _Ready()
    {
        var root = new Control();
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);

        var hbox = new HBoxContainer();
        hbox.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        hbox.AddThemeConstantOverride("separation", 12);
        root.AddChild(hbox);

        // ── Colonne gauche : liste + ajout ──────────────────
        var leftPanel = new PanelContainer();
        leftPanel.CustomMinimumSize = new Vector2(260, 0);
        leftPanel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        hbox.AddChild(leftPanel);

        var leftVbox = new VBoxContainer();
        leftVbox.AddThemeConstantOverride("separation", 8);
        leftPanel.AddChild(leftVbox);

        var listTitle = new Label { Text = "Mes serveurs" };
        listTitle.HorizontalAlignment = HorizontalAlignment.Center;
        leftVbox.AddChild(listTitle);

        var scroll = new ScrollContainer();
        scroll.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        leftVbox.AddChild(scroll);

        _serverList = new VBoxContainer();
        scroll.AddChild(_serverList);

        // Formulaire ajout
        leftVbox.AddChild(new HSeparator());
        leftVbox.AddChild(new Label { Text = "Ajouter un serveur" });

        _addNameField = new LineEdit { PlaceholderText = "Nom" };
        leftVbox.AddChild(_addNameField);
        _addAddressField = new LineEdit { PlaceholderText = "Adresse (ex: 192.168.1.10)" };
        leftVbox.AddChild(_addAddressField);
        _addPortField = new LineEdit { PlaceholderText = "Port (4242)", Text = "4242" };
        leftVbox.AddChild(_addPortField);

        var addBtn = new Button { Text = "Ajouter" };
        addBtn.Pressed += OnAddServer;
        leftVbox.AddChild(addBtn);

        var backBtn = new Button { Text = "Retour" };
        backBtn.Pressed += () => BackRequested?.Invoke();
        leftVbox.AddChild(backBtn);

        // ── Colonne droite : détail du serveur sélectionné ──
        var rightPanel = new PanelContainer();
        rightPanel.CustomMinimumSize = new Vector2(280, 0);
        rightPanel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        hbox.AddChild(rightPanel);

        _detailPanel = new VBoxContainer();
        _detailPanel.AddThemeConstantOverride("separation", 8);
        _detailPanel.Visible = false;
        rightPanel.AddChild(_detailPanel);

        _detailName = new Label { Text = "" };
        _detailName.HorizontalAlignment = HorizontalAlignment.Center;
        _detailPanel.AddChild(_detailName);

        _detailStatus = new Label { Text = "" };
        _detailPanel.AddChild(_detailStatus);

        _detailMode = new Label { Text = "" };
        _detailPanel.AddChild(_detailMode);

        _detailRules = new Label { Text = "" };
        _detailPanel.AddChild(_detailRules);

        _detailPlayers = new Label { Text = "" };
        _detailPanel.AddChild(_detailPlayers);

        _joinBtn = new Button { Text = "Rejoindre" };
        _joinBtn.Pressed += OnJoinSelected;
        _joinBtn.Disabled = true;
        _detailPanel.AddChild(_joinBtn);

        _deleteBtn = new Button { Text = "Supprimer ce serveur" };
        _deleteBtn.Pressed += OnDeleteSelected;
        _detailPanel.AddChild(_deleteBtn);

        RefreshList();
    }

    private void RefreshList()
    {
        _servers = _repo.Load();
        foreach (var child in _serverList.GetChildren())
            child.QueueFree();

        for (int i = 0; i < _servers.Count; i++)
        {
            var idx = i;
            var srv = _servers[i];
            var btn = new Button { Text = $"{srv.Name}  ({srv.Address}:{srv.Port})" };
            btn.Pressed += () => SelectServer(idx);
            _serverList.AddChild(btn);
        }

        if (_servers.Count == 0)
        {
            _serverList.AddChild(new Label { Text = "Aucun serveur enregistré" });
            _detailPanel.Visible = false;
            _selectedIndex = -1;
        }
    }

    private void SelectServer(int index)
    {
        _selectedIndex = index;
        _selectedStatus = null;
        _joinBtn.Disabled = true;

        var srv = _servers[index];
        _detailName.Text = srv.Name;
        _detailStatus.Text = "Récupération du statut…";
        _detailMode.Text = "";
        _detailRules.Text = "";
        _detailPlayers.Text = "";
        _detailPanel.Visible = true;

        StatusRequested?.Invoke(srv.Address, srv.Port);
    }

    public void ShowStatus(ServerStatusResponse status)
    {
        if (_selectedIndex < 0) return;
        _selectedStatus = status;

        string modeLabel = status.Mode switch
        {
            GameMode.BattleRoyale => "Battle Royale",
            GameMode.Deathmatch => "Deathmatch",
            GameMode.Teams => "Équipes",
            GameMode.CaptureZone => "Capture de zone",
            _ => status.Mode.ToString(),
        };
        string phaseLabel = status.Phase switch
        {
            GamePhase.WaitingForPlayers => "En attente",
            GamePhase.Lobby => "Lobby",
            GamePhase.InProgress => "Partie en cours",
            GamePhase.GameOver => "Terminée",
            _ => status.Phase.ToString(),
        };

        _detailStatus.Text = $"Statut : {phaseLabel}";
        _detailMode.Text = $"Mode : {modeLabel}";

        var rules = new System.Text.StringBuilder();
        if (status.Mode is GameMode.Deathmatch or GameMode.CaptureZone)
            rules.Append($"Durée : {status.DurationSeconds / 60} min  ");
        if (status.Mode == GameMode.CaptureZone)
            rules.Append($"Score : {status.ScoreToWin}  ");
        if (status.HasCode)
            rules.Append("[Code requis]");
        _detailRules.Text = rules.ToString().Trim();

        _detailPlayers.Text = $"Joueurs : {status.PlayerCount}\n{string.Join(", ", status.Players)}";

        _joinBtn.Disabled = status.Phase == GamePhase.InProgress;
    }

    public void ShowStatusError()
    {
        if (_selectedIndex < 0) return;
        _detailStatus.Text = "Hors ligne ou inaccessible";
        _detailMode.Text = "";
        _detailRules.Text = "";
        _detailPlayers.Text = "";
        _joinBtn.Disabled = true;
    }

    private void OnJoinSelected()
    {
        if (_selectedIndex < 0 || _selectedIndex >= _servers.Count) return;
        var srv = _servers[_selectedIndex];
        string? roomCode = (_selectedStatus?.HasCode == true) ? PromptCode() : null;
        JoinRequested?.Invoke(srv.Address, srv.Port, roomCode);
    }

    private static string? PromptCode()
    {
        // Simple fallback: l'écran de mot de passe de salle gérera ça dans ClientNode
        return null;
    }

    private void OnAddServer()
    {
        var name = _addNameField.Text.Trim();
        var address = _addAddressField.Text.Trim();
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(address)) return;
        if (!int.TryParse(_addPortField.Text.Trim(), out int port) || port < 1 || port > 65535) return;

        _servers.Add(new SavedServer(name, address, port));
        _repo.Save(_servers);

        _addNameField.Text = "";
        _addAddressField.Text = "";
        _addPortField.Text = "4242";
        RefreshList();
    }

    private void OnDeleteSelected()
    {
        if (_selectedIndex < 0 || _selectedIndex >= _servers.Count) return;
        _servers.RemoveAt(_selectedIndex);
        _repo.Save(_servers);
        _selectedIndex = -1;
        _detailPanel.Visible = false;
        RefreshList();
    }
}

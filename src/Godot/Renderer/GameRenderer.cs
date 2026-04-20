using System.Collections.Generic;
using System;
using Godot;
using BattleTank.GameLogic.Network;
using BattleTank.GameLogic.Shared;
using BattleTank.Godot.Nodes;
using BattleTank.Godot.UI;

namespace BattleTank.Godot.Renderer;

public partial class GameRenderer : Node2D
{
    private readonly Dictionary<int, TankNode> _tankNodes = new();
    private readonly Dictionary<int, BulletNode> _bulletNodes = new();
    private readonly Dictionary<int, int> _tankPrevHealth = new();
    private readonly List<WallNode> _wallNodes = new();

    private readonly Dictionary<int, int> _playerTeamMap = new();
    private int _localTeamId = -1;

    private Network.IGameStateProvider _network = null!;
    private HudNode _hud = null!;
    private ZoneNode _zoneNode = null!;
    private ControlPointsNode _controlPointsNode = null!;
    private KillFeedNode _killFeed = null!;
    private Camera2D _camera = null!;
    private int _localPlayerId;
    private bool _spectating;
    private GameMode _gameMode;
    private int _ticksRemaining;
    private int[] _teamScores = [];
    private int _localKills;
    private int _localDeaths;

    public event Action? BulletCreated;
    public event Action? TankHit;
    public event Action? TankEliminated;

    public void Initialize(Network.IGameStateProvider network, HudNode hud, int localPlayerId)
    {
        // Unsubscribe from previous provider to avoid duplicate events
        if (_network is not null)
        {
            _network.GameStateFullReceived -= OnGameStateFull;
            _network.GameStateDeltaReceived -= OnGameStateDelta;
            _network.PlayerEliminated -= OnPlayerEliminated;
        }

        // Reset team state
        _playerTeamMap.Clear();
        _localTeamId = -1;

        // Free nodes from previous game session
        foreach (var node in _tankNodes.Values) node.QueueFree();
        _tankNodes.Clear();
        foreach (var node in _bulletNodes.Values) node.QueueFree();
        _bulletNodes.Clear();
        _tankPrevHealth.Clear();
        foreach (var wall in _wallNodes) wall.QueueFree();
        _wallNodes.Clear();
        _zoneNode?.QueueFree();
        _controlPointsNode?.QueueFree();
        _killFeed?.QueueFree();
        _camera?.QueueFree();
        _spectating = false;

        _network = network;
        _hud = hud;
        _localPlayerId = localPlayerId;
        _localKills = 0;
        _localDeaths = 0;
        _ticksRemaining = 0;
        _teamScores = [];
        _gameMode = GameMode.BattleRoyale;

        _hud.Initialize(localPlayerId);

        _zoneNode = new ZoneNode();
        AddChild(_zoneNode);

        _controlPointsNode = new ControlPointsNode();
        AddChild(_controlPointsNode);

        foreach (var wall in GameLogic.Shared.MapLayout.Walls)
        {
            var wallNode = new WallNode();
            wallNode.Initialize(wall);
            AddChild(wallNode);
            _wallNodes.Add(wallNode);
        }

        _killFeed = new KillFeedNode();
        AddChild(_killFeed);

        _camera = new Camera2D { Enabled = true };
        AddChild(_camera);

        _network.GameStateFullReceived += OnGameStateFull;
        _network.GameStateDeltaReceived += OnGameStateDelta;
        _network.PlayerEliminated += OnPlayerEliminated;
    }

    public void EnterSpectatorMode()
    {
        _spectating = true;
        _camera.Enabled = true;
    }

    public void ExitSpectatorMode()
    {
        _spectating = false;
    }

    public override void _ExitTree()
    {
        if (_network is null) return;
        _network.GameStateFullReceived -= OnGameStateFull;
        _network.GameStateDeltaReceived -= OnGameStateDelta;
        _network.PlayerEliminated -= OnPlayerEliminated;
    }

    private void OnGameStateFull(GameStateFull state)
    {
        _gameMode = state.Mode;
        _ticksRemaining = state.TicksRemaining;
        _teamScores = state.TeamScores ?? [];

        // Build team map from player info so TankNodes can be coloured correctly
        _playerTeamMap.Clear();
        foreach (var player in state.Players)
        {
            _playerTeamMap[player.Id] = player.TeamId;
            if (player.Id == _localPlayerId)
                _localKills = player.Kills;
        }
        _localTeamId = _playerTeamMap.TryGetValue(_localPlayerId, out int lt) ? lt : -1;
        _hud.SetTeamInfo(_localTeamId, _playerTeamMap);

        foreach (var snapshot in state.Tanks)
        {
            GetOrCreateTankNode(snapshot.Id).UpdateFrom(snapshot);
            _tankPrevHealth[snapshot.Id] = snapshot.Health;
        }

        _zoneNode.Visible = state.Mode == GameMode.BattleRoyale;
        SyncBullets(state.Bullets);
        _zoneNode.UpdateFrom(state.Zone);
        _controlPointsNode.UpdateFrom(state.ControlPoints);
        UpdateHud(state.Tanks, state.Zone, state.ControlPoints);

        foreach (var tank in state.Tanks)
        {
            if (tank.Id == _localPlayerId)
            {
                _camera.Position = new Vector2(tank.X, tank.Y);
                break;
            }
        }
    }

    private void OnGameStateDelta(GameStateDelta state)
    {
        _ticksRemaining = state.TicksRemaining;
        if (state.TeamScores is { Length: > 0 })
            _teamScores = state.TeamScores;

        foreach (var snapshot in state.Tanks)
        {
            if (_tankNodes.TryGetValue(snapshot.Id, out var node))
                node.UpdateFrom(snapshot);

            if (_tankPrevHealth.TryGetValue(snapshot.Id, out int prevHp))
            {
                if (snapshot.Health <= 0 && prevHp > 0)
                    TankEliminated?.Invoke();
                else if (snapshot.Health < prevHp && snapshot.Health > 0)
                    TankHit?.Invoke();
            }

            _tankPrevHealth[snapshot.Id] = snapshot.Health;
        }

        SyncBullets(state.Bullets);
        _zoneNode.UpdateFrom(state.Zone);
        _controlPointsNode.UpdateFrom(state.ControlPoints);
        UpdateHud(state.Tanks, state.Zone, state.ControlPoints);

        if (_spectating)
            UpdateSpectatorCamera(state.Tanks);
        else
            UpdatePlayerCamera(state.Tanks);
    }

    private TankNode GetOrCreateTankNode(int playerId)
    {
        if (_tankNodes.TryGetValue(playerId, out var existing))
            return existing;

        bool isLocal = playerId == _localPlayerId;
        bool isAlly = !isLocal && _localTeamId >= 0
            && _playerTeamMap.TryGetValue(playerId, out int tid) && tid == _localTeamId;
        var node = new TankNode();
        node.Initialize(playerId, isLocal, isAlly);
        AddChild(node);
        _tankNodes[playerId] = node;
        return node;
    }

    private void SyncBullets(BulletSnapshot[] snapshots)
    {
        var activeIds = new HashSet<int>();

        foreach (var snapshot in snapshots)
        {
            activeIds.Add(snapshot.Id);

            if (!_bulletNodes.TryGetValue(snapshot.Id, out var node))
            {
                node = new BulletNode();
                node.Initialize(snapshot.Id);
                AddChild(node);
                _bulletNodes[snapshot.Id] = node;
                BulletCreated?.Invoke();
            }

            node.UpdateFrom(snapshot);
        }

        var toRemove = new List<int>();
        foreach (var id in _bulletNodes.Keys)
        {
            if (!activeIds.Contains(id))
                toRemove.Add(id);
        }

        foreach (var id in toRemove)
        {
            _bulletNodes[id].QueueFree();
            _bulletNodes.Remove(id);
        }
    }

    private void OnPlayerEliminated(PlayerEliminatedMessage msg)
    {
        _killFeed.AddEntry(msg.EliminatedPlayerId, msg.KillerPlayerId);
        if (msg.KillerPlayerId == _localPlayerId) _localKills++;
        if (msg.EliminatedPlayerId == _localPlayerId) _localDeaths++;
    }

    private void UpdateHud(TankSnapshot[] tanks, ZoneSnapshot zone, ControlPointSnapshot[] controlPoints)
    {
        int aliveCount = 0;
        int localHealth = 0;

        foreach (var tank in tanks)
        {
            if (tank.Health > 0)
                aliveCount++;
            if (tank.Id == _localPlayerId)
                localHealth = tank.Health;
        }

        _hud.UpdateHealth(localHealth);
        _hud.UpdateAliveCount(aliveCount);
        _hud.UpdateTimer(_ticksRemaining);
        _hud.UpdateScore(_teamScores, _localKills, _localDeaths, _gameMode);
        _hud.UpdateMinimap(tanks, zone, controlPoints);
    }

    private void UpdatePlayerCamera(TankSnapshot[] tanks)
    {
        foreach (var tank in tanks)
        {
            if (tank.Id == _localPlayerId && tank.Health > 0)
            {
                _camera.Position = new Vector2(tank.X, tank.Y);
                return;
            }
        }
    }

    private void UpdateSpectatorCamera(TankSnapshot[] tanks)
    {
        foreach (var tank in tanks)
        {
            if (tank.Health > 0)
            {
                _camera.Position = new Vector2(tank.X, tank.Y);
                return;
            }
        }
    }
}

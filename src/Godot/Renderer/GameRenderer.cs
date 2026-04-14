using System.Collections.Generic;
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

    private Network.ClientNetworkManager _network = null!;
    private HudNode _hud = null!;
    private ZoneNode _zoneNode = null!;
    private ControlPointsNode _controlPointsNode = null!;
    private KillFeedNode _killFeed = null!;
    private int _localPlayerId;

    public void Initialize(Network.ClientNetworkManager network, HudNode hud, int localPlayerId)
    {
        _network = network;
        _hud = hud;
        _localPlayerId = localPlayerId;

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
        }

        _killFeed = new KillFeedNode();
        AddChild(_killFeed);

        _network.GameStateFullReceived += OnGameStateFull;
        _network.GameStateDeltaReceived += OnGameStateDelta;
        _network.PlayerEliminated += OnPlayerEliminated;
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
        foreach (var snapshot in state.Tanks)
            GetOrCreateTankNode(snapshot.Id).UpdateFrom(snapshot);

        SyncBullets(state.Bullets);
        _zoneNode.UpdateFrom(state.Zone);
        _controlPointsNode.UpdateFrom(state.ControlPoints);
        UpdateHud(state.Tanks, state.Zone, state.ControlPoints);
    }

    private void OnGameStateDelta(GameStateDelta state)
    {
        foreach (var snapshot in state.Tanks)
        {
            if (_tankNodes.TryGetValue(snapshot.Id, out var node))
                node.UpdateFrom(snapshot);
        }

        SyncBullets(state.Bullets);
        _zoneNode.UpdateFrom(state.Zone);
        _controlPointsNode.UpdateFrom(state.ControlPoints);
        UpdateHud(state.Tanks, state.Zone, state.ControlPoints);
    }

    private TankNode GetOrCreateTankNode(int playerId)
    {
        if (_tankNodes.TryGetValue(playerId, out var existing))
            return existing;

        var node = new TankNode();
        node.Initialize(playerId, playerId == _localPlayerId);
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
        _hud.UpdateMinimap(tanks, zone, controlPoints);
    }
}

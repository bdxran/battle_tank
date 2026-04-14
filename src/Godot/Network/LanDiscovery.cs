using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Godot;

namespace BattleTank.Godot.Network;

/// <summary>
/// Listens for LAN broadcast packets from LanAnnouncer.
/// Maintains a server list with a 6-second TTL and fires ServerListChanged on changes.
/// </summary>
public partial class LanDiscovery : Node
{
    private const int ListenPort = 4243;
    private const double TtlSeconds = 6.0;

    public event Action<ServerAnnouncement[]>? ServerListChanged;

    private UdpClient? _udp;
    private readonly Dictionary<string, (ServerAnnouncement Info, double LastSeen)> _servers = new();
    private double _now;

    public void StartListening()
    {
        try
        {
            _udp = new UdpClient(ListenPort) { EnableBroadcast = true };
            GD.Print($"[LanDiscovery] Listening on port {ListenPort}");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[LanDiscovery] Failed to bind: {ex.Message}");
        }
    }

    public void StopListening()
    {
        _udp?.Close();
        _udp = null;
        _servers.Clear();
    }

    public override void _Process(double delta)
    {
        if (_udp is null) return;

        _now += delta;
        bool changed = false;

        // Non-blocking receive
        while (_udp.Available > 0)
        {
            try
            {
                var ep = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = _udp.Receive(ref ep);
                string json = Encoding.UTF8.GetString(data);
                var announcement = JsonSerializer.Deserialize<ServerAnnouncement>(json);
                if (announcement is null) continue;

                // Use sender address (overrides what the payload says)
                announcement = announcement with { Address = ep.Address.ToString() };
                string key = $"{announcement.Address}:{announcement.Port}";
                _servers[key] = (announcement, _now);
                changed = true;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[LanDiscovery] Receive error: {ex.Message}");
            }
        }

        // Expire stale entries
        var toRemove = new List<string>();
        foreach (var (key, entry) in _servers)
        {
            if (_now - entry.LastSeen > TtlSeconds)
                toRemove.Add(key);
        }
        foreach (var key in toRemove)
        {
            _servers.Remove(key);
            changed = true;
        }

        if (changed)
            FireServerListChanged();
    }

    private void FireServerListChanged()
    {
        var list = new ServerAnnouncement[_servers.Count];
        int i = 0;
        foreach (var entry in _servers.Values)
            list[i++] = entry.Info;
        ServerListChanged?.Invoke(list);
    }

    public override void _ExitTree() => StopListening();
}

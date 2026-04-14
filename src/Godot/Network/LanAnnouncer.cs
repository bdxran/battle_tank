using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Godot;

namespace BattleTank.Godot.Network;

/// <summary>
/// Broadcasts server presence on the local network via UDP every 2 seconds.
/// Receivers use LanDiscovery to populate the room browser.
/// </summary>
public partial class LanAnnouncer : Node
{
    private const int BroadcastPort = 4243;
    private const float BroadcastInterval = 2f;

    private UdpClient? _udp;
    private ServerAnnouncement _announcement = null!;
    private float _timer;
    private bool _active;

    public void Start(ServerAnnouncement announcement)
    {
        _announcement = announcement;
        _udp = new UdpClient { EnableBroadcast = true };
        _active = true;
        _timer = 0f;
        GD.Print($"[LanAnnouncer] Broadcasting '{announcement.Name}' on port {BroadcastPort}");
    }

    public void Stop()
    {
        _active = false;
        _udp?.Close();
        _udp = null;
    }

    public override void _Process(double delta)
    {
        if (!_active || _udp is null) return;

        _timer += (float)delta;
        if (_timer < BroadcastInterval) return;
        _timer = 0f;

        try
        {
            string json = JsonSerializer.Serialize(_announcement);
            byte[] data = Encoding.UTF8.GetBytes(json);
            _udp.Send(data, data.Length, new IPEndPoint(IPAddress.Broadcast, BroadcastPort));
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[LanAnnouncer] Broadcast error: {ex.Message}");
        }
    }

    public override void _ExitTree() => Stop();
}

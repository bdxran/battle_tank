namespace BattleTank.Godot.Network;

/// <summary>Shared announcement payload for LAN discovery (JSON serializable).</summary>
public record ServerAnnouncement(
    string Address,
    int Port,
    string Name,
    int Players,
    string Mode,
    bool HasCode,
    string AppVersion = ""
);

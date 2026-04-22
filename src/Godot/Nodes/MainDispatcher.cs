using Godot;
using BattleTank.Godot.Settings;

namespace BattleTank.Godot.Nodes;

/// <summary>
/// Entry point. Spawns ServerNode when running as dedicated server
/// (exported with dedicated_server=true, or launched with --server arg),
/// otherwise spawns ClientNode.
/// </summary>
public partial class MainDispatcher : Node
{
    public override void _Ready()
    {
        AppPaths.EnsureDirectoriesExist();

        bool isServer = OS.HasFeature("dedicated_server")
            || System.Array.IndexOf(OS.GetCmdlineUserArgs(), "--server") >= 0;

        if (isServer)
        {
            GD.Print("[MainDispatcher] Starting in dedicated server mode");
            AddChild(new ServerNode());
        }
        else
        {
            GD.Print("[MainDispatcher] Starting in client mode");
            AddChild(new ClientNode());
        }
    }
}

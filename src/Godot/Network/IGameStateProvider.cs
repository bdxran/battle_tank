using System;
using BattleTank.GameLogic.Network;

namespace BattleTank.Godot.Network;

/// <summary>
/// Abstraction over any source of game state updates (network or local).
/// Implemented by ClientNetworkManager (network mode) and LocalGameNode (offline mode).
/// </summary>
public interface IGameStateProvider
{
    event Action<GameStateFull>? GameStateFullReceived;
    event Action<GameStateDelta>? GameStateDeltaReceived;
    event Action<PlayerEliminatedMessage>? PlayerEliminated;
}

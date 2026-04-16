using System.Collections.Generic;
using BattleTank.GameLogic.Entities;
using BattleTank.GameLogic.Network;

namespace BattleTank.GameLogic.AI;

/// <summary>
/// Represents an AI-controlled player. ComputeInput is called once per game tick.
/// </summary>
public interface IBot
{
    int PlayerId { get; }

    /// <summary>
    /// Computes the input flags the bot wants to apply this tick.
    /// </summary>
    InputFlags ComputeInput(
        IReadOnlyDictionary<int, TankEntity> tanks,
        IReadOnlyList<ControlPoint> controlPoints,
        uint currentTick);
}

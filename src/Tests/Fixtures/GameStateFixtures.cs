using System.Numerics;
using Microsoft.Extensions.Logging.Abstractions;
using BattleTank.GameLogic.Entities;
using BattleTank.GameLogic.Network;
using BattleTank.GameLogic.Rules;
using BattleTank.GameLogic.Shared;

namespace BattleTank.Tests.Fixtures;

/// <summary>Shared factory methods for test setup.</summary>
public static class GameStateFixtures
{
    public static readonly float Dt = 1f / Constants.TickRate;

    /// <summary>Creates a GameRoom with BattleRoyaleRules and advances through lobby countdown.</summary>
    public static GameRoom CreateStartedRoom(IBattleRules? rules = null)
    {
        var room = rules != null
            ? new GameRoom(NullLogger<GameRoom>.Instance, rules)
            : new GameRoom(NullLogger<GameRoom>.Instance);
        room.AddPlayer(1, "P1");
        room.AddPlayer(2, "P2");
        AdvanceThroughLobby(room);
        return room;
    }

    /// <summary>Creates a TankEntity at the given position with full health.</summary>
    public static TankEntity Tank(int id, float x, float y)
        => new(id, new Vector2(x, y));

    /// <summary>Creates a TankEntity at (500,500) with full health.</summary>
    public static TankEntity Tank(int id)
        => Tank(id, 500f, 500f);

    /// <summary>Advances the room through the full lobby countdown.</summary>
    public static void AdvanceThroughLobby(GameRoom room)
    {
        for (int i = 0; i <= Constants.LobbyCountdownTicks; i++)
            room.Tick(Dt);
    }

    /// <summary>Advances the room by the given number of ticks.</summary>
    public static void AdvanceTicks(GameRoom room, int ticks)
    {
        for (int i = 0; i < ticks; i++)
            room.Tick(Dt);
    }

    /// <summary>Applies a fire input and ticks once.</summary>
    public static void FireAndTick(GameRoom room, int playerId, uint seq)
    {
        room.ApplyInput(playerId, new PlayerInput(playerId, InputFlags.Fire, seq));
        room.Tick(Dt);
    }
}

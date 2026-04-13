using System.Numerics;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using BattleTank.GameLogic.Network;
using BattleTank.GameLogic.Rules;
using BattleTank.GameLogic.Shared;

namespace BattleTank.Tests.Rules;

[TestFixture]
public class GameRoomTests
{
    private GameRoom CreateRoom() => new(NullLogger<GameRoom>.Instance);

    private static void AdvanceThroughLobby(GameRoom room)
    {
        float dt = 1f / Constants.TickRate;
        for (int i = 0; i <= Constants.LobbyCountdownTicks; i++)
            room.Tick(dt);
    }

    [Test]
    public void AddPlayer_Success()
    {
        var room = CreateRoom();
        var result = room.AddPlayer(1);
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Id, Is.EqualTo(1));
    }

    [Test]
    public void AddPlayer_WhenFull_Fails()
    {
        var room = CreateRoom();
        for (int i = 0; i < Constants.MaxPlayersPerRoom; i++)
            room.AddPlayer(i + 1);

        var result = room.AddPlayer(99);
        Assert.That(result.IsSuccess, Is.False);
    }

    [Test]
    public void AddPlayer_WhenInProgress_Fails()
    {
        var room = CreateRoom();
        room.AddPlayer(1);
        room.AddPlayer(2);
        AdvanceThroughLobby(room);
        Assert.That(room.Phase, Is.EqualTo(GamePhase.InProgress));

        var result = room.AddPlayer(3);
        Assert.That(result.IsSuccess, Is.False);
    }

    [Test]
    public void TwoPlayers_TransitionToLobby()
    {
        var room = CreateRoom();
        room.AddPlayer(1);
        Assert.That(room.Phase, Is.EqualTo(GamePhase.WaitingForPlayers));

        room.AddPlayer(2);
        Assert.That(room.Phase, Is.EqualTo(GamePhase.Lobby));
    }

    [Test]
    public void TwoPlayers_AfterLobbyCountdown_TransitionToInProgress()
    {
        var room = CreateRoom();
        room.AddPlayer(1);
        room.AddPlayer(2);
        AdvanceThroughLobby(room);
        Assert.That(room.Phase, Is.EqualTo(GamePhase.InProgress));
    }

    [Test]
    public void RemovePlayer_LastAlive_TransitionsToGameOver()
    {
        var room = CreateRoom();
        room.AddPlayer(1);
        room.AddPlayer(2);
        AdvanceThroughLobby(room);

        room.RemovePlayer(2);
        Assert.That(room.Phase, Is.EqualTo(GamePhase.GameOver));
        Assert.That(room.WinnerId, Is.EqualTo(1));
    }

    [Test]
    public void Tick_WithMoveForward_ChangesPosition()
    {
        var room = CreateRoom();
        var result = room.AddPlayer(1);
        room.AddPlayer(2);
        AdvanceThroughLobby(room);

        var initialPos = new Vector2(result.Value.Position.X, result.Value.Position.Y);
        var input = new PlayerInput(1, InputFlags.MoveForward, 1);
        room.ApplyInput(1, input);

        room.Tick(1f / Constants.TickRate);

        var newPos = result.Value.Position;
        Assert.That(newPos, Is.Not.EqualTo(initialPos));
    }

    [Test]
    public void GetFullState_ReturnsTankCount()
    {
        var room = CreateRoom();
        room.AddPlayer(1);
        room.AddPlayer(2);

        var state = room.GetFullState();
        Assert.That(state.Tanks.Length, Is.EqualTo(2));
    }

    [Test]
    public void Reset_ReturnsToWaitingForPlayers()
    {
        var room = CreateRoom();
        room.AddPlayer(1);
        room.AddPlayer(2);
        AdvanceThroughLobby(room);
        Assert.That(room.Phase, Is.EqualTo(GamePhase.InProgress));

        room.Reset();
        Assert.That(room.Phase, Is.EqualTo(GamePhase.WaitingForPlayers));
        Assert.That(room.WinnerId, Is.EqualTo(-1));
    }
}

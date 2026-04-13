using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using BattleTank.GameLogic.Network;
using BattleTank.GameLogic.Rules;
using BattleTank.GameLogic.Shared;

namespace BattleTank.Tests.Rules;

[TestFixture]
public class LobbyCountdownTests
{
    private GameRoom CreateRoom() => new(NullLogger<GameRoom>.Instance);

    [Test]
    public void TwoPlayers_TransitionsToLobby_NotInProgress()
    {
        var room = CreateRoom();
        room.AddPlayer(1);
        room.AddPlayer(2);

        Assert.That(room.Phase, Is.EqualTo(GamePhase.Lobby));
    }

    [Test]
    public void Tick_WhenLobbyCountdownExpires_TransitionsToInProgress()
    {
        var room = CreateRoom();
        room.AddPlayer(1);
        room.AddPlayer(2);

        float dt = 1f / Constants.TickRate;
        for (int i = 0; i <= Constants.LobbyCountdownTicks; i++)
            room.Tick(dt);

        Assert.That(room.Phase, Is.EqualTo(GamePhase.InProgress));
    }

    [Test]
    public void Tick_BeforeCountdownExpires_StaysInLobby()
    {
        var room = CreateRoom();
        room.AddPlayer(1);
        room.AddPlayer(2);

        float dt = 1f / Constants.TickRate;
        for (int i = 0; i < Constants.LobbyCountdownTicks - 1; i++)
            room.Tick(dt);

        Assert.That(room.Phase, Is.EqualTo(GamePhase.Lobby));
    }

    [Test]
    public void AddPlayer_DuringLobby_IsAllowed()
    {
        var room = CreateRoom();
        room.AddPlayer(1);
        room.AddPlayer(2);
        Assert.That(room.Phase, Is.EqualTo(GamePhase.Lobby));

        var result = room.AddPlayer(3);
        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public void CountdownSecondsRemaining_DecreasesOverTime()
    {
        var room = CreateRoom();
        room.AddPlayer(1);
        room.AddPlayer(2);

        int initialSeconds = room.CountdownSecondsRemaining;

        float dt = 1f / Constants.TickRate;
        for (int i = 0; i < Constants.TickRate; i++) // advance 1 second
            room.Tick(dt);

        Assert.That(room.CountdownSecondsRemaining, Is.LessThan(initialSeconds));
    }

    [Test]
    public void CountdownSecondsRemaining_IsZeroOnGameStart()
    {
        var room = CreateRoom();
        room.AddPlayer(1);
        room.AddPlayer(2);

        float dt = 1f / Constants.TickRate;
        for (int i = 0; i <= Constants.LobbyCountdownTicks; i++)
            room.Tick(dt);

        Assert.That(room.CountdownSecondsRemaining, Is.EqualTo(0));
    }
}

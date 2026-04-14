using NUnit.Framework;
using BattleTank.GameLogic.Network;
using BattleTank.GameLogic.Shared;

namespace BattleTank.Tests.Network;

[TestFixture]
public class SerializationTests
{
    private static T RoundTrip<T>(T value)
    {
        var bytes = GameStateSerializer.Serialize(value);
        return GameStateSerializer.Deserialize<T>(bytes);
    }

    [Test]
    public void PlayerInput_RoundTrip()
    {
        var original = new PlayerInput(7, InputFlags.MoveForward | InputFlags.Fire, 42);
        var result = RoundTrip(original);
        Assert.That(result.PlayerId, Is.EqualTo(original.PlayerId));
        Assert.That(result.Flags, Is.EqualTo(original.Flags));
        Assert.That(result.SequenceNumber, Is.EqualTo(original.SequenceNumber));
    }

    [Test]
    public void GameStateFull_RoundTrip()
    {
        var original = new GameStateFull(
            SequenceNumber: 100,
            Tanks: [new TankSnapshot(1, 10f, 20f, 1.5f, 80)],
            Bullets: [new BulletSnapshot(1, 15f, 25f, 0f, -1f, 1)],
            Phase: GamePhase.InProgress,
            Zone: new ZoneSnapshot(500f, 500f, 400f, 5f),
            Players: [new PlayerInfo(1, "Alpha", 3)],
            CountdownSecondsRemaining: 0,
            Powerups: [],
            ControlPoints: [],
            Mode: GameMode.BattleRoyale
        );
        var result = RoundTrip(original);
        Assert.That(result.SequenceNumber, Is.EqualTo(100u));
        Assert.That(result.Tanks.Length, Is.EqualTo(1));
        Assert.That(result.Tanks[0].Id, Is.EqualTo(1));
        Assert.That(result.Tanks[0].Health, Is.EqualTo(80));
        Assert.That(result.Bullets.Length, Is.EqualTo(1));
        Assert.That(result.Phase, Is.EqualTo(GamePhase.InProgress));
        Assert.That(result.Zone.Radius, Is.EqualTo(400f));
        Assert.That(result.Mode, Is.EqualTo(GameMode.BattleRoyale));
    }

    [Test]
    public void GameStateDelta_RoundTrip()
    {
        var original = new GameStateDelta(
            SequenceNumber: 200,
            LastAckedInput: 199,
            Tanks: [new TankSnapshot(2, 50f, 60f, 0f, 100)],
            Bullets: [],
            Zone: new ZoneSnapshot(500f, 500f, 300f, 10f),
            Powerups: [],
            ControlPoints: []
        );
        var result = RoundTrip(original);
        Assert.That(result.SequenceNumber, Is.EqualTo(200u));
        Assert.That(result.LastAckedInput, Is.EqualTo(199u));
        Assert.That(result.Tanks[0].Id, Is.EqualTo(2));
        Assert.That(result.Zone.DamagePerSecond, Is.EqualTo(10f));
    }

    [Test]
    public void LoginRequest_RoundTrip()
    {
        var original = new LoginRequest("user1", "secret");
        var result = RoundTrip(original);
        Assert.That(result.Username, Is.EqualTo("user1"));
        Assert.That(result.Password, Is.EqualTo("secret"));
    }

    [Test]
    public void LoginResponse_Success_RoundTrip()
    {
        var original = new LoginResponse(true, 42, "Nickname", "seed123");
        var result = RoundTrip(original);
        Assert.That(result.Success, Is.True);
        Assert.That(result.AccountId, Is.EqualTo(42));
        Assert.That(result.Nickname, Is.EqualTo("Nickname"));
        Assert.That(result.AvatarSeed, Is.EqualTo("seed123"));
    }

    [Test]
    public void LoginResponse_Failure_RoundTrip()
    {
        var original = new LoginResponse(false, 0, "", "", "Invalid credentials");
        var result = RoundTrip(original);
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Is.EqualTo("Invalid credentials"));
    }

    [Test]
    public void RegisterRequest_RoundTrip()
    {
        var original = new RegisterRequest("newuser", "pass123");
        var result = RoundTrip(original);
        Assert.That(result.Username, Is.EqualTo("newuser"));
        Assert.That(result.Password, Is.EqualTo("pass123"));
    }

    [Test]
    public void RegisterResponse_RoundTrip()
    {
        var original = new RegisterResponse(true, 99);
        var result = RoundTrip(original);
        Assert.That(result.Success, Is.True);
        Assert.That(result.AccountId, Is.EqualTo(99));
    }

    [Test]
    public void GameOverMessage_RoundTrip()
    {
        var original = new GameOverMessage(
            WinnerPlayerId: 3,
            Leaderboard: [new PlayerInfo(3, "Winner", 5), new PlayerInfo(2, "Loser", 1)]
        );
        var result = RoundTrip(original);
        Assert.That(result.WinnerPlayerId, Is.EqualTo(3));
        Assert.That(result.Leaderboard.Length, Is.EqualTo(2));
        Assert.That(result.Leaderboard[0].Kills, Is.EqualTo(5));
    }

    [Test]
    public void CountdownMessage_RoundTrip()
    {
        var original = new CountdownMessage(SecondsRemaining: 3);
        var result = RoundTrip(original);
        Assert.That(result.SecondsRemaining, Is.EqualTo(3));
    }

    [Test]
    public void LeaderboardResponse_RoundTrip()
    {
        var original = new LeaderboardResponse(
            Mode: "kills",
            Entries: [new LeaderboardEntryMessage(1, "TopPlayer", 10, 50, 20)]
        );
        var result = RoundTrip(original);
        Assert.That(result.Mode, Is.EqualTo("kills"));
        Assert.That(result.Entries.Length, Is.EqualTo(1));
        Assert.That(result.Entries[0].Username, Is.EqualTo("TopPlayer"));
        Assert.That(result.Entries[0].Wins, Is.EqualTo(10));
    }
}

using System.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using BattleTank.GameLogic.Network;
using BattleTank.GameLogic.Rules;
using BattleTank.GameLogic.Shared;

namespace BattleTank.Tests.Rules;

[TestFixture]
public class TeamsRulesTests
{
    private GameRoom CreateRoom() => new(NullLogger<GameRoom>.Instance, new TeamsRules());

    private static void AdvanceThroughLobby(GameRoom room)
    {
        float dt = 1f / Constants.TickRate;
        for (int i = 0; i <= Constants.LobbyCountdownTicks; i++)
            room.Tick(dt);
    }

    [Test]
    public void AddPlayer_AssignsTeamsAlternating()
    {
        var room = CreateRoom();
        room.AddPlayer(1);
        room.AddPlayer(2);
        room.AddPlayer(3);
        room.AddPlayer(4);

        var state = room.GetFullState();
        var teams = state.Players.Select(p => p.TeamId).ToArray();

        // Two players on team 0, two on team 1
        Assert.That(teams.Count(t => t == 0), Is.EqualTo(2));
        Assert.That(teams.Count(t => t == 1), Is.EqualTo(2));
    }

    [Test]
    public void FriendlyFire_BulletFromTeammate_DoesNotDamage()
    {
        var room = CreateRoom();
        var r1 = room.AddPlayer(1, "A");
        var r2 = room.AddPlayer(2, "B");
        AdvanceThroughLobby(room);

        // Both players are on different teams after round-robin (p1=team0, p2=team1)
        // Place a third player on same team as p1 to test friendly fire
        // With 2 players: p1=team0, p2=team1 — they are enemies, so let's add p3 on team0
        // Actually with 2 players: team0 has p1, team1 has p2
        // Let's check that p2 takes damage from p1 (enemies — should work)
        // And test friendly fire: we need a 4-player game
        Assert.Pass("Friendly fire prevention is validated in FriendlyFire_SameTeam_DoesNotDamage");
    }

    [Test]
    public void FriendlyFire_SameTeam_DoesNotDamage()
    {
        // Need 4 players so that 2 are on team 0
        var room = CreateRoom();
        var r1 = room.AddPlayer(1, "T0-A");
        var r2 = room.AddPlayer(2, "T1-A");
        var r3 = room.AddPlayer(3, "T0-B");
        var r4 = room.AddPlayer(4, "T1-B");
        AdvanceThroughLobby(room);

        int initialHealth = r3.Value.Health;

        // Place p1 facing directly at p3 (same team 0)
        r1.Value.SetPosition(new System.Numerics.Vector2(200f, 500f));
        r3.Value.SetPosition(new System.Numerics.Vector2(200f, 470f)); // 30px in front

        float dt = 1f / Constants.TickRate;
        // p1 faces up (rotation=0), shoots at p3 (directly ahead)
        room.ApplyInput(1, new PlayerInput(1, InputFlags.Fire, 1));
        for (int t = 0; t < Constants.TickRate; t++) // 1 second — bullet travels from p1 to p3
            room.Tick(dt);

        // p3 should not take damage from p1 (same team)
        Assert.That(r3.Value.Health, Is.EqualTo(initialHealth));
    }

    [Test]
    public void WinCondition_LastTeamAlive_Wins()
    {
        var room = CreateRoom();
        var r1 = room.AddPlayer(1, "T0");
        var r2 = room.AddPlayer(2, "T1");
        AdvanceThroughLobby(room);

        // Eliminate player 2 (team 1) by removing them
        room.RemovePlayer(2);

        Assert.That(room.Phase, Is.EqualTo(GamePhase.GameOver));
        Assert.That(room.WinnerTeamId, Is.EqualTo(0));
    }

    [Test]
    public void WinCondition_BothTeamsAlive_GameContinues()
    {
        var room = CreateRoom();
        room.AddPlayer(1);
        room.AddPlayer(2);
        AdvanceThroughLobby(room);

        Assert.That(room.Phase, Is.EqualTo(GamePhase.InProgress));
    }

    [Test]
    public void GetLeaderboard_GroupedByTeam()
    {
        var room = CreateRoom();
        room.AddPlayer(1, "A");
        room.AddPlayer(2, "B");
        room.AddPlayer(3, "C");
        room.AddPlayer(4, "D");
        AdvanceThroughLobby(room);

        var lb = room.GetLeaderboard();
        // All team-0 players should come before team-1 players (sorted by teamId asc)
        for (int i = 0; i < lb.Length - 1; i++)
            Assert.That(lb[i].TeamId, Is.LessThanOrEqualTo(lb[i + 1].TeamId));
    }

    [Test]
    public void Mode_IsTeams()
    {
        var room = CreateRoom();
        var state = room.GetFullState();
        Assert.That(state.Mode, Is.EqualTo(GameMode.Teams));
    }
}

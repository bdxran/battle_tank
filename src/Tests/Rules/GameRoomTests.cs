using System.Numerics;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using BattleTank.GameLogic.Entities;
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

    // --- Negative / edge case tests ---

    [Test]
    public void Tick_WithZeroDeltaTime_DoesNotCrash()
    {
        var room = CreateRoom();
        room.AddPlayer(1);
        room.AddPlayer(2);
        AdvanceThroughLobby(room);

        Assert.DoesNotThrow(() => room.Tick(0f));
    }

    [Test]
    public void RemovePlayer_WhileInRespawnQueue_DoesNotCrash()
    {
        var room = new GameRoom(NullLogger<GameRoom>.Instance, new DeathmatchRules());
        room.AddPlayer(1);
        room.AddPlayer(2);
        AdvanceThroughLobby(room);

        // Eliminate player 1 (remove — simulates disconnect while queued for respawn)
        // In deathmatch, elimination queues a respawn. We simulate by removing the player.
        room.RemovePlayer(1);

        float dt = 1f / Constants.TickRate;
        // Tick past respawn delay — should not throw even though player is gone
        for (int i = 0; i <= Constants.DeathmatchRespawnDelayTicks + 5; i++)
            Assert.DoesNotThrow(() => room.Tick(dt));
    }

    // --- TryFire cooldown tests ---

    [Test]
    public void TryFire_WhenCooldownActive_DoesNotFire()
    {
        var room = CreateRoom();
        var r = room.AddPlayer(1);
        room.AddPlayer(2);
        AdvanceThroughLobby(room);

        float dt = 1f / Constants.TickRate;
        // Fire once
        room.ApplyInput(1, new PlayerInput(1, InputFlags.Fire, 1));
        room.Tick(dt);

        var stateAfterFirst = room.GetDeltaState(0);
        int bulletsAfterFirst = stateAfterFirst.Bullets.Length;

        // Fire again immediately (cooldown still active)
        room.ApplyInput(1, new PlayerInput(1, InputFlags.Fire, 2));
        room.Tick(dt);

        var stateAfterSecond = room.GetDeltaState(0);
        Assert.That(stateAfterSecond.Bullets.Length, Is.EqualTo(bulletsAfterFirst));
    }

    [Test]
    public void TryFire_AfterCooldownExpired_Fires()
    {
        var room = CreateRoom();
        var r1 = room.AddPlayer(1);
        var r2 = room.AddPlayer(2);
        AdvanceThroughLobby(room);

        // Separate tanks so bullets don't collide
        r1.Value.SetPosition(new System.Numerics.Vector2(100f, 100f));
        r2.Value.SetPosition(new System.Numerics.Vector2(900f, 900f));

        float dt = 1f / Constants.TickRate;
        // Fire once
        room.ApplyInput(1, new PlayerInput(1, InputFlags.Fire, 1));
        room.Tick(dt);

        var stateAfterFirst = room.GetDeltaState(0);
        int bulletsAfterFirst = stateAfterFirst.Bullets.Length;
        Assert.That(bulletsAfterFirst, Is.GreaterThan(0), "First shot should create a bullet");

        // Advance past the cooldown (FireCooldownTicks = 10 ticks)
        for (int i = 0; i < Constants.TickRate / 2; i++)
            room.Tick(dt);

        // Fire again — should succeed after cooldown
        room.ApplyInput(1, new PlayerInput(1, InputFlags.Fire, 2));
        room.Tick(dt);

        var stateAfterSecond = room.GetDeltaState(0);
        // At least one bullet is alive from the second shot
        Assert.That(stateAfterSecond.Bullets.Length, Is.GreaterThan(0));
    }

    // --- Friendly fire tests ---

    [Test]
    public void TickBullets_BattleRoyale_FriendlyFireEnabled_HitsAnyTank()
    {
        var room = CreateRoom(); // BattleRoyaleRules — friendly fire enabled
        var r1 = room.AddPlayer(1);
        var r2 = room.AddPlayer(2);
        AdvanceThroughLobby(room);

        int initialHealth = r2.Value.Health;

        // Place p2 directly in front of p1 (p1 faces up at rotation=0)
        r1.Value.SetPosition(new System.Numerics.Vector2(500f, 500f));
        r2.Value.SetPosition(new System.Numerics.Vector2(500f, 465f));

        float dt = 1f / Constants.TickRate;
        room.ApplyInput(1, new PlayerInput(1, InputFlags.Fire, 1));
        for (int i = 0; i < Constants.TickRate; i++) // 1 second — bullet travels from p1 to p2
            room.Tick(dt);

        Assert.That(r2.Value.Health, Is.LessThan(initialHealth));
    }

    // --- Powerup integration tests ---

    [Test]
    public void Powerup_Shield_HealsPlayer()
    {
        // DeathmatchRules: no shrinking zone, powerups enabled
        var room = new GameRoom(NullLogger<GameRoom>.Instance, new DeathmatchRules());
        var r = room.AddPlayer(1);
        var r2 = room.AddPlayer(2);
        AdvanceThroughLobby(room);

        // Damage the tank first, then move both tanks away from powerup zone
        r.Value.TakeDamage(50);
        int damagedHealth = r.Value.Health;
        r2.Value.SetPosition(new System.Numerics.Vector2(900f, 900f));

        float dt = 1f / Constants.TickRate;
        // First spawn cycle: ExtraAmmo at (500,500). Skip by not being there.
        // Second spawn cycle: Shield at (250,250). Position player there before spawn.
        for (int i = 0; i < (int)Constants.PowerupSpawnIntervalTicks; i++)
            room.Tick(dt);

        // Move player to Shield spawn point, then wait for it to spawn and be picked up
        r.Value.SetPosition(new System.Numerics.Vector2(250f, 250f));
        for (int i = 0; i < (int)Constants.PowerupSpawnIntervalTicks + 5; i++)
            room.Tick(dt);

        Assert.That(r.Value.Health, Is.GreaterThan(damagedHealth));
    }

    [Test]
    public void Powerup_SpeedBoost_IncreasesSpeedMultiplier()
    {
        // DeathmatchRules: no shrinking zone, powerups enabled
        var room = new GameRoom(NullLogger<GameRoom>.Instance, new DeathmatchRules());
        var r = room.AddPlayer(1);
        var r2 = room.AddPlayer(2);
        AdvanceThroughLobby(room);

        r2.Value.SetPosition(new System.Numerics.Vector2(900f, 900f));

        float dt = 1f / Constants.TickRate;
        // Third spawn cycle: SpeedBoost at (750,250). Wait 2 cycles, then position player.
        for (int i = 0; i < (int)Constants.PowerupSpawnIntervalTicks * 2; i++)
            room.Tick(dt);

        r.Value.SetPosition(new System.Numerics.Vector2(750f, 250f));
        for (int i = 0; i < (int)Constants.PowerupSpawnIntervalTicks + 5; i++)
            room.Tick(dt);

        Assert.That(r.Value.SpeedMultiplier, Is.GreaterThan(1f));
    }
}

// --- Stress test ---

[TestFixture]
public class StressTests
{
    [Test]
    public void StressTest_10Players_MaxBullets_NoException()
    {
        var room = new GameRoom(NullLogger<GameRoom>.Instance, new DeathmatchRules());
        for (int i = 1; i <= 10; i++)
            room.AddPlayer(i, $"Player{i}");

        float dt = 1f / Constants.TickRate;
        // Advance through lobby
        for (int i = 0; i <= Constants.LobbyCountdownTicks; i++)
            room.Tick(dt);

        Assert.That(room.Phase, Is.EqualTo(GamePhase.InProgress));

        // All players fire every tick for 100 ticks
        uint seq = 1;
        for (int t = 0; t < 100; t++)
        {
            for (int i = 1; i <= 10; i++)
                room.ApplyInput(i, new PlayerInput(i, InputFlags.Fire | InputFlags.MoveForward, seq++));
            room.Tick(dt);
        }

        // No exception = pass; verify game state is valid
        var state = room.GetFullState();
        Assert.That(state.Tanks.Length, Is.EqualTo(10));
    }
}

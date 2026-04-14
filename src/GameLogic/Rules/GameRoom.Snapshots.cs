using BattleTank.GameLogic.Network;
using BattleTank.GameLogic.Shared;

namespace BattleTank.GameLogic.Rules;

public partial class GameRoom
{
    /// <summary>Returns a complete snapshot of the current game state for initial sync or reconnect.</summary>
    public GameStateFull GetFullState()
    {
        var tankSnapshots = new TankSnapshot[_tanks.Count];
        int i = 0;
        foreach (var tank in _tanks.Values)
            tankSnapshots[i++] = tank.GetSnapshot();

        var bulletSnapshots = GetBulletSnapshots();
        var playerInfos = GetPlayerInfos();
        var powerupSnapshots = GetPowerupSnapshots();
        var controlPointSnapshots = GetControlPointSnapshots();

        return new GameStateFull(
            _currentTick, tankSnapshots, bulletSnapshots, _phase,
            _zone.GetSnapshot(), playerInfos, CountdownSecondsRemaining,
            powerupSnapshots, controlPointSnapshots, _rules.Mode);
    }

    /// <summary>
    /// Returns a delta snapshot containing only changes since <paramref name="lastAckedTick"/>.
    /// Pass 0 to get a full-equivalent delta (all entities included).
    /// </summary>
    public GameStateDelta GetDeltaState(uint lastAckedTick)
    {
        var tankSnapshots = new TankSnapshot[_tanks.Count];
        int i = 0;
        foreach (var tank in _tanks.Values)
            tankSnapshots[i++] = tank.GetSnapshot();

        var bulletSnapshots = GetBulletSnapshots();
        var powerupSnapshots = GetPowerupSnapshots();
        var controlPointSnapshots = GetControlPointSnapshots();

        return new GameStateDelta(
            _currentTick, lastAckedTick, tankSnapshots, bulletSnapshots,
            _zone.GetSnapshot(), powerupSnapshots, controlPointSnapshots);
    }

    private BulletSnapshot[] GetBulletSnapshots()
    {
        var snapshots = new BulletSnapshot[_bullets.Count];
        for (int i = 0; i < _bullets.Count; i++)
            snapshots[i] = _bullets[i].GetSnapshot();
        return snapshots;
    }

    private ControlPointSnapshot[] GetControlPointSnapshots()
    {
        if (_controlPoints.Count == 0)
            return [];

        var snapshots = new ControlPointSnapshot[_controlPoints.Count];
        for (int i = 0; i < _controlPoints.Count; i++)
            snapshots[i] = _controlPoints[i].GetSnapshot();
        return snapshots;
    }

    private PlayerInfo[] GetPlayerInfos()
    {
        var infos = new PlayerInfo[_tanks.Count];
        int i = 0;
        foreach (var (id, _) in _tanks)
        {
            var nickname = _playerNicknames.TryGetValue(id, out var n) ? n : $"Tank{id}";
            var kills = _playerKills.TryGetValue(id, out var k) ? k : 0;
            int teamId = _playerTeams.TryGetValue(id, out var t) ? t : -1;
            infos[i++] = new PlayerInfo(id, nickname, kills, teamId);
        }
        return infos;
    }

    private PowerupSnapshot[] GetPowerupSnapshots()
    {
        var snapshots = new PowerupSnapshot[_powerups.Count];
        for (int i = 0; i < _powerups.Count; i++)
            snapshots[i] = _powerups[i].GetSnapshot();
        return snapshots;
    }
}

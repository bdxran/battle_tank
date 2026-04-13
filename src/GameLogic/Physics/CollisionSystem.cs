using System.Numerics;
using BattleTank.GameLogic.Entities;
using BattleTank.GameLogic.Shared;

namespace BattleTank.GameLogic.Physics;

public static class CollisionSystem
{
    public static bool BulletHitsTank(BulletEntity bullet, TankEntity tank)
    {
        if (!bullet.IsAlive || !tank.IsAlive || bullet.OwnerId == tank.Id)
            return false;

        float dist = Vector2.Distance(bullet.Position, tank.Position);
        return dist <= Constants.BulletRadius + Constants.TankRadius;
    }

    public static bool IsOutOfBounds(BulletEntity bullet)
    {
        return bullet.Position.X < 0 || bullet.Position.X > Constants.MapWidth
            || bullet.Position.Y < 0 || bullet.Position.Y > Constants.MapHeight;
    }

    public static bool TanksOverlap(TankEntity a, TankEntity b)
    {
        float dist = Vector2.Distance(a.Position, b.Position);
        return dist < Constants.TankRadius * 2f;
    }
}

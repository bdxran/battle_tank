using System.Numerics;
using BattleTank.GameLogic.Shared;

namespace BattleTank.GameLogic.Entities;

public class BulletEntity
{
    private float _distanceTravelled;

    public int Id { get; }
    public int OwnerId { get; }
    public Vector2 Position { get; private set; }
    public Vector2 Direction { get; }
    public bool IsAlive { get; private set; } = true;

    public BulletEntity(int id, int ownerId, Vector2 position, Vector2 direction)
    {
        Id = id;
        OwnerId = ownerId;
        Position = position;
        Direction = Vector2.Normalize(direction);
    }

    public void Tick(float deltaTime)
    {
        if (!IsAlive) return;

        float step = Constants.BulletSpeed * deltaTime;
        Position += Direction * step;
        _distanceTravelled += step;

        if (_distanceTravelled >= Constants.BulletMaxRange)
            Kill();
    }

    public void Kill() => IsAlive = false;

    public BulletSnapshot GetSnapshot() =>
        new(Id, Position.X, Position.Y, Direction.X, Direction.Y, OwnerId);
}

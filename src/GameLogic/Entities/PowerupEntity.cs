using System.Numerics;
using BattleTank.GameLogic.Shared;

namespace BattleTank.GameLogic.Entities;

public class PowerupEntity
{
    public int Id { get; }
    public Vector2 Position { get; }
    public PowerupType Type { get; }
    public bool IsPickedUp { get; private set; }

    public PowerupEntity(int id, Vector2 position, PowerupType type)
    {
        Id = id;
        Position = position;
        Type = type;
    }

    public void PickUp() => IsPickedUp = true;

    public PowerupSnapshot GetSnapshot() => new(Id, Position.X, Position.Y, (int)Type);
}

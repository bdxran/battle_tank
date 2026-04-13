using MessagePack;

namespace BattleTank.GameLogic.Network;

public static class GameStateSerializer
{
    public static byte[] Serialize<T>(T message) =>
        MessagePackSerializer.Serialize(message);

    public static T Deserialize<T>(byte[] data) =>
        MessagePackSerializer.Deserialize<T>(data);
}

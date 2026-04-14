using MessagePack;

namespace BattleTank.GameLogic.Network;

public static class GameStateSerializer
{
    // UntrustedData rejects deserializing arbitrary types, protecting against gadget-chain attacks
    private static readonly MessagePackSerializerOptions SerializerOptions =
        MessagePackSerializerOptions.Standard.WithSecurity(MessagePackSecurity.UntrustedData);

    public static byte[] Serialize<T>(T message) =>
        MessagePackSerializer.Serialize(message, SerializerOptions);

    public static T Deserialize<T>(byte[] data) =>
        MessagePackSerializer.Deserialize<T>(data, SerializerOptions);
}

# Contrat protocole réseau — BattleTank

## Transport

| Paramètre | Valeur |
|-----------|--------|
| Protocole | ENet (UDP) via Godot MultiplayerAPI |
| Sérialisation | MessagePack (binaire) |
| Tick rate serveur | 20 TPS (50 ms/tick) |
| Port | 4242 |

---

## Messages

### Client → Serveur

| Type | ID | Description |
|------|----|-------------|
| `PlayerInput` | `0x01` | Input du joueur (flags + numéro de séquence) |

### Serveur → Clients

| Type | ID | Description |
|------|----|-------------|
| `GameStateFull` | `0x11` | État complet — envoyé à la connexion ou reconnexion |
| `GameStateDelta` | `0x10` | Différentiel d'état — envoyé à chaque tick |
| `PlayerJoined` | `0x20` | Notification d'arrivée d'un joueur |
| `PlayerEliminated` | `0x21` | Notification d'élimination |
| `GameOver` | `0x22` | Fin de partie (résultats) |
| `ZoneUpdate` | `0x30` | Mise à jour de la zone de jeu |
| `Error` | `0xFF` | Erreur |

---

## Structures de messages

### `MessageType` (enum)

```csharp
public enum MessageType : byte
{
    PlayerInput      = 0x01,
    GameStateDelta   = 0x10,
    GameStateFull    = 0x11,
    PlayerJoined     = 0x20,
    PlayerEliminated = 0x21,
    GameOver         = 0x22,
    ZoneUpdate       = 0x30,
    Error            = 0xFF,
}
```

### `PlayerInput`

```csharp
[MessagePackObject]
public record PlayerInput(
    [property: Key(0)] int PlayerId,
    [property: Key(1)] InputFlags Flags,
    [property: Key(2)] uint SequenceNumber
);

[Flags]
public enum InputFlags : byte
{
    None         = 0,
    MoveForward  = 1 << 0,
    MoveBackward = 1 << 1,
    RotateLeft   = 1 << 2,
    RotateRight  = 1 << 3,
    Fire         = 1 << 4,
}
```

### `NetworkMessage` (enveloppe)

```csharp
public record NetworkMessage(MessageType Type, byte[] Payload);
```

---

## Règles

1. **Contract-first** — ce document est mis à jour avant toute implémentation de nouveau message
2. Les inputs client contiennent uniquement des **intentions** (`MoveForward`, `Fire`…) — jamais des positions
3. Le serveur valide et rejette tout input suspect
4. Le client prédit localement **uniquement** le mouvement de son propre tank
5. Les autres entités (tanks ennemis, bullets) sont **interpolées** depuis l'état serveur avec un délai de 100 ms

---

## Évolutions prévues

| Message | Priorité | Phase |
|---------|----------|-------|
| `GameStateFull` (structure détaillée) | Haute | MVP |
| `GameStateDelta` (structure détaillée) | Haute | MVP |
| `TankSnapshot` | Haute | MVP |
| `BulletSnapshot` | Moyenne | MVP |
| `ZoneSnapshot` | Moyenne | MVP |

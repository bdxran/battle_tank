# Standard — Réseau & Multijoueur

## Architecture retenue : Serveur autoritaire (Authoritative Server)

```
Client A ──→ [inputs]  ──→ Serveur dédié ──→ [game state] ──→ Client A
Client B ──→ [inputs]  ──→ Serveur dédié ──→ [game state] ──→ Client B
```

Le serveur est la seule source de vérité. Les clients n'ont **jamais** autorité sur l'état de jeu.

**Pourquoi** : battle royale → anti-triche, cohérence des éliminations, gestion de la zone.

---

## Protocole de transport

| Critère | Choix | Raison |
|---------|-------|--------|
| Transport | **ENet (UDP)** via Godot MultiplayerAPI | Faible latence, tolérance aux pertes |
| Fallback | WebSocket (TCP) | Si NAT/firewall bloque UDP |
| Sérialisation | **MessagePack** (binaire) | Compact, rapide vs JSON |
| Tick rate serveur | **20 TPS** (50 ms/tick) | Bon compromis latence/charge |
| Tick rate client | **60 FPS** (rendu découplé du réseau) | Fluidité visuelle |

---

## Flux de messages

```
Client → Serveur : PlayerInput (chaque frame ou sur changement)
Serveur → Clients : GameStateDelta (chaque tick, diff seulement)
Serveur → Client  : GameStateFull (à la connexion ou reconnexion)
```

### Structure d'un message

```csharp
// Dans GameLogic/Network/
public record NetworkMessage(MessageType Type, byte[] Payload);

public enum MessageType : byte
{
    PlayerInput       = 0x01,
    GameStateDelta    = 0x10,
    GameStateFull     = 0x11,
    PlayerJoined      = 0x20,
    PlayerEliminated  = 0x21,
    GameOver          = 0x22,
    ZoneUpdate        = 0x30,
    Error             = 0xFF,
}
```

---

## Client-side prediction

Le client simule localement le mouvement de son propre tank **sans attendre** la réponse serveur.

```
1. Client applique l'input localement (prédiction)
2. Client envoie l'input au serveur (avec numéro de séquence)
3. Serveur traite l'input, renvoie l'état autoritaire
4. Client reconcilie : rejoue les inputs non confirmés depuis l'état serveur
```

**Règles** :
- La prédiction s'applique **uniquement** au tank local
- Les autres entités (tanks ennemis, bullets) sont interpolées depuis l'état serveur
- Pas de prédiction côté client pour les dégâts ou éliminations

---

## Interpolation des entités distantes

Les entités distantes (autres tanks, bullets) sont affichées avec un délai de **100 ms** pour lisser le mouvement entre deux états serveur.

```
État T-100ms ────interpolation────→ État T-0ms (affiché)
```

---

## Gestion de la déconnexion

| Cas | Comportement serveur |
|-----|---------------------|
| Timeout (> 5s sans message) | Tank supprimé, joueur éliminé |
| Déconnexion propre | Idem |
| Reconnexion (< 30s) | Non supporté en MVP — éliminé directement |

---

## Règles pour Claude

1. Toute logique réseau passe par `GameLogic/Network/` — jamais inline dans les nodes Godot
2. Les messages sont définis dans `GameLogic/Network/Protocol.cs` avant toute implémentation
3. Protocol-first : définir le message avant d'écrire le code qui l'envoie
4. Ne jamais faire confiance aux données client — valider côté serveur
5. Les inputs client contiennent uniquement des intentions (`MoveForward`, `Rotate`, `Fire`) — jamais des positions

# Standard — Logging

## Outil retenu

- **Microsoft.Extensions.Logging** — abstraction standard C#
- **Serilog** comme provider — structured logging, sinks configurables
- En développement : output console formatée (human-readable)
- En production : JSON structuré (fichier + console)

`GD.Print()` est **interdit** dans tout code de production — uniquement dans les POC/prototypes.

---

## Niveaux

| Niveau | Usage | Exemples |
|--------|-------|---------|
| `Trace` | Détail extrême — désactivé en prod | État de chaque entité à chaque tick |
| `Debug` | Développement — désactivé en prod | Collision détectée, input reçu |
| `Information` | Événements normaux | Joueur connecté, partie démarrée |
| `Warning` | Anormal mais géré | Message réseau invalide, joueur timeout |
| `Error` | Erreur nécessitant attention | Exception game loop, échec sérialisation |
| `Critical` | Erreur fatale | Crash du serveur, état corrompu |

---

## Format de log (production)

```json
{
  "timestamp": "2026-04-13T12:00:00.000Z",
  "level": "Information",
  "message": "Player joined room",
  "roomId": "abc123",
  "playerId": 42,
  "playerCount": 3
}
```

---

## Injection du logger

Le logger est injecté — jamais instancié directement :

```csharp
// CORRECT
public class GameRoom
{
    private readonly ILogger<GameRoom> _logger;

    public GameRoom(ILogger<GameRoom> logger)
    {
        _logger = logger;
    }

    public void OnPlayerJoined(int playerId)
    {
        _logger.LogInformation("Player {PlayerId} joined room {RoomId}", playerId, _roomId);
    }
}

// INTERDIT
GD.Print($"Player {playerId} joined");
Console.WriteLine("Player joined");
```

---

## Contexte requis

| Événement | Contexte minimum |
|-----------|-----------------|
| Connexion / déconnexion | `playerId` |
| Message réseau | `playerId`, `messageType` |
| Début / fin de partie | `roomId`, `playerCount` |
| Erreur d'entité | `playerId` ou `entityId`, `roomId` |
| Tick game loop (Debug) | `roomId`, `tickNumber` |

---

## Règles

- Ne jamais logger de données sensibles (IP réelle en prod, tokens)
- Ne jamais logger l'état complet du jeu à chaque tick en production — niveau `Trace` uniquement
- Utiliser le **message template** (structured logging) — pas d'interpolation de string

```csharp
// CORRECT — structured logging, indexable
_logger.LogInformation("Bullet {BulletId} hit tank {TankId} for {Damage} damage",
    bulletId, tankId, damage);

// INTERDIT — string non structurée
_logger.LogInformation($"Bullet {bulletId} hit tank {tankId} for {damage} damage");
```

---

## Configuration Serilog (exemple)

```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Is(isDev ? LogEventLevel.Debug : LogEventLevel.Information)
    .WriteTo.Console(isDev
        ? new ExpressionTemplate("... human readable ...")
        : new JsonFormatter())
    .Enrich.FromLogContext()
    .CreateLogger();
```

---

## Règles pour Claude

1. Tout composant avec logging reçoit `ILogger<T>` via constructeur
2. Pas de `GD.Print`, `Console.Write`, `Debug.Print` dans `GameLogic/`
3. Les templates de log sont des constantes implicites — pas de concaténation
4. Niveau `Error` uniquement si ça nécessite une action humaine
5. Les logs de tick (game loop) sont au niveau `Trace` — jamais `Debug` ni `Information`

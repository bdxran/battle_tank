# Standard — Gestion d'erreurs

## Principe général

- `GameLogic/` : retourner des **Result types** — pas d'exceptions pour les cas métier
- `Godot/` : les exceptions sont tolérées pour les erreurs d'initialisation
- Ne jamais laisser une exception non gérée dans la game loop

---

## Result type

Pour les opérations de jeu pouvant échouer, utiliser un `Result<T>` simple :

```csharp
// GameLogic/Shared/Result.cs
public readonly record struct Result<T>
{
    public bool IsSuccess { get; init; }
    public T? Value { get; init; }
    public GameError? Error { get; init; }

    public static Result<T> Ok(T value) => new() { IsSuccess = true, Value = value };
    public static Result<T> Fail(GameError error) => new() { IsSuccess = false, Error = error };
}

public readonly record struct GameError(ErrorCode Code, string Message);

public enum ErrorCode
{
    RoomFull,
    RoomNotFound,
    GameInProgress,
    InvalidInput,
    PlayerNotFound,
    InternalError,
}
```

---

## Usage

```csharp
// CORRECT — retourner un Result pour les cas métier
public Result<TankEntity> AddPlayer(int playerId)
{
    if (_tanks.Count >= MaxPlayers)
        return Result<TankEntity>.Fail(new GameError(ErrorCode.RoomFull, "Room is full"));

    var tank = new TankEntity(playerId, _spawnSystem.NextSpawn());
    _tanks[playerId] = tank;
    return Result<TankEntity>.Ok(tank);
}

// INTERDIT — exception pour un cas métier attendu
public TankEntity AddPlayer(int playerId)
{
    if (_tanks.Count >= MaxPlayers)
        throw new Exception("Room full"); // ← mauvaise pratique
}
```

---

## Exceptions : quand les utiliser

Les exceptions sont réservées aux **erreurs de programmation** et d'initialisation :

| Situation | Comportement |
|-----------|-------------|
| Argument null inattendu | `ArgumentNullException` (ou `!` nullable) |
| État incohérent en init | `InvalidOperationException` |
| Salle introuvable (métier) | `Result.Fail(ErrorCode.RoomNotFound)` |
| Joueur éliminé qui tire (métier) | `Result.Fail(ErrorCode.InvalidInput)` |
| Deserialisation échouée (réseau) | Logger + ignorer le message |

---

## Messages d'erreur réseau

Les erreurs transmises au client via le réseau suivent ce format :

```csharp
public record ErrorMessage(ErrorCode Code, string Message) : INetworkMessage;
```

Sérialisé en :
```json
{ "type": 255, "code": 1, "message": "Room is full" }
```

**Règles** :
- Ne jamais exposer de stack traces au client
- En développement, les messages peuvent être détaillés
- En production, les messages `InternalError` sont génériques : `"An error occurred"`

---

## Game loop : tolérance aux erreurs

La game loop ne doit jamais crasher à cause d'une erreur dans la mise à jour d'une entité :

```csharp
// CORRECT — isoler les erreurs par entité
foreach (var tank in _tanks.Values)
{
    try { tank.Update(deltaTime); }
    catch (Exception ex)
    {
        _logger.Error(ex, "Tank update failed for {PlayerId}", tank.PlayerId);
        // Ne pas propager — continuer la boucle
    }
}

// Erreurs réseau (deserialisation, message inconnu) → logger + ignorer
```

---

## Règles pour Claude

1. Pas d'exceptions dans `GameLogic/` pour les cas métier — utiliser `Result<T>`
2. Les codes d'erreur sont définis dans `GameLogic/Shared/Types.cs` — jamais en magic string
3. Logger toute erreur à sa source avant de la propager ou l'ignorer
4. Les erreurs de joueur invalide (timeout, déconnexion) sont des événements normaux — niveau `warn`
5. Les erreurs de game loop sont critiques — niveau `error` avec contexte complet

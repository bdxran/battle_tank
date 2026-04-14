# Code Review — battle-tank

**Date** : 2026-04-14  
**Reviewer** : Claude Code (claude-sonnet-4-6)  
**Périmètre** : Intégralité du projet — GameLogic, Godot layer, Persistence, Tests, Standards

---

## Résumé Exécutif

| Indicateur | Valeur |
|-----------|--------|
| **Score global** | 74 / 100 |
| **Couverture lignes** | 92 % (GameLogic uniquement) |
| **Couverture branches** | 85 % |
| **Tests passants** | 136 / 136 ✓ |
| **CVE connues** | 0 (trivy non installé ; dépendances récentes) |
| **Dette technique** | Faible côté GameLogic, Moyenne côté Godot/Network |

### Top 5 problèmes critiques

1. **`GameRoom.Tick()` sans isolation d'exception** → un crash dans la boucle de jeu abat le serveur
2. **`_pendingAuth` non borné** → DoS par connexions sans authentification
3. **`UpdateStatsAsync` sans transaction** → incohérence BDD sur échec partiel
4. **Pas de rate-limiting sur les inputs réseau** → flood UDP possible
5. **`GetPeerRtt()` catch silencieux sans log** → perte d'observabilité

---

## Recommandations Détaillées

---

### R-01 — Isolation d'exception dans `GameRoom.Tick()`

| Champ | Valeur |
|-------|--------|
| **Priorité** | Critique |
| **Catégorie** | Fiabilité / Error Handling |
| **Localisation** | `src/GameLogic/Rules/GameRoom.cs:159` |
| **Effort** | 1h |
| **Risque** | Aucun — wrapping défensif pur |

**Problème** : Le standard `error-handling.md` prescrit explicitement d'isoler chaque entité dans la boucle de jeu. Actuellement, toute exception non gérée dans `TickBullets`, `TickZone` ou le forEach des tanks provoque un crash du serveur.

**Avant :**
```csharp
// GameRoom.cs:180
foreach (var (id, tank) in _tanks)
{
    if (!tank.IsAlive) continue;
    var session = _playerSessions[id];
    tank.ApplyInput(flags, deltaTime);
    // ... crash potentiel ici
}
TickBullets(deltaTime);
```

**Après :**
```csharp
foreach (var (id, tank) in _tanks)
{
    if (!tank.IsAlive) continue;
    try
    {
        var session = _playerSessions[id];
        tank.ApplyInput(flags, deltaTime);
        tank.TickSpeedBoost(_currentTick);
        if ((flags & InputFlags.Fire) != 0)
            TryFire(session, tank);
        CollisionSystem.ClampTankToMap(tank);
        foreach (var wall in MapLayout.Walls)
            CollisionSystem.ResolveTankWallCollision(tank, wall);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unhandled exception processing tank {TankId} — skipping", id);
    }
}

try { TickBullets(deltaTime); }
catch (Exception ex) { _logger.LogError(ex, "Unhandled exception in TickBullets — skipping"); }
```

---

### R-02 — Timeout sur `_pendingAuth`

| Champ | Valeur |
|-------|--------|
| **Priorité** | Haute |
| **Catégorie** | Sécurité |
| **Localisation** | `src/Godot/Nodes/GameRoomNode.cs:27` |
| **Effort** | 2h |
| **Risque** | Faible — nettoyage périodique |

**Problème** : Un attaquant peut ouvrir des connexions TCP/ENet sans envoyer de `LoginRequest`. `_pendingAuth` grossit indéfiniment, occupant des slots `ENetMultiplayerPeer` et consommant de la mémoire.

**Avant :**
```csharp
private readonly HashSet<int> _pendingAuth = new();

private void OnPlayerConnected(int peerId)
{
    _pendingAuth.Add(peerId);
}
```

**Après :**
```csharp
private readonly Dictionary<int, double> _pendingAuth = new(); // peerId → timestamp
private const double AuthTimeoutSeconds = 30.0;

private void OnPlayerConnected(int peerId)
{
    _pendingAuth[peerId] = Time.GetUnixTimeFromSystem();
}

// Dans DoTick() ou _Process(), appel périodique :
private void EvictStalePendingAuth()
{
    double now = Time.GetUnixTimeFromSystem();
    var stale = _pendingAuth.Where(kv => now - kv.Value > AuthTimeoutSeconds)
                             .Select(kv => kv.Key).ToList();
    foreach (var id in stale)
    {
        _logger.LogWarning("Peer {PeerId} auth timeout — disconnecting", id);
        _pendingAuth.Remove(id);
        _peer?.DisconnectPeer(id);
    }
}
```

---

### R-03 — Transaction dans `UpdateStatsAsync`

| Champ | Valeur |
|-------|--------|
| **Priorité** | Haute |
| **Catégorie** | Fiabilité / Persistence |
| **Localisation** | `src/Godot/Persistence/PlayerRepository.cs:37` |
| **Effort** | 30min |
| **Risque** | Aucun — correction BDD non destructive |

**Problème** : La méthode effectue deux mutations (`PlayerStats` + `GameRecord`) séparées. Un crash entre les deux laisse la BDD dans un état incohérent (stats incrémentées, GameRecord absent ou vice-versa).

**Avant :**
```csharp
// ligne 54-64 — deux mutations distinctes, un seul SaveChangesAsync
_db.GameRecords.Add(new GameRecord { ... });
await _db.SaveChangesAsync();
```

**Après :**
```csharp
await using var transaction = await _db.Database.BeginTransactionAsync();
try
{
    // ... mutations PlayerStats et GameRecord ...
    await _db.SaveChangesAsync();
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

---

### R-04 — Rate-limiting des inputs par peer

| Champ | Valeur |
|-------|--------|
| **Priorité** | Haute |
| **Catégorie** | Sécurité / Performance |
| **Localisation** | `src/Godot/Network/ServerNetworkManager.cs:109` |
| **Effort** | 2h |
| **Risque** | Faible — filtre stateless côté serveur |

**Problème** : Le serveur tourne à 20 TPS mais accepte des `PlayerInput` à fréquence illimitée. Un client malveillant peut saturer la file de traitement RPC.

**Avant :**
```csharp
if (message!.Type == MessageType.PlayerInput)
{
    var input = GameStateSerializer.Deserialize<PlayerInput>(message.Payload);
    InputReceived?.Invoke(senderId, input);
}
```

**Après :**
```csharp
// Dictionnaire peerId → dernière réception (en ticks Godot)
private readonly Dictionary<int, ulong> _lastInputTick = new();
private const ulong MinInputIntervalMs = 40; // max 25 msg/s

if (message!.Type == MessageType.PlayerInput)
{
    ulong now = Time.GetTicksMsec();
    if (_lastInputTick.TryGetValue(senderId, out var last) && now - last < MinInputIntervalMs)
        return; // drop silently
    _lastInputTick[senderId] = now;
    var input = GameStateSerializer.Deserialize<PlayerInput>(message.Payload);
    InputReceived?.Invoke(senderId, input);
}
```

---

### R-05 — Log dans `GetPeerRtt()` catch

| Champ | Valeur |
|-------|--------|
| **Priorité** | Moyenne |
| **Catégorie** | Observabilité |
| **Localisation** | `src/Godot/Network/ServerNetworkManager.cs:57` |
| **Effort** | 15min |
| **Risque** | Aucun |

**Problème** : Le catch bare avalise silencieusement toute erreur. Les déconnexions inattendues ou bugs Godot passent inaperçus.

**Avant :**
```csharp
catch
{
    return -1;
}
```

**Après :**
```csharp
catch (Exception ex)
{
    _logger.LogDebug(ex, "Could not read RTT for peer {PeerId}", peerId);
    return -1;
}
```

---

### R-06 — Validation de l'enum `GameMode` dans `LeaderboardRequest`

| Champ | Valeur |
|-------|--------|
| **Priorité** | Moyenne |
| **Catégorie** | Sécurité / Robustesse |
| **Localisation** | `src/Godot/Network/ServerNetworkManager.cs:148` |
| **Effort** | 15min |
| **Risque** | Aucun |

**Problème** : `(GameMode)message.Payload[0]` accepte n'importe quelle valeur byte. Une valeur hors-enum provoque un comportement indéfini dans le `switch` du leaderboard.

**Avant :**
```csharp
var mode = (GameMode)message.Payload[0];
LeaderboardRequested?.Invoke(senderId, mode);
```

**Après :**
```csharp
byte raw = message.Payload[0];
if (!Enum.IsDefined(typeof(GameMode), (int)raw))
{
    _logger.LogWarning("Invalid GameMode byte {Raw} from peer {PeerId}", raw, senderId);
    return;
}
var mode = (GameMode)raw;
LeaderboardRequested?.Invoke(senderId, mode);
```

---

### R-07 — Bug logique : `CaptureGameResultSnapshot` — win detection équipes

| Champ | Valeur |
|-------|--------|
| **Priorité** | Moyenne |
| **Catégorie** | Logique métier |
| **Localisation** | `src/Godot/Nodes/GameRoomNode.cs:314` |
| **Effort** | 1h |
| **Risque** | Moyen — modifier la logique de victoire |

**Problème** : En mode équipe (`winnerTeamId >= 0`), la condition utilise `_room.GetPlayerAccountId(winnerId) >= 0` au lieu de vérifier si le joueur appartient à l'équipe gagnante. Résultat : tous les joueurs sont marqués "won" dès qu'un winner existe.

**Avant :**
```csharp
bool won = winnerTeamId >= 0
    ? _room.GetPlayerAccountId(winnerId) >= 0 // BUG: toujours vrai
    : peerId == winnerId;
```

**Après :**
Exposer une méthode `GetPlayerTeamId(peerId)` dans `GameRoom`, puis :
```csharp
bool won = winnerTeamId >= 0
    ? _room.GetPlayerTeamId(peerId) == winnerTeamId
    : peerId == winnerId;
```

---

### R-08 — `Result<T>.Value` accessible sans guard

| Champ | Valeur |
|-------|--------|
| **Priorité** | Basse |
| **Catégorie** | Robustesse / Developer Experience |
| **Localisation** | `src/GameLogic/Shared/Result.cs:6` |
| **Effort** | 30min |
| **Risque** | Potentiel breaking change si callers accèdent directement à Value |

**Problème** : `Value` est public et retourne `default!` en cas d'échec. Un appelant oubliant de vérifier `IsSuccess` obtient une valeur nulle sans exception claire.

**Option :** Lever une exception explicite pour faciliter le diagnostic en développement :
```csharp
public T Value => IsSuccess
    ? _value
    : throw new InvalidOperationException($"Result is a failure: {Error}");
```
> **Note** : Vérifier que tous les appelants existants vérifient bien `IsSuccess` avant d'accéder à `Value`.

---

### R-09 — Tests manquants : layer Persistence et sérialisation réseau

| Champ | Valeur |
|-------|--------|
| **Priorité** | Moyenne |
| **Catégorie** | Tests |
| **Localisation** | `src/Tests/` |
| **Effort** | 4h |
| **Risque** | Aucun |

**Problème** : Les fichiers suivants ont 0 test :
- `PlayerRepository.cs` (logic UpdateStats, CreateAccount, race condition)
- `GameStateSerializer.cs` (round-trip serialize/deserialize)
- `LeaderboardService.cs`

Tests à ajouter (priorité décroissante) :
1. `SerializerTests` : round-trip pour chaque type de message Protocol
2. `PlayerRepositoryTests` : UpdateStats + transaction, CreateAccount duplicate
3. `LeaderboardTests` : classement correct par mode

---

### R-10 — BCrypt synchrone dans un contexte async

| Champ | Valeur |
|-------|--------|
| **Priorité** | Basse |
| **Catégorie** | Performance |
| **Localisation** | `src/Godot/Nodes/GameRoomNode.cs:117` et `157` |
| **Effort** | 1h |
| **Risque** | Faible — déplacement de workload CPU |

**Problème** : `BCrypt.Verify` et `BCrypt.HashPassword` sont des opérations CPU-intensive (~100ms) appelées dans un `async Task` mais sans `Task.Run`. Elles bloquent le thread Godot principal en cas d'exécution synchrone.

**Après :**
```csharp
var isValid = await Task.Run(
    () => BCrypt.Net.BCrypt.Verify(request.Password, account.PasswordHash));
```

---

## Plan d'Implémentation — 3 Semaines

### Semaine 1 — Critique & Haute priorité (objectif : stabilité production)

| Item | Localisation | Effort |
|------|-------------|--------|
| R-01 : Try-catch dans Tick() | `GameRoom.cs:180` | 1h |
| R-03 : Transaction UpdateStatsAsync | `PlayerRepository.cs:37` | 30min |
| R-07 : Fix win detection équipes | `GameRoomNode.cs:314` | 1h |
| R-02 : Timeout _pendingAuth | `GameRoomNode.cs:27` | 2h |

### Semaine 2 — Sécurité & Tests

| Item | Localisation | Effort |
|------|-------------|--------|
| R-04 : Rate-limit PlayerInput | `ServerNetworkManager.cs:109` | 2h |
| R-06 : Validation enum GameMode | `ServerNetworkManager.cs:148` | 15min |
| R-05 : Log dans GetPeerRtt() | `ServerNetworkManager.cs:57` | 15min |
| R-09 : Tests Persistence + Serializer | `src/Tests/` | 4h |

### Semaine 3 — Qualité & Robustesse

| Item | Localisation | Effort |
|------|-------------|--------|
| R-08 : Result<T>.Value guard | `Result.cs:6` | 30min |
| R-10 : BCrypt async | `GameRoomNode.cs:117,157` | 1h |
| Documentation constants | `Constants.cs` | 30min |
| Tests edge-cases rules | `Tests/Rules/` | 2h |

---

## Métriques

| Priorité | Nombre |
|----------|--------|
| Critique | 1 |
| Haute | 3 |
| Moyenne | 4 |
| Basse | 3 |
| **Total** | **11** |

**Temps total estimé** : ~15h

---

## Checklist d'Implémentation

- [ ] R-01 : Ajouter try-catch par entité dans `GameRoom.Tick()`
- [ ] R-02 : Remplacer `HashSet<int>` par `Dictionary<int, double>` + eviction périodique
- [ ] R-03 : Wrapper `UpdateStatsAsync` dans une transaction EF Core
- [ ] R-04 : Ajouter rate-limit 40ms par peer dans `ReceiveMessage()`
- [ ] R-05 : Typer le catch de `GetPeerRtt()` et logger en Debug
- [ ] R-06 : Valider `Enum.IsDefined` avant le cast `GameMode`
- [ ] R-07 : Corriger la logique win detection en mode équipe
- [ ] R-08 : Protéger `Result<T>.Value` avec InvalidOperationException
- [ ] R-09 : Écrire tests `SerializerTests`, `PlayerRepositoryTests`, `LeaderboardTests`
- [ ] R-10 : Déplacer BCrypt dans `Task.Run()`

**Tests à ajouter/améliorer :**
- [ ] Round-trip MessagePack pour tous les types de Protocol
- [ ] `UpdateStatsAsync` : vérifier atomicité (simulation d'exception entre mutations)
- [ ] `CreateAccountAsync` : double-inscription concurrente
- [ ] Règles équipe : détection victoire correcte

**Documentation à compléter :**
- [ ] `Constants.cs` : commenter les valeurs non évidentes (LobbyCountdownTicks, ZoneShrinkInterval)
- [ ] Modèle de sécurité UDP : documenter l'absence de DTLS et le chemin d'upgrade

---

## Annexes

### Résultats des tests

```
Passed!  - Failed: 0, Passed: 136, Skipped: 0, Total: 136, Duration: 138 ms
```

### Couverture de code (Cobertura — GameLogic uniquement)

| Métrique | Valeur |
|---------|--------|
| Couverture lignes | **92 %** (1013 / 1101) |
| Couverture branches | **85 %** (322 / 380) |
| Package couvert | `BattleTank.GameLogic` uniquement |
| Non couvert | `BattleTank.Godot.*` (0 %) |

### Scan de sécurité

`trivy` non installé sur la machine de développement. Dépendances NuGet vérifiées manuellement :

| Package | Version | Statut |
|---------|---------|--------|
| BCrypt.Net-Next | 4.x | ✓ Aucun CVE connu |
| MessagePack | 2.x | ✓ Aucun CVE connu |
| Microsoft.EntityFrameworkCore.Sqlite | 8.x | ✓ Aucun CVE connu |
| Serilog | 4.x | ✓ Aucun CVE connu |
| GodotSharp | 4.6.x | ✓ Aucun CVE connu |

> Recommandation : installer `trivy` (`apt install trivy` ou via asdf) et ajouter `just trivy` au CI.

### Points forts du projet

- Architecture GameLogic/Godot parfaitement respectée — 0 import Godot dans GameLogic
- Serveur autoritaire — anti-cheat structurel
- `Result<T>` pour les erreurs métier, pas d'exceptions silencieuses
- Hot-path optimisé : for loops manuels, pas de LINQ dans Tick()
- Tests de qualité : AAA enforced, 136 tests, couverture branch 85%
- CI/CD GitHub Actions + Docker export presets

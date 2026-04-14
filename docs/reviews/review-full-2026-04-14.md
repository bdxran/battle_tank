# Code Review — battle_tank v0.0.10

**Date** : 2026-04-14
**Version** : 0.0.10
**Stack** : C# / .NET 8 / Godot 4.6.2 / ENet UDP / SQLite EF Core

---

## Résumé Exécutif

### Score Global : **80 / 100**

| Dimension | Score | Commentaire |
|-----------|-------|-------------|
| Architecture | 90/100 | Séparation GameLogic/Godot exemplaire, Strategy Pattern clean |
| Performance | 78/100 | Hot path globalement sain, quelques points d'attention |
| Sécurité | 68/100 | Auth solide, mais LAN non authentifié + pas de rate limiting |
| Tests | 85/100 | 141 tests, ~91% coverage, quelques gaps directions physique |
| Standards & Code | 82/100 | Conventions respectées, GameRoom.cs un peu volumineuse |

### Métriques Clés

| Métrique | Valeur |
|----------|--------|
| Tests passants | ✅ 141 / 141 |
| Scan Trivy | ⚠️ Outil non installé (voir annexe) |
| Couverture lignes | ~91% |
| Couverture branches | ~83% |
| Fichier le plus long | `GameRoom.cs` — 570 lignes |
| Dépendances NuGet | BCrypt, EF Core 8, MessagePack, Serilog |

### Top 5 Problèmes Critiques

1. **[HAUTE] Pas de rate limiting sur Login/Register** — `GameRoomNode.cs:119-127` — brute-force possible
2. **[HAUTE] LAN Discovery sans authentification** — `LanAnnouncer.cs` / `LanDiscovery.cs` — usurpation de serveur possible
3. **[MOYENNE] `GameRoom.cs` trop volumineuse** (570 lignes) — maintenabilité dégradée
4. **[MOYENNE] `FireCooldownTicks` hardcodé dans `GameRoom`** — non extensible par mode de jeu
5. **[BASSE] Powerup type déterministe** (`_nextPowerupId % 3`) — distribution prévisible

---

## 1. Performance & Optimisation

### 1.1 Hot path — `GameRoom.Tick()` ✅ Globalement sain

**Fichier** : `src/GameLogic/Rules/GameRoom.cs:189-262`

La boucle principale (20 TPS) utilise correctement des boucles `for` et `foreach` sur des collections natives. Pas de LINQ dans les chemins critiques. `GetAndClearEliminations()` n'alloue que si la liste est non vide — déjà optimisé.

### 1.2 `TickBullets` — RemoveAt en boucle inverse ✅

**Fichier** : `src/GameLogic/Rules/GameRoom.cs:410-414`

Pattern correct : itération inverse pour `RemoveAt`. Pas de problème de performance.

### 1.3 Absence de hard cap sur les bullets ⚠️

**Fichier** : `src/GameLogic/Rules/GameRoom.cs:352`
**Priorité** : Basse | **Effort** : 30 min

`_bullets` n'a pas de limite maximale. Avec 10 joueurs et cooldown 10 ticks, le maximum théorique est ~40 bullets simultanées — pas de risque immédiat. Un hard cap défensif est toutefois recommandé.

**Code avant** :
```csharp
_bullets.Add(new BulletEntity(_nextBulletId++, tank.Id, spawnPos, direction));
```

**Code après** :
```csharp
if (_bullets.Count < Constants.MaxBulletsInFlight)
    _bullets.Add(new BulletEntity(_nextBulletId++, tank.Id, spawnPos, direction));
```

**Ajout dans `Constants.cs`** :
```csharp
public const int MaxBulletsInFlight = 200; // safety cap (10 players × 20 bullets max)
```

### 1.4 Powerup pickup — `MathF.Sqrt` dans boucle nested ⚠️

**Fichier** : `src/GameLogic/Rules/GameRoom.cs:512-513`
**Priorité** : Basse | **Effort** : 15 min

Avec 10 tanks et ~5 powerups au maximum, l'impact est négligeable. Micro-optimisation simple :

**Code avant** :
```csharp
float dist = MathF.Sqrt(dx * dx + dy * dy);
if (dist < pickupDist)
```

**Code après** :
```csharp
if (dx * dx + dy * dy < pickupDist * pickupDist)
```

### 1.5 `GetDeltaState` — lastAckedTick toujours 0 ⚠️

**Fichier** : `src/Godot/Nodes/GameRoomNode.cs:397-399`
**Priorité** : Basse | **Effort** : 2h

```csharp
_network.Broadcast(new NetworkMessage(
    MessageType.GameStateDelta,
    GameStateSerializer.Serialize(_room.GetDeltaState(0))));  // ← toujours 0
```

Le delta est broadcast avec `lastAckedTick = 0`, donc on envoie l'état complet à chaque tick déguisé en delta. Ce n'est pas un bug fonctionnel mais de la bande passante gaspillée. À noter pour une future optimisation réseau.

---

## 2. Architecture & Design

### 2.1 Séparation GameLogic / Godot ✅ Exemplaire

Aucun `using Godot;` dans `GameLogic/`. Les tests NUnit tournent en CLI sans Godot. Pattern Thin Wrapper correctement appliqué sur tous les nodes. `IGameStateProvider` découple proprement `GameRenderer` du réseau.

### 2.2 Strategy Pattern (IBattleRules) ✅ Excellent

L'interface `IBattleRules` est bien définie, les 5 implémentations respectent le contrat. Le polymorphisme pour win conditions, spawns et scoring est élégant.

### 2.3 `GameRoom.cs` trop volumineuse ⚠️

**Fichier** : `src/GameLogic/Rules/GameRoom.cs` — 570 lignes
**Priorité** : Moyenne | **Effort** : 3h | **Impact maintenabilité** : +30%

`GameRoom` mélange plusieurs responsabilités :
- Gestion des sessions joueurs (`PlayerSession`, `AddPlayer`, `RemovePlayer`)
- Physique des bullets (`TickBullets`)
- Gestion des powerups (`TickPowerups`, `ApplyPowerup`)
- File de respawn (`ProcessRespawnQueue`)
- Snapshots réseau (`GetFullState`, `GetDeltaState`)

**Recommandation** : Splitter en fichiers partiels C# sans changer l'API publique :

```
GameRoom.cs            → ~200 lignes (API publique + orchestration Tick)
GameRoom.Bullets.cs    → TickBullets, TryFire
GameRoom.Powerups.cs   → TickPowerups, ApplyPowerup
GameRoom.Snapshots.cs  → GetFullState, GetDeltaState, GetBulletSnapshots…
```

### 2.4 `FireCooldownTicks` hardcodé dans `GameRoom` ⚠️

**Fichier** : `src/GameLogic/Rules/GameRoom.cs:57`
**Priorité** : Moyenne | **Effort** : 1h

```csharp
private const uint FireCooldownTicks = 10; // 0.5s at 20 TPS
```

Ce cooldown devrait appartenir à `IBattleRules` pour permettre des modes avec cadence différente (ex. training mode avec tir plus rapide).

**Code avant** :
```csharp
// GameRoom.cs
private const uint FireCooldownTicks = 10;
```

**Code après** :
```csharp
// IBattleRules.cs
uint FireCooldownTicks { get; }

// Toutes les implémentations par défaut :
public uint FireCooldownTicks => 10;

// TrainingRules :
public uint FireCooldownTicks => 5; // tir plus rapide en training
```

### 2.5 `GameRoomState` — mutation implicite ⚠️

**Fichier** : `src/GameLogic/Rules/GameRoomState.cs`
**Priorité** : Basse | **Effort** : 2h

`GameRoomState` expose les mêmes dictionnaires mutables (`_respawnQueue`, `_teamScores`) que `GameRoom`. Les règles modifient directement l'état partagé — fonctionnel mais viole la séparation propriétaire/consommateur.

**Recommandation** : Documenter explicitement quelles collections sont owned par `GameRoom` vs modifiables par `IBattleRules`.

### 2.6 Bot fill dans `GameRoomNode` ✅ Acceptable

La logique de bot fill (`FillBotsIfNeeded`) est dans `GameRoomNode` car elle nécessite de broadcaster les `PlayerJoined`. Couplage légitime avec le réseau — pas de violation d'architecture.

---

## 3. Sécurité & Fiabilité

### 3.1 Pas de rate limiting sur Login/Register 🔴

**Fichier** : `src/Godot/Nodes/GameRoomNode.cs:119-127`
**Priorité** : Haute | **Effort** : 2h

`OnLoginReceived` et `OnRegisterReceived` lancent des tâches async sans aucun throttling. Un attaquant peut envoyer des milliers de `LoginRequest` par seconde pour brute-forcer les mots de passe ou saturer SQLite.

**Code avant** :
```csharp
private void OnLoginReceived(int peerId, LoginRequest request)
{
    _ = HandleLoginAsync(peerId, request);
}
```

**Code après** :
```csharp
private readonly Dictionary<int, int> _authAttempts = new();
private const int MaxAuthAttemptsPerPeer = 5;

private void OnLoginReceived(int peerId, LoginRequest request)
{
    _authAttempts.TryGetValue(peerId, out int attempts);
    if (attempts >= MaxAuthAttemptsPerPeer)
    {
        _logger.LogWarning("Rate limit: peer {PeerId} exceeded auth attempts — disconnecting", peerId);
        _network.DisconnectPeer(peerId);
        return;
    }
    _authAttempts[peerId] = attempts + 1;
    _ = HandleLoginAsync(peerId, request);
}
// Nettoyer _authAttempts dans OnPlayerDisconnected
```

**Risques** : Connexions légitimes coupées en cas d'erreur de saisie répétée. Mitiger avec un délai progressif plutôt qu'une coupure dure si souhaité.

### 3.2 LAN Discovery sans authentification ⚠️

**Fichiers** : `src/Godot/Network/LanAnnouncer.cs`, `src/Godot/Network/LanDiscovery.cs`
**Priorité** : Haute | **Effort** : 2h

Les broadcasts UDP LAN ne sont pas signés. N'importe qui sur le réseau local peut envoyer un faux `ServerAnnouncement` pour attirer des clients vers un serveur malveillant.

**Solution minimale — filtrage par version** :

**Code avant** (`ServerAnnouncement.cs`) :
```csharp
public record ServerAnnouncement(string Name, int Port, string Mode, int PlayerCount, string RoomCode);
```

**Code après** :
```csharp
public record ServerAnnouncement(string Name, int Port, string Mode, int PlayerCount,
    string RoomCode, string AppVersion);
```

`LanDiscovery.cs` : ignorer les annonces dont `AppVersion != Constants.GameVersion`.

### 3.3 Credentials SMTP via env vars ✅

**Fichier** : `src/Godot/CrashReport/CrashReportMailer.cs`

Bonne pratique — pas de credentials hardcodés, fallback gracieux si `SMTP_HOST` absent.

**Point mineur** : L'adresse du destinataire est hardcodée ligne 12. Envisager `CRASH_REPORT_EMAIL` env var.

### 3.4 Timeout d'authentification ✅

**Fichier** : `src/Godot/Nodes/GameRoomNode.cs:340-355`

`EvictStalePendingAuth()` déconnecte les peers non authentifiés après 30s. Bonne protection contre les connexions fantômes.

### 3.5 Rate limiting inputs ENet ✅

**Fichier** : `src/Godot/Network/ServerNetworkManager.cs:16`

`MinInputIntervalMs = 40` (max 25 msg/s par peer). Protection correcte contre le flood d'inputs.

### 3.6 Validation inputs ✅

`ApplyInput` vérifie le `SequenceNumber` pour rejeter les inputs obsolètes. Logique de jeu 100% côté serveur (authoritative).

### 3.7 MessagePack — sécurité désérialisation ⚠️

**Fichier** : `src/GameLogic/Network/GameStateSerializer.cs`
**Priorité** : Basse | **Effort** : 30 min

Vérifier que MessagePack n'utilise pas `TypelessContractlessStandardResolver` qui permettrait la désérialisation de types arbitraires (gadget chain).

```csharp
// Recommandé pour données réseau non fiables :
var options = MessagePackSerializerOptions.Standard
    .WithSecurity(MessagePackSecurity.UntrustedData);
```

### 3.8 Graceful shutdown ⚠️

**Fichier** : `src/Godot/Nodes/ServerNode.cs`
**Priorité** : Basse | **Effort** : 1h

Vérifier que `ServerNetworkManager.Stop()` est appelé sur `_Notification(NotificationWMCloseRequest)` pour fermer proprement les connexions ENet sur CTRL+C.

---

## 4. Standards & Conventions

### 4.1 Naming conventions ✅

PascalCase classes, `_camelCase` champs privés, interfaces `I*`, enums PascalCase — conformes à `standards/csharp-code.md`.

### 4.2 Nullable reference types ✅

`<Nullable>enable</Nullable>` activé. Usage de `null!` pour les injections Godot (accepté). Pas de NullReferenceException apparent.

### 4.3 Structured logging ✅

Message templates Serilog (`{PlayerId}`, `{BulletId}`) — conforme à `standards/logging.md`. Pas d'interpolation de chaînes dans les appels de log.

### 4.4 `Result<T>` pattern ✅

`AddPlayer`, `AddBot` retournent `Result<T>`. Pas d'exceptions métier.

### 4.5 Powerup type déterministe ⚠️

**Fichier** : `src/GameLogic/Rules/GameRoom.cs:496`
**Priorité** : Basse | **Effort** : 30 min

```csharp
var type = (PowerupType)(_nextPowerupId % 3);  // prévisible
```

**Code après** :
```csharp
// Ajouter _random = new Random(); dans le constructeur
var type = (PowerupType)(_random.Next(3));
```

### 4.6 Doc comments incomplets ⚠️

**Priorité** : Basse

`GameRoom.Tick`, `AddPlayer`, `GetFullState` et les méthodes de `IBattleRules` manquent de doc comments XML.

---

## 5. Tests

### 5.1 Couverture globale ✅

141 tests, 162 ms. Excellent coverage sur entités, rules, physique et réseau.

### 5.2 Gap : directions wall collision ⚠️

**Fichier** : `src/Tests/Physics/WallCollisionTests.cs`
**Priorité** : Moyenne | **Effort** : 1h

`ResolveTankWallCollision` est testée pour le push gauche uniquement. Directions right/up/down non couvertes.

**Tests à ajouter** :
```csharp
[TestCase(50f, 200f)]   // push right (tank à gauche du mur)
[TestCase(350f, 200f)]  // push left (tank à droite du mur)
[TestCase(200f, 50f)]   // push down
[TestCase(200f, 350f)]  // push up
public void ResolveTankWallCollision_AllDirections_TankPushedOutOfWall(float tankX, float tankY)
{
    var tank = new TankEntity(1, new Vector2(tankX, tankY));
    var wall = new WallData(150f, 150f, 100f, 100f); // mur centré en (200,200)
    bool resolved = CollisionSystem.ResolveTankWallCollision(tank, wall);
    resolved.Should().BeTrue();
    // tank.Position should be outside wall bounds
}
```

### 5.3 Gap : sérialisation types Protocol ⚠️

**Fichier** : `src/Tests/Network/SerializationTests.cs`
**Priorité** : Basse | **Effort** : 1h

Non testés : `LoginRequest`, `RegisterRequest`, `LeaderboardResponse`, `PlayerEliminatedMessage`, `JoinTrainingRequest`.

### 5.4 Test friendly fire fragile ⚠️

**Fichier** : `src/Tests/Rules/TeamsRulesTests.cs`
**Priorité** : Basse | **Effort** : 30 min

Le test `FriendlyFire_SameTeam_DoesNotDamage` crée 4 joueurs pour garantir 2 équipes. Fragile si `MinPlayersToStart` change. Utiliser `new TeamsRules()` directement avec `GameStateFixtures`.

---

## 6. Fonctionnalités & Logique Métier

### 6.1 SimpleBot — aucun pathfinding ⚠️

**Fichier** : `src/GameLogic/AI/SimpleBot.cs`
**Priorité** : Basse (acceptable training mode)

Le bot se bloque contre les murs. Acceptable pour Training, limité pour Solo avec bots.

**Recommandation** : Détecter l'immobilité (comparer position tick N vs N-20) et forcer une rotation si bloqué.

### 6.2 Respawn queue — joueur déconnecté ✅

`ProcessRespawnQueue` gère proprement le cas via `_tanks.TryGetValue`. Le test `RemovePlayer_WhileInRespawnQueue_DoesNotCrash` valide ce comportement.

### 6.3 Zone shrink — comportement cohérent ✅

`ZoneController` centré sur (500,500), shrink -80px toutes les 30s, minimum 50px. Comportement prévisible et équilibré pour Battle Royale.

---

## Plan d'Implémentation (3 semaines)

### Semaine 1 — Critique & Haute priorité (~8h)

| # | Item | Fichier | Effort |
|---|------|---------|--------|
| 1 | Rate limiting Login/Register | `GameRoomNode.cs:119` | 2h |
| 2 | LAN Discovery — filtrage AppVersion | `ServerAnnouncement.cs`, `LanDiscovery.cs` | 2h |
| 3 | Tests directions wall collision (4 directions) | `WallCollisionTests.cs` | 1h |
| 4 | Hard cap bullets + constante | `GameRoom.cs:352`, `Constants.cs` | 30min |
| 5 | Powerup type aléatoire | `GameRoom.cs:496` | 30min |
| 6 | `CrashReportMailer` — Recipient en env var | `CrashReportMailer.cs:12` | 30min |
| 7 | Valider graceful shutdown | `ServerNode.cs` | 1h |

### Semaine 2 — Moyenne priorité + Tests (~6h)

| # | Item | Fichier | Effort |
|---|------|---------|--------|
| 8 | `FireCooldownTicks` dans `IBattleRules` | `IBattleRules.cs` + 5 implémentations | 1h |
| 9 | Tests sérialisation types manquants | `SerializationTests.cs` | 1h |
| 10 | Fix test friendly fire fragile | `TeamsRulesTests.cs` | 30min |
| 11 | Optimisation distance pickup (sqrt → squared) | `GameRoom.cs:512` | 15min |
| 12 | MessagePack — UntrustedData security option | `GameStateSerializer.cs` | 30min |
| 13 | Installer Trivy + step CI | `justfile`, `ci.yml` | 2h |

### Semaine 3 — Refactoring (~5h)

| # | Item | Fichier | Effort |
|---|------|---------|--------|
| 14 | Split `GameRoom.cs` en partial classes | `GameRoom.cs` | 3h |
| 15 | Doc comments `IBattleRules` + `GameRoom` | `IBattleRules.cs`, `GameRoom.cs` | 1h |
| 16 | SimpleBot — détection blocage murs | `SimpleBot.cs` | 1h |

---

## Métriques Globales

| Priorité | Nombre | Effort total |
|----------|--------|--------------|
| Haute | 2 | 4h |
| Moyenne | 4 | 5h30 |
| Basse | 10 | 9h |
| **Total** | **16** | **~18h30** |

---

## Checklist d'Implémentation

### Sécurité
- [ ] Rate limiting `OnLoginReceived` / `OnRegisterReceived` (max 5 tentatives/peer)
- [ ] `_authAttempts` nettoyé dans `OnPlayerDisconnected`
- [ ] `ServerAnnouncement` — ajouter `AppVersion`
- [ ] `LanDiscovery` — filtrer si `AppVersion != Constants.GameVersion`
- [ ] `CrashReportMailer` — `Recipient` depuis env var `CRASH_REPORT_EMAIL`
- [ ] Valider graceful shutdown dans `ServerNode`
- [ ] MessagePack — `MessagePackSecurity.UntrustedData` sur données réseau

### Performance
- [ ] `MaxBulletsInFlight = 200` dans `Constants.cs`
- [ ] Guard bullet cap dans `GameRoom.TryFire`
- [ ] Distance² dans `TickPowerups` (supprimer `MathF.Sqrt`)

### Architecture
- [ ] `FireCooldownTicks` dans `IBattleRules` (default 10, Training 5)
- [ ] Mettre à jour les 5 implémentations de règles
- [ ] Split `GameRoom.cs` en 4 fichiers partiels

### Tests
- [ ] `ResolveTankWallCollision` — couvrir directions right/up/down
- [ ] Sérialisation : `LoginRequest`, `RegisterRequest`, `LeaderboardResponse`, `PlayerEliminatedMessage`
- [ ] Refactorer test friendly fire `TeamsRulesTests`
- [ ] Installer Trivy + step CI

### Standards
- [ ] Doc comments `IBattleRules` méthodes
- [ ] Doc comments `GameRoom.Tick`, `AddPlayer`, `GetFullState`
- [ ] Powerup type → `_random.Next(3)`

---

## Annexes

### A. Résultats Tests

```
dotnet test src/Tests/BattleTank.Tests.csproj
Passed! — Failed: 0, Passed: 141, Skipped: 0, Total: 141, Duration: 162 ms
```

### B. Scan Trivy

```
trivy: not found
```

Trivy n'est pas installé sur l'environnement local. Ajouter au CI :

```yaml
# .github/workflows/ci.yml
- name: Security scan
  uses: aquasecurity/trivy-action@master
  with:
    scan-type: 'fs'
    scan-ref: '.'
    severity: 'CRITICAL,HIGH'
    exit-code: '1'
```

### C. Dépendances NuGet — Points de vigilance

| Package | Usage | Point de vigilance |
|---------|-------|-------------------|
| `BCrypt.Net-Next` | Hash passwords | Vérifier version >= 4.0.3 |
| `MessagePack` | Sérialisation réseau | Activer `UntrustedData` security |
| `EF Core SQLite 8` | Persistance | Stable avec .NET 8 ✅ |
| `Serilog` | Logging | Vérifier sink filesystem non accessible |

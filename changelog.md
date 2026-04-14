# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- `Tests/Rules/BattleRoyaleRulesTests.cs` — 7 tests couvrant mode, spawn, kill tracking, win condition (1 survivant, 0 survivant, plusieurs vivants), leaderboard
- `Tests/Shared/ResultTests.cs` — 6 tests couvrant `Result<T>` Ok/Fail (IsSuccess, Value, Error, valeur par défaut)
- `Tests/Network/SerializationTests.cs` — 11 tests d'intégration MessagePack round-trip : `PlayerInput`, `GameStateFull`, `GameStateDelta`, `LoginRequest/Response`, `RegisterRequest/Response`, `GameOverMessage`, `CountdownMessage`, `LeaderboardResponse`

Couverture GameLogic atteinte : 91% lignes / 83% branches (objectif 80%).

---

- `GameLogic/Persistence/Models/` — entités EF Core : `PlayerAccount` (username, hash bcrypt, avatarSeed, createdAt), `PlayerStats` (kills, deaths, wins, gamesPlayed, playtimeSeconds par mode), `GameRecord` (historique des parties)
- `GameLogic/Persistence/IPlayerRepository` — interface de persistance : find, create, updateStats, getStats
- `GameLogic/Persistence/ILeaderboardService` — interface leaderboard par mode
- `Godot/Persistence/BattleTankDbContext` — DbContext EF Core SQLite, `EnsureCreated()` au démarrage serveur
- `Godot/Persistence/PlayerRepository` — implémentation EF Core : bcrypt verify/hash, stats incrémentales, historique des parties
- `Godot/Persistence/LeaderboardService` — classement par mode (wins > kills) via LINQ/EF Core
- `GameLogic/Network/Protocol.cs` — nouveaux messages : `LoginRequest/Response`, `RegisterRequest/Response`, `LeaderboardRequest/Response`, `LeaderboardEntryMessage`
- `Godot/Network/ServerNetworkManager` — RPC `ReceiveReliableMessage` (Reliable) pour auth ; événements `LoginReceived`, `RegisterReceived`, `LeaderboardRequested` ; `SendToPlayerReliable`
- `Godot/Network/ClientNetworkManager` — RPC `ReceiveReliableMessage` (Reliable) pour réponses auth ; `SendLogin`, `SendRegister`, `RequestLeaderboard` ; événements `LoginResponseReceived`, `RegisterResponseReceived`, `LeaderboardResponseReceived`
- `Godot/Nodes/GameRoomNode` — auth flow pre-game (`_pendingAuth` / `_authenticated`) : les peers attendent `LoginRequest`/`RegisterRequest` avant d'entrer en salle ; sauvegarde asynchrone des stats à la fin de partie (capture snapshot avant `Reset()`)
- `Godot/Nodes/ServerNode` — initialisation `BattleTankDbContext` + injection `IPlayerRepository` / `ILeaderboardService` dans `GameRoomNode` ; export `DbPath`
- `Godot/Nodes/ClientNode` — gestion login/register, stockage `_accountId`/`_nickname`, input bloqué jusqu'à auth
- `Godot/UI/LoginScreen` — écran login/register procédural (CanvasLayer) : champs username/password, boutons Login/Register, label de statut/erreur
- `GameLogic/Rules/GameRoom` — tracking `_playerAccountIds`, `_gameStartTick`, propriétés `GameDurationSeconds`, `PlayerKills`, méthodes `SetPlayerAccountId`/`GetPlayerAccountId`

- `GameLogic/Shared/Types.cs` — `GameMode` enum (BattleRoyale/Teams/Deathmatch/CaptureZone), `ControlPointSnapshot`, `PlayerInfo.TeamId`
- `GameLogic/Shared/Constants.cs` — constantes deathmatch (`DeathmatchDurationTicks`, `DeathmatchRespawnDelayTicks`), capture de zone (`CaptureZoneScoreToWin`, `CaptureZoneDurationTicks`, `ControlPointRadius`, `CaptureRatePerSecond`)
- `GameLogic/Entities/TankEntity.cs` — `TeamId`, `IsEliminated`, `Respawn(Vector2 position)` (réinitialise santé, position, vitesse)
- `GameLogic/Entities/ControlPoint.cs` — zone de capture : progression, équipe dominante, snapshot
- `GameLogic/Rules/IBattleRules.cs` — interface de règles modulaire : `Mode`, `IsFriendlyFireEnabled`, `UseShrinkingZone`, `UsesPowerups`, `Initialize`, `GetSpawnPoint`, `OnPlayerAdded`, `OnElimination`, `OnTick`, `CheckWinCondition`, `GetLeaderboard`
- `GameLogic/Rules/GameRoomState.cs` — contexte partagé passé aux règles (tanks, kills, équipes, scores, respawn queue, control points)
- `GameLogic/Rules/BattleRoyaleRules.cs` — règles BR : dernier survivant, zone rétrécissante, powerups
- `GameLogic/Rules/TeamsRules.cs` — règles équipes : attribution round-robin, no friendly fire, victoire par équipe, spawn groupé
- `GameLogic/Rules/DeathmatchRules.cs` — règles deathmatch : timer 3 min, respawn après 3s, victoire par kills
- `GameLogic/Rules/CaptureZoneRules.cs` — règles capture de zone : 3 points de contrôle, score cumulatif par équipe, victoire à 100 pts ou au timer
- `GameLogic/Rules/GameRoom.cs` — refactorisé : délégation à `IBattleRules`, support du respawn, friendly fire check, `WinnerTeamId`, `TeamScores`, nouveau constructeur `GameRoom(logger, rules)`
- `GameLogic/Network/Protocol.cs` — `GameMode Mode` et `ControlPointSnapshot[]? ControlPoints` dans `GameStateFull`/`GameStateDelta`, `WinnerTeamId` dans `GameOverMessage`
- `Tests/Rules/TeamsRulesTests.cs` — 7 tests : attribution équipes, friendly fire, victoire par équipe, leaderboard groupé
- `Tests/Rules/DeathmatchRulesTests.cs` — 6 tests : timer, victoire par kills, respawn, partie continue après mort
- `Tests/Rules/CaptureZoneRulesTests.cs` — 7 tests : 3 control points, score cumulatif, timer, victoire, reset
- `Tests/Entities/ControlPointTests.cs` — 7 tests : progression, contestation, capture complète, snapshot, tank mort ignoré

- `GameLogic/Shared/Types.cs` — `GamePhase.Lobby`, `PowerupType` (ExtraAmmo/Shield/SpeedBoost), `PowerupSnapshot`, `PlayerInfo` (id, nickname, kills)
- `GameLogic/Shared/Constants.cs` — `LobbyCountdownTicks`, `PowerupSpawnIntervalTicks`, `SpeedBoostDurationTicks`, `ShieldHealAmount`, `PowerupRadius`
- `GameLogic/Entities/PowerupEntity.cs` — entité powerup (position, type, pickup state, snapshot)
- `GameLogic/Entities/TankEntity.cs` — méthodes `Heal()`, `ApplySpeedBoost()`, `TickSpeedBoost()`, propriété `SpeedMultiplier`
- `GameLogic/Rules/GameRoom.cs` — phase Lobby avec compte à rebours (3s), pseudo joueur, suivi des kills par joueur, spawn de powerups toutes les 10s, application des effets powerup
- `GameLogic/Network/Protocol.cs` — `CountdownMessage`, `MessageType.Countdown`, `PlayerInfo[]` dans `GameStateFull`, `PowerupSnapshot[]` dans `GameStateFull`/`GameStateDelta`, leaderboard dans `GameOverMessage`
- `Tests/Rules/LobbyCountdownTests.cs` — 6 tests : transition Lobby, compte à rebours, accès pendant Lobby
- `Tests/Rules/ScoreTests.cs` — 6 tests : suivi kills, leaderboard trié, pseudos
- `Tests/Entities/PowerupEntityTests.cs` — 7 tests : pickup, snapshot, Heal, SpeedBoost

- `GameLogic/Network/Protocol.cs` — types de messages réseau (MessageType, InputFlags, PlayerInput, GameStateFull, GameStateDelta, PlayerJoined/Eliminated/Over, ZoneUpdate)
- `GameLogic/Network/GameStateSerializer.cs` — sérialisation/désérialisation MessagePack générique
- `GameLogic/Entities/TankEntity.cs` — entité tank (position, rotation, santé, mouvement, dégâts, snapshot)
- `GameLogic/Shared/Types.cs` — types partagés : GamePhase, TankSnapshot, BulletSnapshot, ZoneSnapshot
- `GameLogic/Shared/Constants.cs` — constantes tank, balle, map et zone
- `GameLogic/Shared/Result.cs` — type `Result<T>` pour les opérations de salle
- `GameLogic/Entities/BulletEntity.cs` — entité balle (trajectoire, portée max, destruction)
- `GameLogic/Physics/CollisionSystem.cs` — détection balle-tank et hors-carte
- `GameLogic/Rules/GameRoom.cs` — salle de jeu : gestion joueurs, boucle 20 TPS, tir avec cooldown, condition de victoire
- `GameLogic/Rules/ZoneController.cs` — zone rétrécissante : rétrécissement toutes les 30s, dégâts aux tanks hors zone
- `Godot/Network/ServerNetworkManager.cs` — serveur ENet : démarrage, connexion/déconnexion, réception PlayerInput, broadcast
- `Godot/Network/ClientNetworkManager.cs` — client ENet : connexion, envoi PlayerInput, réception GameStateFull/Delta
- `Godot/Nodes/GameRoomNode.cs` — driver Godot, accumulator 20 TPS, câblage réseau ↔ GameRoom
- `Godot/Nodes/TankNode.cs` — Node2D, rendu tank (corps + canon via `_Draw()`)
- `Godot/Nodes/BulletNode.cs` — Node2D, rendu balle (cercle jaune)
- `Godot/Nodes/ZoneNode.cs` — Node2D, rendu safe zone (cercle vert + contour blanc)
- `Godot/Nodes/ServerNode.cs` — point d'entrée serveur headless
- `Godot/Nodes/ClientNode.cs` — point d'entrée client, lecture clavier, envoi inputs
- `Godot/Renderer/GameRenderer.cs` — sync nodes TankNode/BulletNode/ZoneNode depuis GameStateDelta
- `Godot/UI/HudNode.cs` — CanvasLayer : HP + compteur de joueurs vivants
- `Tests/Entities/TankEntityTests.cs` — 12 tests NUnit sur TankEntity
- `Tests/Entities/BulletEntityTests.cs` — 4 tests NUnit sur BulletEntity
- `Tests/Physics/CollisionSystemTests.cs` — 6 tests NUnit sur CollisionSystem
- `Tests/Rules/GameRoomTests.cs` — 8 tests NUnit sur GameRoom
- `Tests/Rules/ZoneControllerTests.cs` — 6 tests NUnit sur ZoneController

### Changed

- `GameLogic/Shared/Constants.cs` — passage de `internal` à `public` (accès depuis Godot/)
- `GameLogic/Network/Protocol.cs` — `GameStateFull` et `GameStateDelta` incluent désormais `ZoneSnapshot`
- `Godot/Network/ClientNetworkManager.cs` — ajout `IsConnected()`

- `GameLogic/Rules/GameRoom.cs` — tracking des éliminations (balle + zone), `GetAndClearEliminations()`
- `GameLogic/Rules/ZoneController.cs` — zone rétrécissante toutes les 30s, dégâts hors zone
- `GameLogic/Shared/Types.cs` — `WallData` record
- `GameLogic/Shared/MapLayout.cs` — 15 murs statiques (croix centrale, blocs quadrants, couloirs)
- `GameLogic/Physics/CollisionSystem.cs` — `BulletHitsWall`, `ResolveTankWallCollision`, `ClampTankToMap`
- `GameLogic/Entities/TankEntity.cs` — `SetPosition()` pour résolution de collision
- `Godot/Nodes/GameRoomNode.cs` — broadcast `PlayerEliminated` après chaque tick
- `Godot/Nodes/ZoneNode.cs` — rendu safe zone (cercle vert + contour blanc)
- `Godot/Nodes/WallNode.cs` — rendu murs (rectangle brun)
- `Godot/Network/ClientNetworkManager.cs` — events `PlayerEliminated` et `GameOver`
- `Godot/Renderer/GameRenderer.cs` — `ZoneNode`, `WallNode`, minimap via HudNode
- `Godot/UI/HudNode.cs` — `MinimapNode` intégré, `Initialize(localPlayerId)`
- `Godot/UI/MinimapNode.cs` — minimap 120×120px : tanks (local/ennemi/mort) + arc safe zone
- `Godot/UI/GameOverScreen.cs` — écran VICTORY / DEFEAT / ELIMINATED
- `Godot/Nodes/ClientNode.cs` — gestion `PlayerEliminated` et `GameOver`, stop inputs après mort
- `Tests/Rules/ZoneControllerTests.cs` — 6 tests NUnit
- `Tests/Physics/WallCollisionTests.cs` — 6 tests NUnit

### Fixed

### Removed

---

## [0.1.0] - 2026-04-13

### Added

- Initial project setup from template
- CLAUDE.md, backlog.md, issue.md, team.md
- Standards : architecture, network, csharp-code, testing, error-handling, logging
- Justfile, .pre-commit-config.yaml
- Claude commands : commit, adr, std, review, review-changes, optimize, security, test, pr

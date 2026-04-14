# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.0.12] — 2026-04-14

### Added

- `GameLogic/Rules/GameRoom.Bullets.cs` — partial class : `TryFire`, `TickBullets` extraits de `GameRoom.cs`
- `GameLogic/Rules/GameRoom.Powerups.cs` — partial class : `TickPowerups`, `ApplyPowerup` extraits de `GameRoom.cs`
- `GameLogic/Rules/GameRoom.Snapshots.cs` — partial class : `GetFullState`, `GetDeltaState`, helpers snapshot extraits de `GameRoom.cs`

### Changed

- `GameLogic/Rules/GameRoom.cs` — déclaré `partial` ; `GetFullState`, `GetDeltaState`, `TryFire`, `TickBullets`, `TickPowerups`, `ApplyPowerup` déplacés dans les fichiers partiels ; doc comments ajoutés sur `Tick`, `AddPlayer`, `GetFullState`, `GetDeltaState`
- `GameLogic/Rules/IBattleRules.cs` — doc comments ajoutés sur toutes les propriétés (`Mode`, `IsFriendlyFireEnabled`, `UseShrinkingZone`, `UsesPowerups`)
- `GameLogic/AI/SimpleBot.cs` — détection de blocage murs : si la position ne change pas sur 20 ticks consécutifs, change de direction immédiatement
- `Godot/Nodes/ServerNode.cs` — graceful shutdown : `_Notification(NotificationWMCloseRequest)` appelle `Stop()` + `Quit()` pour fermer proprement sur CTRL+C headless
- `Tests/Rules/TeamsRulesTests.cs` — `FriendlyFire_SameTeam_DoesNotDamage` : assertions `GetPlayerTeamId` ajoutées pour documenter explicitement pourquoi 4 joueurs sont nécessaires

## [0.0.11] — 2026-04-14

### Security

- `Godot/Nodes/GameRoomNode.cs` — rate limiting Login/Register : max 5 tentatives par peer, déconnexion automatique au dépassement ; `_authAttempts` nettoyé à la déconnexion
- `Godot/Network/ServerAnnouncement.cs` — ajout du champ `AppVersion` dans les broadcasts LAN
- `Godot/Network/LanDiscovery.cs` — filtre les annonces dont `AppVersion != Constants.GameVersion` pour rejeter les serveurs incompatibles
- `Godot/CrashReport/CrashReportMailer.cs` — adresse destinataire configurable via env var `CRASH_REPORT_EMAIL` (fallback sur valeur précédente)
- `GameLogic/Network/GameStateSerializer.cs` — `MessagePackSecurity.UntrustedData` activé sur serialize/deserialize pour protéger contre les gadget-chain attacks

### Added

- `GameLogic/Shared/Constants.cs` — `MaxBulletsInFlight = 200` : hard cap défensif sur les bullets simultanées
- `GameLogic/Rules/IBattleRules.cs` — propriété `FireCooldownTicks` : cooldown de tir configurable par mode de jeu
- `Tests/Physics/WallCollisionTests.cs` — 3 nouveaux tests couvrant les directions right/up/down pour `ResolveTankWallCollision`

### Changed

- `GameLogic/Rules/GameRoom.cs` — `FireCooldownTicks` supprimé comme constante locale, délégué à `_rules.FireCooldownTicks` ; bullet cap appliqué dans `TryFire` ; powerup type aléatoire via `_random.Next(3)` (au lieu de `_nextPowerupId % 3`) ; distance pickup en distance² (suppression `MathF.Sqrt`) ; `Random` injectable via constructeur pour tests déterministes
- `GameLogic/Rules/BattleRoyaleRules.cs`, `DeathmatchRules.cs`, `TeamsRules.cs`, `CaptureZoneRules.cs` — implémentent `FireCooldownTicks => 10`
- `GameLogic/Rules/TrainingRules.cs` — `FireCooldownTicks => 5` (tir plus rapide en training)
- `Godot/Nodes/HostNode.cs` — passe `AppVersion: Constants.GameVersion` à `LanAnnouncer`
- `Tests/Rules/GameRoomTests.cs` — tests powerup refactorisés avec `Random` seedé pour déterminisme

## [0.0.10] — 2026-04-14

### Added (Phase 11 — Solo local & découverte LAN)

- `Godot/Network/IGameStateProvider.cs` — interface découplant `GameRenderer` de `ClientNetworkManager` ; implémentée par `ClientNetworkManager` (réseau) et `LocalGameNode` (offline)
- `Godot/Nodes/LocalGameNode.cs` — mode solo offline : instancie `GameRoom` + bots pour tous les modes de jeu (Training, BattleRoyale, Teams, Deathmatch, CaptureZone), lit l'input clavier directement, alimente `GameRenderer` à 20 TPS via snapshots locaux
- `Godot/UI/MainMenuScreen.cs` — écran d'accueil avec trois entrées : « Jouer solo », « Héberger une partie », « Rejoindre une partie » ; remplace la connexion automatique au démarrage
- `Godot/UI/SoloModeScreen.cs` — sélecteur de mode pour le jeu solo ; liste tous les modes avec champ pseudo
- `Godot/Nodes/HostNode.cs` — hébergement in-process : démarre `ServerNetworkManager` + `GameRoomNode` dans le même process, lance `LanAnnouncer`, fire `ServerReady` pour connexion loopback client
- `Godot/UI/HostSetupScreen.cs` — configuration de la partie hébergée (nom, port, code optionnel)
- `Godot/UI/ServerInfoScreen.cs` — affiche les infos de connexion (IP, port, code) à partager avec les autres joueurs
- `Godot/Network/LanAnnouncer.cs` — broadcast UDP toutes les 2 s sur le port 4243 (payload JSON : nom, port, mode, joueurs, hasCode)
- `Godot/Network/LanDiscovery.cs` — écoute les broadcasts UDP LAN, maintient une liste de serveurs avec TTL 6 s, fire `ServerListChanged`
- `Godot/Network/ServerAnnouncement.cs` — record partagé entre `LanAnnouncer` et `LanDiscovery`
- `Godot/UI/RoomBrowserScreen.cs` — browser de parties LAN + saisie IP manuelle ; gère le prompt de code si la room est protégée
- `Godot/UI/RoomPasswordScreen.cs` — saisie du code de room avant connexion

### Changed

- `Godot/Renderer/GameRenderer.cs` — `Initialize()` accepte `IGameStateProvider` au lieu de `ClientNetworkManager` (rétrocompatible)
- `Godot/Network/ClientNetworkManager.cs` — implémente `IGameStateProvider`
- `Godot/Network/ServerNetworkManager.cs` — `JoinTrainingReceived` passe désormais `JoinTrainingRequest` complet (au lieu du nickname seul) pour permettre la vérification du code de room
- `GameLogic/Network/Protocol.cs` — `LoginRequest` et `JoinTrainingRequest` ajoutent `string? RoomCode = null` (Key 2 / Key 1, rétrocompatible MessagePack)
- `Godot/Nodes/GameRoomNode.cs` — propriété `RoomCode` ; vérifie le code à la connexion (`OnLoginReceived`, `OnJoinTrainingReceived`) et refuse avec `"Code incorrect"` si invalide
- `Godot/Nodes/ClientNode.cs` — refactorisé pour le nouveau flux de navigation : `MainMenuScreen` → solo / hébergement / rejoindre ; `_pendingRoomCode` injecté dans `LoginRequest` et `JoinTrainingRequest`

## [0.0.9] — 2026-04-14

### Added (Phase 9 — Solo & IA ennemie)

- `GameLogic/AI/IBot.cs` — interface `IBot` : `ComputeInput(tanks, currentTick)` appelé à chaque tick
- `GameLogic/AI/SimpleBot.cs` — implémentation `SimpleBot` : déplacement aléatoire changeant toutes les 2 s, rotation vers la cible la plus proche, tir dès l'alignement (tolérance 15°)
- `GameLogic/Rules/TrainingRules.cs` — règles d'entraînement : démarrage immédiat avec 1 joueur (`MinPlayersToStart = 1`), pas de zone rétrécissante, respawn rapide sans condition de victoire
- `GameLogic/Shared/Types.cs` — ajout de `GameMode.Training = 4`
- `GameLogic/Network/Protocol.cs` — ajout `MessageType.JoinTraining` (0x50) et `JoinTrainingRequest` (nickname)
- `GameLogic/Rules/IBattleRules.cs` — ajout propriété `int MinPlayersToStart` (toutes les implémentations retournent `Constants.MinPlayersToStart`, sauf `TrainingRules` qui retourne 1)
- `GameLogic/Rules/GameRoom.cs` — `AddBot()` : crée un `SimpleBot` avec ID négatif et suffixe `[BOT]` ; `IsBot(id)` ; bots tick avant le loop tanks ; `Reset()` nettoie les bots
- `Godot/Network/ServerNetworkManager.cs` — événement `JoinTrainingReceived` dispatché sur `MessageType.JoinTraining`
- `Godot/Nodes/GameRoomNode.cs` — mode entraînement (`trainingMode=true`) : `TrainingRules`, auth bypass via `JoinTraining`, pas de stats ; bot fill au démarrage de la partie (`FillBotsIfNeeded`) ; `_botsFilled` reset à chaque partie
- `Godot/UI/TrainingOverlayNode.cs` — overlay training : bouton "Rejoindre une partie" (reconnexion en mode ranked) + bouton "Quitter"
- `Godot/UI/LoginScreen.cs` — bouton "Mode Entraînement" (actif après connexion au serveur) ; événement `TrainingRequested`
- `Godot/Network/ClientNetworkManager.cs` — méthode `SendJoinTraining()`
- `Godot/Nodes/ClientNode.cs` — gestion training mode : `OnTrainingRequested`, `OnGameStateFullReceived` (activation overlay), `OnJoinRankedRequested` (reconnexion ranked), `OnQuitRequested`

### Added (Phase 3 — Capture de zone)

- `ControlPointsNode.cs` — nouveau node Godot qui dessine les zones de capture sur la carte principale ; couleur par équipe (jaune = neutre, bleu = team 0, rouge = team 1) avec arc de progression
- `MinimapNode.cs` — affichage des zones de capture sur la minimap avec couleur par équipe
- `HudNode.cs` / `GameRenderer.cs` — `ControlPointSnapshot[]` propagé du `GameStateFull`/`GameStateDelta` jusqu'à la minimap et au `ControlPointsNode`

### Added (Documentation — serveur & client)

- `docs/server-setup.md` — guide d'hébergement : Docker (recommandé) + binaire direct, configuration réseau, systemd service
- `docs/client-setup.md` — guide d'installation client : Linux (chmod + prérequis), macOS (Gatekeeper / xattr), Windows (SmartScreen)
- `README.md` — section "Jouer" avec liens vers les deux guides et la page releases
- `release.yml` — les archives de release incluent désormais `SETUP.md` (copie du guide correspondant)

### Added (CI/CD — GitHub Actions)

- `.github/workflows/ci.yml` — pipeline CI : tests NUnit C# sur chaque push/PR vers master
- `.github/workflows/release.yml` — pipeline release : déclenché sur tag `v*`, exporte server Linux + client Linux/Windows/macOS via `barichello/godot-ci:4.6.2`, crée une GitHub Release avec les archives téléchargeables et les notes de release générées depuis `git log`

### Added (Phase 7 — Ops)

- `export_presets.cfg` — Godot 4.6 export presets: Linux Server (dedicated headless), Linux client, Windows client, macOS client
- `justfile` — export commands: `export-server`, `export-client-linux`, `export-client-windows`, `export-client-macos`, `export-client`
- `justfile` — docker commands: `docker-build`, `docker-run`, `docker-stop`, `docker-logs`, `docker-metrics`
- `ServerNetworkManager.cs` — `GetPeerRtt(int peerId)` exposes ENet round-trip time per peer via `ENetPacketPeer.GetStatistic()`
- `GameRoomNode.cs` — periodic metrics logging every 5 s (100 ticks): player count, game phase, and per-peer RTT; lines prefixed with `[metrics]` for easy grepping from `docker logs`

### Fixed (Phase 7 — Ops)

- `docker/Dockerfile` — bumped `GODOT_VERSION` from 4.3 to 4.6 to match project
- `docker/Dockerfile` — switched production base image from `debian:bookworm-slim` to `mcr.microsoft.com/dotnet/runtime:8.0-bookworm-slim` (required for C# Godot exports)
- `docker/Dockerfile` — added `mkdir -p /build` before export step

### Added (Phase 5 — polish)

- `TankNode.cs` — procedural explosion animation (3 expanding rings with alpha fade, 0.5s duration) plays on tank death
- `TankNode.cs` — damage flash overlay (white semi-transparent rect, 0.15s) on health decrease
- `KillFeedNode.cs` — kill feed overlay (top-right `CanvasLayer`) showing "Player X eliminated Player Y" entries that auto-expire after 4s (max 5 visible)
- `GameRenderer.cs` — wired `PlayerEliminated` network event to `KillFeedNode`
- `BulletNode.cs` — muzzle flash animation (orange fading circle, 0.12s) on bullet creation
- `AudioManagerNode.cs` — sound effect manager; loads `fire.ogg`, `hit.ogg`, `death.ogg` from `res://assets/sounds/` at startup; missing files silently ignored; events: `BulletCreated` → fire, `TankHit` → hit, `TankEliminated` → death
- `SpectatorOverlayNode.cs` — spectator overlay (CanvasLayer) displayed when the local player is eliminated; shows "SPECTATING" banner and live survivor count
- `GameRenderer.cs` — `EnterSpectatorMode()` enables `Camera2D` that follows the first alive tank; events `BulletCreated`, `TankHit`, `TankEliminated` emitted for audio
- `ClientNode.cs` — on elimination: enters spectator mode instead of showing game-over screen immediately; game-over screen shown only when the match ends
- `assets/sounds/README.md` — placeholder documenting expected `.ogg` filenames

### Fixed (Phase 6 — quality corrections)

- Tests — replaced hardcoded magic numbers with `Constants` references: `TakeDamage(25)` → `TakeDamage(Constants.BulletDamage)`, `TakeDamage(50)` → `TakeDamage(Constants.BulletDamage * 2)`, health assertions use `Constants.TankMaxHealth`

### Changed (Phase 6 — quality corrections)

- `GameRoom.cs` — refactored 4 internal per-player dictionaries (`AccountId`, `InputBuffer`, `LastInputSeq`, `LastFireTick`) into a single `PlayerSession` class; reduces dictionary count from 9 to 5
- `GameRoom.cs` — fixed `ExtraAmmo` powerup cooldown reset: `_currentTick - FireCooldownTicks + 1` instead of `0` (semantically correct, safe against uint underflow at early ticks)
- `GameRoom.cs` — spawn position is now captured at elimination time and stored in `RespawnQueue`, not computed at dequeue
- `CaptureZoneRules.cs` — replaced float score accumulation (`_floatScores` dict) with integer tick increment (`state.TeamScores[team]++`) — no rounding drift
- `Constants.cs` — `CaptureZoneScoreToWin` updated from 100 to 200 (equivalent 10s at 20 TPS with integer scoring)
- `Protocol.cs` — removed nullable `?` from `ControlPointSnapshot[]` in `GameStateFull` and `GameStateDelta`; always non-null (empty array when no control points)
- `GameRoomState.cs` — `RespawnQueue` type updated to `Queue<(int, uint, Vector2)>` to include spawn position
- `MapLayout.cs` — translated French comments to English
- `Result.cs` — documented `default!` usage in `Fail` constructor

### Added (Phase 6 — tests)

- `GameRoomTests.cs` — `TryFire_WhenCooldownActive_DoesNotFire`, `TryFire_AfterCooldownExpired_Fires`, `TickBullets_BattleRoyale_FriendlyFireEnabled_HitsAnyTank`
- `GameRoomTests.cs` — `Powerup_Shield_HealsPlayer`, `Powerup_SpeedBoost_IncreasesSpeedMultiplier`
- `GameRoomTests.cs` — `Tick_WithZeroDeltaTime_DoesNotCrash`, `RemovePlayer_WhileInRespawnQueue_DoesNotCrash` (negative/edge cases)
- `GameRoomTests.cs` — `StressTests.StressTest_10Players_MaxBullets_NoException`
- `CollisionSystemTests.cs` — `IsOutOfBounds` and `BulletHitsTank` converted to `[TestCase]` parametrized tests
- `GameStateFixtures.cs` — added builder helpers: `CreateStartedRoom`, `Tank`, `AdvanceThroughLobby`, `AdvanceTicks`, `FireAndTick`
- `SerializationTests.cs` — updated `GameStateFull_RoundTrip` and `GameStateDelta_RoundTrip` to pass `ControlPoints` argument

Total tests: 136 (was 116)

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

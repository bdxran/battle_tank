# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

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

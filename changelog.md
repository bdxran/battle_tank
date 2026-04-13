# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- `GameLogic/Network/Protocol.cs` — types de messages réseau (MessageType, InputFlags, PlayerInput, GameStateFull, GameStateDelta, PlayerJoined/Eliminated/Over, ZoneUpdate)
- `GameLogic/Network/GameStateSerializer.cs` — sérialisation/désérialisation MessagePack générique
- `GameLogic/Entities/TankEntity.cs` — entité tank (position, rotation, santé, mouvement, dégâts, snapshot)
- `GameLogic/Shared/Types.cs` — types partagés : GamePhase, TankSnapshot, BulletSnapshot
- `GameLogic/Shared/Constants.cs` — constantes tank et balle (vitesse, santé max, dégâts)
- `Tests/Entities/TankEntityTests.cs` — 12 tests unitaires NUnit sur TankEntity
- `Godot/Network/ServerNetworkManager.cs` — serveur ENet basique : démarrage, connexion/déconnexion joueurs, réception PlayerInput, broadcast GameState
- `Godot/Network/ClientNetworkManager.cs` — client ENet basique : connexion au serveur, envoi PlayerInput, réception GameStateFull/Delta

### Changed

- `GameLogic/Shared/Constants.cs` — passage de `internal` à `public` (accès depuis Godot/)

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

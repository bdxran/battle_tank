# Changelog

All notable changes to this project will be documented in this file.

## [Unreleased]

## [0.0.23] - 2026-04-22

### Added
- `AppPaths` : chemins utilisateur cross-platform dans `~/Documents/BattleTank/` (`settings.cfg`, `battle_tank.db`, `crash_reports/`)
- `installer/windows/setup.iss` : wizard Inno Setup — télécharge depuis GitHub Releases, raccourcis Bureau + Menu Démarrer, uninstaller, `[InstallDelete]` pour mise à jour propre
- `scripts/install-linux.sh` : installe dans `~/.local/share/games/battle-tank/`, symlink `~/.local/bin/battle-tank`, fichier `.desktop`
- `scripts/update-linux.sh` : mise à jour automatique Linux depuis GitHub Releases (copié dans le dossier d'install)
- `UpdateChecker` : vérifie GitHub Releases au démarrage client (timeout 3s, silencieux si pas de réseau)
- `UpdateBannerNode` : bandeau en haut du menu principal quand une mise à jour est disponible
- `UpdateLauncher` : lance `BattleTank-Setup.exe /VERYSILENT` (Windows) ou `update.sh` (Linux)
- CI `release.yml` : job `installer-windows` — publie `BattleTank-Setup.exe` dans les assets GitHub Release

### Changed
- `InputSettings`, `BattleTankDbContext`, `CrashReporter` : chemins migrés vers `AppPaths` (depuis `user://` et chemins relatifs)
- `MainDispatcher._Ready()` : appelle `AppPaths.EnsureDirectoriesExist()` avant tout accès à la persistance

## [0.0.22] - 2026-04-20

### Fixed
- CI : copie des DLLs C# rendue robuste avec `find` récursif — le sous-dossier dans `.godot/mono/temp/bin/` change selon la config

## [0.0.21] - 2026-04-20

### Fixed
- CI : chemin des templates d'export corrigé vers `4.6.2.stable.mono/`

## [0.0.20] - 2026-04-20

### Fixed
- CI : image Docker changée vers `mono-4.6.2` — l'image standard ne contient pas le support .NET

## [0.0.19] - 2026-04-19

### Fixed
- CI : copie de tous les `.dll` depuis `.godot/mono/temp/bin/` dans l'archive — `BattleTank.GameLogic.dll` et dépendances manquaient

## [0.0.18] - 2026-04-19

### Fixed
- CI : `BattleTank.dll` inclus dans les archives d'export

## [0.0.17] - 2026-04-18

### Added
- `UserPreferencesRepository` : persistance du dernier nom d'utilisateur — le champ username est pré-rempli au lancement
- `CollisionSystem.ResolveTankTankCollision` : résolution physique des collisions entre tanks (push-back symétrique)
- `CountdownNode.StartCountdown(int seconds)` : durée paramétrable
- `ClientNode` : décompte de démarrage affiché en multijoueur

## [0.0.16] - 2026-04-17

### Added
- `HostSetupScreen` : sélection du mode de jeu + paramètres (durée DM/CZ, score cible CZ)
- `DeathmatchRules` / `CaptureZoneRules` : durée et score cible configurables via constructeur

## [0.0.15] - 2026-04-16

### Added
- Invincibilité 3s (60 ticks) après chaque respawn
- Kill assists : comptabilisés dans le scoreboard
- CaptureZone : compteur de captures de zones par joueur
- Scoreboard : colonne Assists + colonne Zones (CaptureZone uniquement)

### Fixed
- Spawn overlap au respawn simultané
- Spawn safe : point le plus éloigné des ennemis en vie (DM et CZ)
- CaptureZone : respawn automatique 6s après élimination
- IA : se dirige vers la zone la plus proche non contrôlée par son équipe
- HUD : timer MM:SS en DM et CZ, score kills/équipes selon le mode
- Scoreboard Tab : tableau K/D/ratio affiché pendant la partie
- Écran de fin : tableau de scores + boutons Rejouer / Menu principal
- Pause bloquée pendant le décompte — ESC ignoré tant que le countdown est actif
- Tir allié en CaptureZone corrigé (`IsFriendlyFireEnabled = false`)

## [0.0.14] - 2026-04-15

### Added
- Phase 12 — Serveur dédié piloté : admin se connecte avec mot de passe, configure le mode à distance
  - 6 nouveaux messages protocole (`AdminLoginRequest/Response`, `ServerConfigRequest/Response`, `ServerStatusRequest/Response`)
  - `ServerAdminScreen` : connexion admin → panneau config → "Jouer sur ce serveur"
- Phase 12 — Liste de serveurs : `SavedServerRepository` (JSON local) + `ServerListScreen`

### Fixed
- Modes Deathmatch et CaptureZone : crash au lancement (`Math.Abs(playerId) % n` pour les IDs bots négatifs)
- Caméra : suit le char local dans tous les modes
- IA : ne tire plus à travers les murs (vérification LOS dans `CollisionSystem.HasLineOfSight`)
- IA : ne cible plus ses alliés en modes équipes
- Couleurs tanks : joueur local = bleu, alliés = vert, ennemis = rouge
- Zone BR masquée hors Battle Royale + délai d'activation 15s

## [0.0.13] - 2026-04-14

### Added
- Phase 11 — Solo local et découverte LAN : `LocalGameNode`, `HostNode`, `LanAnnouncer`, `LanDiscovery`, `RoomBrowserScreen`
- `MainMenuScreen` : Jouer solo / Héberger / Rejoindre
- Menu pause (Escape)
- Keybindings configurables

### Fixed
- ESC pause, délai post-countdown, navigation menus
- Game loop : prévention du death spiral de l'accumulateur

## [0.0.12] - 2026-04-10

### Added
- Phase 9 — IA ennemie : `SimpleBot`, `IBot`, mode entraînement
- Phase 8 — Crash reporting : rapport structuré, UI de signalement, stockage local

## [0.0.10] - 2026-04-05

### Added
- Phase 7 — Ops : export Godot multi-plateformes, CI GitHub Actions (tests + release)

## [0.0.9] - 2026-04-01

### Added
- Phase 4 — Comptes & Progression : authentification, stats persistées (SQLite), leaderboard
- Phase 5 — Polish : tests NUnit, animations, SFX
- Phase 6 — Code review fixes

## [0.0.8] - 2026-03-25

### Added
- Phase 3 — Modes additionnels : Teams, Deathmatch, CaptureZone, powerups, respawn

## [0.0.7] - 2026-03-20

### Added
- Spectator mode, bullet flash, AudioManager

## [0.0.1-alpha] - 2026-03-01

### Added
- Phase 1 MVP : game loop 20 TPS, tank, bullets, collisions, ENet réseau
- Phase 2 Battle Royale : zone rétrécissante, éliminations, minimap

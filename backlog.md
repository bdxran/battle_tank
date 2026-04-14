# Backlog — battle-tank

## Phase 1 — Core MVP

> Objectif : Un tank peut se connecter, bouger, tirer, et être tué.

- [x] Setup projet Godot 4.x + C# (.csproj, solution, NuGet)
- [x] Serveur ENet basique (connexion/déconnexion joueurs via MultiplayerAPI)
- [x] Protocole de messages (MessagePack, types partagés GameLogic/Network)
- [x] Entité Tank (position, direction, vie, vitesse)
- [x] Boucle de jeu serveur (game loop à tick fixe 20 TPS)
- [x] Contrôle du tank (déplacement, rotation, tir)
- [x] Entité Bullet (trajectoire, collision, dommages)
- [x] Détection de collisions (tank-tank, bullet-tank, bullet-mur)
- [x] Renderer Godot (TankNode, BulletNode, carte)
- [x] HUD basique (vie, nombre de joueurs restants)
- [x] Système de salle (GameRoom) et matchmaking basique

## Phase 2 — Battle Royale Solo (1vsAll)

> Objectif : Une partie complète de BR jouable, du lobby à l'écran de victoire.

- [x] Zone de jeu rétrécissante (safe zone + zone de dégâts)
- [x] Élimination définitive (pas de respawn)
- [x] Écran de fin de partie (victoire / défaite / classement)
- [x] Minimap (joueurs visibles, zone safe)
- [x] Obstacles et murs sur la carte (couverture tactique)
- [x] Powerups (munitions, bouclier, vitesse)
- [x] Score / leaderboard en fin de partie
- [x] Lobby / salle d'attente avec compte à rebours avant début de partie
- [x] Pseudo joueur (saisi ou aléatoire)

## Phase 3 — Modes de jeu additionnels

> Objectif : Varier les plaisirs entre amis.

**Infrastructure commune :**
- [x] Sélecteur de mode en lobby (BR Solo, Teams, Deathmatch, Capture de zone)
- [x] Système de règles modulaire (`IBattleRules`)
- [x] Respawn configurable par mode (délai + position)

**Teams (2v2, 4v4) :**
- [x] Équipes avec couleur différente
- [x] Friendly fire désactivé (configurable)
- [x] Victoire par équipe (dernière équipe en vie)
- [x] Spawn groupé par équipe

**Deathmatch chrono :**
- [x] Timer de partie (durée configurable)
- [x] Respawn activé (délai court)
- [x] Score = nombre de kills
- [x] Leaderboard live en HUD
- [x] Victoire : plus de kills au chrono

**Capture de zone :**
- [x] Zones de contrôle sur la carte (3 zones)
- [x] Capture progressive (tank dans la zone = accumule des points)
- [ ] Affichage des zones contrôlées (minimap + carte) ← Godot/UI, hors périmètre GameLogic
- [x] Victoire : premier à X points ou plus de points au timer

## Phase 4 — Comptes & Progression

> Objectif : Suivre ses performances dans le temps.

- [x] Système d'authentification (inscription / connexion)
- [x] Backend persistance (SQLite via EF Core — issue #003)
- [x] Profil joueur (pseudo, avatar généré, date d'inscription)
- [x] Stats persistées : K/D, victoires, parties jouées, temps de jeu
- [x] Historique des parties (date, mode, résultat, kills)
- [x] Classement global (leaderboard par mode)

## Phase 5 — Qualité & Polish

- [x] Tests unitaires NUnit — couverture minimum 80% (GameLogic/)
- [x] Tests d'intégration protocole (sérialisation/désérialisation MessagePack)
- [x] Animations : explosion de tank, destruction (procédural via _Draw)
- [ ] Animations : tir (muzzle flash — nécessite OwnerId dans BulletSnapshot)
- [ ] Effets sonores
- [x] Feedback visuel hit (flash dommage)
- [x] Kill feed (barre latérale "X a tué Y")
- [ ] Mode spectateur pour les joueurs éliminés

## Phase 6 — Corrections qualité (code review)

> Objectif : corriger les problèmes identifiés lors du code review du 2026-04-14.

**Critique :**
- [x] Refactorer les 9 dictionnaires joueur de `GameRoom` en une classe `PlayerSession` interne
- [x] Remplacer l'accumulation float des scores dans `CaptureZoneRules` par une accumulation entière en ticks
- [x] Corriger le reset cooldown de tir (`ExtraAmmo`) : `_lastFireTick = _currentTick - FireCooldownTicks + 1`

**Majeurs :**
- [x] Capturer la position de spawn à l'élimination (pas au déqueue du respawn)
- [x] Homogénéiser le contrat de `GameRoomState` (tout readonly ou tout mutable)
- [x] Supprimer le `?` injustifié sur `ControlPointSnapshot[]` dans `Protocol.cs`
- [x] Ajouter des tests sur `GameRoom.TryFire()`
- [x] Ajouter des tests sur le friendly fire dans `TickBullets()`
- [x] Ajouter des tests d'intégration powerup (pickup → effet en jeu)
- [x] Ajouter un test de stress (10 joueurs + max bullets simultanés)

**Mineurs :**
- [x] Remplacer les magic numbers dans les tests par des constantes calculées (`Constants.TickRate * n`)
- [x] Implémenter les builders dans `GameStateFixtures.cs`
- [x] Convertir les tests de collision redondants en `[TestCase]` paramétrés
- [x] Ajouter des tests négatifs (`deltaTime <= 0`, suppression joueur pendant respawn queue)
- [x] Traduire les commentaires français dans `MapLayout.cs`
- [x] Corriger le `default!` dans `Result<T>`

## Phase 7 — Ops

- [ ] Export Godot serveur headless (Linux)
- [ ] Export Godot client (Windows/Linux/Mac)
- [ ] Monitoring & alertes (latence ENet, nombre de joueurs)
- [ ] Déploiement (plateforme cible à définir)

---

## Backlog froid — Features futures

> Idées identifiées, pas encore planifiées. À rediscuter quand les phases core sont terminées.

| Feature | Description |
|---------|-------------|
| Génération procédurale de cartes | Carte générée à chaque partie (obstacles, zones, layout) |
| Classes de tanks | Lourd (lent, résistant), Léger (rapide, fragile), Sniper (portée longue) |
| Armes variées | Mitrailleuse, obus à rebond, mines posées au sol |
| Murs destructibles | Obstacles qui peuvent être cassés à coups de canon |
| Chat en lobby | Text chat dans la salle d'attente |
| Tournois | Bracket automatique, mode compétition entre amis |
| Cosmétiques | Couleurs / skins de tank (sans avantage gameplay) |
| Replay | Revoir une partie après coup |
| Terrains variés | Boue (ralentit), eau (bloque), sable (réduit la précision) |
| Blurhash sur les bords | Sur les écrans > 1080p, remplir les bords avec un blurhash (style YouTube) plutôt que du noir uni |

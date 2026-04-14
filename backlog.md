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
- [x] Affichage des zones contrôlées (minimap + carte)
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
- [x] Animations : tir (muzzle flash — nécessite OwnerId dans BulletSnapshot)
- [x] Effets sonores
- [x] Feedback visuel hit (flash dommage)
- [x] Kill feed (barre latérale "X a tué Y")
- [x] Mode spectateur pour les joueurs éliminés

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

- [x] Export Godot serveur headless (Linux)
- [x] Export Godot client (Windows/Linux/Mac)
- [x] Monitoring & alertes (latence ENet, nombre de joueurs)
- [x] Déploiement (plateforme cible à définir)
- [x] CI GitHub Actions — tests automatiques sur chaque push/PR
- [x] Release GitHub Actions — build + export toutes plateformes + GitHub Release sur tag `v*`

## Phase 9 — Solo & IA ennemie

> Objectif : Permettre de jouer seul (entraînement ou partie solo) et combler les slots vides avec des bots IA.

**Terrain d'entraînement :**
- [x] Mode "Entraînement" sélectionnable depuis le menu principal (hors matchmaking)
- [x] Lancement immédiat sans compte à rebours ni nombre minimum de joueurs
- [x] Spawn de bots cibles (statiques ou en mouvement basique) pour s'exercer à viser/tirer
- [x] Pas de stats persistées ni de leaderboard pour ce mode
- [ ] Bouton "Rejoindre une partie" ou "Quitter" depuis l'écran d'entraînement

**IA ennemie (bot fill) :**
- [x] Interface `IBot` dans `GameLogic/` : décision de mouvement + tir à chaque tick
- [x] Implémentation `SimpleBot` : déplacement aléatoire, tir vers le joueur le plus proche
- [x] Option en lobby "Compléter avec des bots" (activée par défaut, désactivable)
- [x] Après expiration du compte à rebours, remplir les slots vides avec des bots jusqu'à `MIN_PLAYERS_TO_START`
- [x] Les bots apparaissent avec un suffixe `[BOT]` dans le HUD / kill feed
- [x] Les bots participent aux règles normales (zone, powerups, élimination)
- [x] Pas de stats enregistrées pour les kills sur bots en mode ranked

## Phase 10 — Assets & Polish artistique

> Objectif : Remplacer le rendu procédural par de vrais assets visuels et sonores.

**Visuels — sprites & animations :**
- [ ] Sprite tank joueur local
- [ ] Sprite tank adversaire (+ variante par équipe/couleur)
- [ ] Animation déplacement tank (chenilles)
- [ ] Animation rotation tourelle
- [ ] Animation tir (muzzle flash)
- [ ] Animation explosion tank
- [ ] Animation destruction tank (épave)
- [ ] Sprite bullet
- [ ] Tileset carte (sol, murs, obstacles)
- [ ] Sprite powerups (munitions, bouclier, vitesse)
- [ ] UI : icônes HUD (vie, munitions), minimap, écrans lobby / victoire / défaite

**Audio — SFX :**
- [ ] Tir (`fire.ogg`)
- [ ] Impact bullet sur tank (`hit.ogg`)
- [ ] Explosion / mort tank (`death.ogg`)
- [ ] Pickup powerup
- [ ] Compte à rebours lobby (bip)

**Audio — Musique :**
- [ ] Musique lobby
- [ ] Musique en jeu
- [ ] Jingle victoire / défaite

**Intégration code :**
- [ ] Remplacer `_Draw()` de `TankNode` par `AnimatedSprite2D`
- [ ] Remplacer `_Draw()` de `BulletNode` par `Sprite2D`
- [ ] Remplacer `DrawRect` de `WallNode` par tileset
- [ ] Brancher les fichiers `.ogg` dans `assets/sounds/`
- [ ] Ajouter musiques via `AudioStreamPlayer` dans `AudioManagerNode`

## Phase 8 — Rapport de crash

> Objectif : Permettre aux joueurs de signaler un crash facilement, avec envoi automatique par mail.

- [x] Détecter les crashs non gérés côté client (exception non catchée, signal OS)
- [x] Générer un rapport de crash structuré : stacktrace, version du jeu, OS, phase de jeu, derniers logs
- [x] UI de signalement : fenêtre modale post-crash avec bouton "Envoyer le rapport"
- [x] Envoi du rapport par mail à randy.blondiaux@contraste.com (SMTP ou service tiers)
- [x] Inclure un champ commentaire libre (optionnel) pour le joueur
- [x] Stocker le rapport localement en cas d'échec d'envoi (retry au prochain lancement)

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

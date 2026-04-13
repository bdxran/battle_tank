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
- [ ] Powerups (munitions, bouclier, vitesse)
- [ ] Score / leaderboard en fin de partie
- [ ] Lobby / salle d'attente avec compte à rebours avant début de partie
- [ ] Pseudo joueur (saisi ou aléatoire)

## Phase 3 — Modes de jeu additionnels

> Objectif : Varier les plaisirs entre amis.

**Infrastructure commune :**
- [ ] Sélecteur de mode en lobby (BR Solo, Teams, Deathmatch, Capture de zone)
- [ ] Système de règles modulaire (`IBattleRules`)
- [ ] Respawn configurable par mode (délai + position)

**Teams (2v2, 4v4) :**
- [ ] Équipes avec couleur différente
- [ ] Friendly fire désactivé (configurable)
- [ ] Victoire par équipe (dernière équipe en vie)
- [ ] Spawn groupé par équipe

**Deathmatch chrono :**
- [ ] Timer de partie (durée configurable)
- [ ] Respawn activé (délai court)
- [ ] Score = nombre de kills
- [ ] Leaderboard live en HUD
- [ ] Victoire : plus de kills au chrono

**Capture de zone :**
- [ ] Zones de contrôle sur la carte (3 zones)
- [ ] Capture progressive (tank dans la zone = accumule des points)
- [ ] Affichage des zones contrôlées (minimap + carte)
- [ ] Victoire : premier à X points ou plus de points au timer

## Phase 4 — Comptes & Progression

> Objectif : Suivre ses performances dans le temps.

- [ ] Système d'authentification (inscription / connexion)
- [ ] Backend persistance (base de données — voir issue #003)
- [ ] Profil joueur (pseudo, avatar généré, date d'inscription)
- [ ] Stats persistées : K/D, victoires, parties jouées, temps de jeu
- [ ] Historique des parties (date, mode, résultat, kills)
- [ ] Classement global (leaderboard par mode)

## Phase 5 — Qualité & Polish

- [ ] Tests unitaires NUnit — couverture minimum 80% (GameLogic/)
- [ ] Tests d'intégration protocole (sérialisation/désérialisation MessagePack)
- [ ] Animations : explosion de tank, tir, destruction
- [ ] Effets sonores
- [ ] Feedback visuel hit (flash dommage)
- [ ] Kill feed (barre latérale "X a tué Y")
- [ ] Mode spectateur pour les joueurs éliminés

## Phase 6 — Ops

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

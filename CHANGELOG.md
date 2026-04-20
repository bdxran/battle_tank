# Changelog

All notable changes to this project will be documented in this file.

## [Unreleased]

### Added
- `UserPreferencesRepository` : persistance du dernier nom d'utilisateur dans `preferences.json` (répertoire utilisateur Godot) — le champ username est pré-rempli au lancement
- `CollisionSystem.ResolveTankTankCollision` : résolution physique des collisions entre tanks (push-back symétrique) — appelé chaque tick dans `GameRoom` après le déplacement des tanks
- `CountdownNode.StartCountdown(int seconds)` : le décompte accepte maintenant un nombre de secondes en paramètre (défaut 3)
- `ClientNode` : le décompte de démarrage est maintenant affiché pour les clients en mode multijoueur (déclenché sur `GameStateFull` avec `Phase == Lobby`)

### Fixed
- `ClientNode` : en mode serveur, les touches restaient bloquées — le client n'envoyait jamais `flags=None` ; désormais l'input est envoyé chaque frame, et `InputFlags.None` est forcé quand la fenêtre perd le focus (`GetWindow().HasFocus()`)
- `GameRoom` : après un respawn, le tank héritait du dernier `InputBuffer` (directions + tir) avant la mort — `InputBuffer` est maintenant remis à `None` au moment du respawn
- `ClientNode` + `GameRenderer` : après un respawn en mode réseau, `_eliminated` restait `true` côté client — le client détecte maintenant le respawn via le `GameStateDelta` (tank local `Health > 0`) et réactive l'envoi d'input ; `GameRenderer.ExitSpectatorMode()` ajouté


- Serveur dédié : la partie ne démarrait jamais avec 1 seul humain — les bots sont maintenant ajoutés dès l'authentification du premier joueur (et non après `InProgress`) pour déclencher la transition `WaitingForPlayers → Lobby`
- `GameRoomNode.Reconfigure()` : `_botFillCount` n'était pas mis à jour par la reconfiguration admin — désormais passé via `ServerConfigRequest.BotFillCount`
- `ServerAdminScreen` : ajout d'un champ "Bots (0 = aucun)" dans le panneau de configuration admin pour contrôler le remplissage par IA

### Added
- `ServerConfigRequest` : nouveau champ `BotFillCount` (Key 5) pour configurer le nombre de bots à remplir depuis l'écran admin

### Fixed
- `ServerNode` : lecture des arguments via `OS.GetCmdlineUserArgs()` (et non `GetCmdlineArgs()`) — `--admin-password` était ignoré, le mot de passe attendu restait vide
- `ServerNode` : serveur dédié ne remplit plus les slots avec des bots (`botFillCount: 0`) — l'affichage "mode entraînement" avec bots disparaît
- `ClientNode.OnQuitRequested()` : retourne au menu principal (`_mainMenuScreen`) en session réseau distante, et à `_soloModeScreen` uniquement en solo local — précédemment bloquait sur `_soloModeScreen` dans tous les cas
- `ClientNode.OnLoginResponse()` : appelle `_hud.Show()` et `_renderer.Show()` après authentification en jeu réseau — le HUD restait caché (masqué au démarrage) pour toutes les sessions réseau, donnant une apparence "mode entraînement"
- `ClientNode.OnAdminPlayRequested()` + `LoginScreen.OnConnected()` : le bouton "Mode Entraînement" est masqué quand on joue depuis le flow admin dédié (`showTraining: false`) — évite que `_trainingMode=true` déclenche l'overlay entraînement lors du login qui suit

### Added
- **Phase 12 — Serveur dédié piloté** : un admin peut se connecter au serveur dédié avec un mot de passe et configurer le mode de jeu à distance
  - `Protocol.cs` : 6 nouveaux messages (`AdminLoginRequest/Response`, `ServerConfigRequest/Response`, `ServerStatusRequest/Response`)
  - `ServerNode` : lit `--admin-password` / `ADMIN_PASSWORD` et `--server-name` / `SERVER_NAME` au démarrage ; gère l'authentification admin et la reconfiguration de la room
  - `GameRoomNode.Reconfigure()` : reconfigure mode/durée/score/friendly-fire/code à chaud (uniquement hors partie en cours)
  - `ServerAdminScreen` : écran de connexion admin → panneau de config → "Jouer sur ce serveur"
  - `MainMenuScreen` : nouveau bouton "Configurer serveur dédié"
- **Phase 12 — Liste de serveurs** : les joueurs maintiennent une liste de serveurs favoris avec aperçu du statut avant de rejoindre
  - `SavedServerRepository` : persistance JSON dans `user_data/servers.json`
  - `ServerListScreen` : liste sauvegardée avec statut live, ajout/suppression, fiche de détail (mode, règles, joueurs) et bouton Rejoindre
  - "Rejoindre une partie" pointe désormais vers `ServerListScreen` au lieu de `RoomBrowserScreen`
- `ClientNetworkManager` : méthodes `SendAdminLogin`, `SendServerConfig`, `SendServerStatusRequest` + events `AdminLoginResponseReceived`, `ServerConfigResponseReceived`, `ServerStatusResponseReceived`
- `ServerNetworkManager` : events `AdminLoginReceived`, `ServerConfigReceived`, `ServerStatusRequested`

### Added
- HostSetupScreen : sélection du mode de jeu (BR, Deathmatch, Équipes, CaptureZone) avec paramètres dynamiques (durée pour DM/CZ, score cible pour CZ)
- DeathmatchRules / CaptureZoneRules : durée configurable via constructeur (`durationSeconds`) ; CaptureZoneRules accepte aussi `scoreToWin`
- GameRoomNode.Initialize / HostNode.Initialize : acceptent `GameMode`, `durationSeconds`, `scoreToWin` — la bonne règle est instanciée selon le mode choisi
- LanAnnouncer : diffuse le vrai mode de jeu (`mode.ToString()`) au lieu de "BattleRoyale" en dur

### Fixed
- LoginScreen : animation "Connexion en cours..." pendant l'établissement de la connexion — les boutons restent désactivés jusqu'à connexion effective, puis le label passe à "Connecté. Saisissez vos identifiants."
- HostNode : échec silencieux du démarrage serveur remplacé par un event `ServerFailed` → `HostSetupScreen` réaffichée avec le message d'erreur (ex: port déjà occupé)
- MainDispatcher : `Main.tscn` utilise désormais `MainDispatcher` qui démarre `ServerNode` si `OS.HasFeature("dedicated_server")` ou arg `--server`, sinon `ClientNode` — corrige `just run` et le build serveur GitHub
- BattleTankDbContext : clés primaires EF Core manquantes sur `PlayerAccount`, `PlayerStats`, `GameRecord` — serveur crashait au démarrage sur `EnsureCreated()`

### Added
- Invincibilité de 3s (60 ticks) après chaque respawn — tank invulnérable aux balles et à la zone (`TankEntity.TickInvincibility`)
- Kill assists : tout joueur ayant infligé des dégâts sans tuer reçoit un assist comptabilisé dans le scoreboard
- CaptureZone : compteur de captures de zones par joueur (attribué aux tanks dans le rayon au moment de la capture)
- Scoreboard : colonne Assists pour tous les modes, colonne Zones uniquement en CaptureZone (6 colonnes total)
- `PlayerInfo` : nouveaux champs `Assists` (Key 5) et `ZoneCaptures` (Key 6)

### Fixed
- Spawn overlap au respawn simultané : le spawn point est désormais calculé au moment du respawn (et non à l'élimination), chaque tank étant remis vivant avant le calcul suivant
- Spawn safe : DM et CaptureZone choisissent le point de spawn le plus éloigné de tous les ennemis en vie (`SafestSpawnPoint`)
- CaptureZone : respawn automatique 6s après élimination (comme Deathmatch)
- IA : les bots en CaptureZone se dirigent vers la zone la plus proche non contrôlée par leur équipe quand aucun ennemi n'est visible
- HUD timer : affiche le temps restant (MM:SS) en Deathmatch et CaptureZone
- HUD score : affiche "Kills: N" en Deathmatch, "Bleu X  -  Rouge Y" en CaptureZone
- Scoreboard overlay (Tab) : tableau K/D/ratio affiché pendant la partie, groupé par équipe en modes équipes
- Écran de fin : tableau de scores intégré au-dessus des boutons Rejouer/Menu
- Suivi des morts (`PlayerDeaths`) dans tous les modes — exposé dans `PlayerInfo`
- `IBattleRules.TicksRemaining` : propriété exposée sur toutes les règles, envoyée dans `GameStateFull` et `GameStateDelta`
- `GameStateFull`/`GameStateDelta` : nouveaux champs `TicksRemaining` (Key 10/7) et `TeamScores` (Key 11/8)

### Fixed
- Pause bloquée pendant le décompte de démarrage — ESC ignoré tant que le countdown est actif (`_countdownActive`)
- Tir allié en CaptureZone corrigé — `IsFriendlyFireEnabled` était `true` au lieu de `false`
- Kills/deaths/ratio des IA non comptabilisés — le check `killerId >= 0` excluait les bots (IDs négatifs) ; remplacé par `ContainsKey` seul dans tous les modes
- Scoreboard Tab vide en cours de partie — le leaderboard est maintenant rechargé depuis `LocalGameNode` à chaque ouverture du scoreboard
- Deathmatch/CaptureZone : crash silencieux au lancement corrigé — `GetSpawnPoint` utilisait `playerId % n` qui retourne un index négatif pour les IDs de bots (négatifs), bloquant `Initialize` avant `ForceStart` ; remplacé par `Math.Abs(playerId) % n`
- Caméra : suit le char du joueur local en temps réel dans tous les modes (Deathmatch, CaptureZone, Teams, BR) — `GameRenderer` active la `Camera2D` dès l'init et met à jour sa position sur chaque delta d'état
- IA : ne tire plus à travers les murs — vérification LOS (segment vs AABB) dans `CollisionSystem.HasLineOfSight`, utilisé par `SimpleBot` avant de tirer
- IA : ne cible plus ses alliés en modes équipes — `FindNearestEnemy` ignore les tanks du même `TeamId`
- Couleurs tanks : joueur local = bleu, alliés = vert, ennemis = rouge dans tous les modes (via `TankNode`, `MinimapNode`, `GameRenderer`)
- Écran de fin : remplace "Disconnecting..." par deux boutons "Rejouer" / "Menu principal" — "Rejouer" relance la même partie en solo, "Menu" retourne au menu principal
- Zone BR masquée dans les modes sans Battle Royale (Training, Deathmatch, Teams, CaptureZone) — `ZoneNode` se cache si `GameStateFull.Mode != BattleRoyale`
- Zone BR invisible au spawn en BR : délai d'activation de 15s avant apparition (`ZoneActivationDelay`) — évite les dégâts immédiats au spawn
- `ZoneController.GetSnapshot()` retourne `Radius = 0` pendant le délai d'activation pour signaler l'état inactif au client

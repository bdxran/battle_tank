# Changelog

All notable changes to this project will be documented in this file.

## [Unreleased]

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

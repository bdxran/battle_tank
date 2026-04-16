# Changelog

All notable changes to this project will be documented in this file.

## [Unreleased]

### Fixed
- Deathmatch/CaptureZone : crash silencieux au lancement corrigé — `GetSpawnPoint` utilisait `playerId % n` qui retourne un index négatif pour les IDs de bots (négatifs), bloquant `Initialize` avant `ForceStart` ; remplacé par `Math.Abs(playerId) % n`
- Caméra : suit le char du joueur local en temps réel dans tous les modes (Deathmatch, CaptureZone, Teams, BR) — `GameRenderer` active la `Camera2D` dès l'init et met à jour sa position sur chaque delta d'état
- IA : ne tire plus à travers les murs — vérification LOS (segment vs AABB) dans `CollisionSystem.HasLineOfSight`, utilisé par `SimpleBot` avant de tirer
- IA : ne cible plus ses alliés en modes équipes — `FindNearestEnemy` ignore les tanks du même `TeamId`
- Couleurs tanks : joueur local = bleu, alliés = vert, ennemis = rouge dans tous les modes (via `TankNode`, `MinimapNode`, `GameRenderer`)
- Écran de fin : remplace "Disconnecting..." par deux boutons "Rejouer" / "Menu principal" — "Rejouer" relance la même partie en solo, "Menu" retourne au menu principal
- Zone BR masquée dans les modes sans Battle Royale (Training, Deathmatch, Teams, CaptureZone) — `ZoneNode` se cache si `GameStateFull.Mode != BattleRoyale`
- Zone BR invisible au spawn en BR : délai d'activation de 15s avant apparition (`ZoneActivationDelay`) — évite les dégâts immédiats au spawn
- `ZoneController.GetSnapshot()` retourne `Radius = 0` pendant le délai d'activation pour signaler l'état inactif au client

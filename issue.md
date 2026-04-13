# Issues — battle-tank

Points ouverts, ambiguïtés ou blocages identifiés en cours de développement.

### Décisions prises

| ID | Décision | Détail |
|----|----------|--------|
| 001 | **Delta** — le serveur envoie uniquement les changements d'état depuis le dernier tick confirmé | Scalabilité : charge croît linéairement vs quadratiquement pour l'état complet. Nécessite : numéro de séquence, ACK côté client, baseline initiale à la connexion. |
| 002 | **Client-side prediction** — le client anticipe son propre mouvement, le serveur confirme/corrige | Jeu entre amis à faible latence. Lag compensation complète (rewind) reportée au backlog froid. |
| 003 | **SQLite embedded** via EF Core | Serveur tourne sur le PC d'un joueur (modèle "Minecraft à l'ancienne"), un seul serveur à la fois, zéro infra externe. |
| 004 | **Intégré au serveur Godot headless** — pas de service séparé | Le serveur est un processus headless distinct lancé sur le PC d'un joueur hôte. Ce joueur lance aussi son client séparément pour jouer. Zéro infra externe. |
| 005 | **Viewport fixe 1920×1080** — carte plus grande, caméra suit le tank, bords noirs sur grands écrans | Garantit que tous les joueurs voient la même zone. Pas de fog of war. Taille de carte à calculer une fois le sprite tank défini (ratio cible ~5×5 viewports). Blurhash sur les bords = backlog froid (polish). |
| 006 | **2 à 10 joueurs** pour tous les modes | Teams : 1v1 min → 5v5 max. BR : 2→10. Config : `MAX_PLAYERS_PER_ROOM=10`, `MIN_PLAYERS_TO_START=2`. |

---

| ID | Description | Priorité | Statut | Source |
|----|-------------|----------|--------|--------|
| 001 | Stratégie de synchronisation d'état : envoyer le delta ou l'état complet à chaque tick ? | haute | ✅ décidé | init |
| 002 | Gestion de la latence / lag compensation côté serveur | moyenne | ✅ décidé | init |
| 003 | Quel backend pour les comptes ? (SQLite embedded vs PostgreSQL) | haute | ✅ décidé | scope |
| 004 | Le serveur de comptes est-il intégré au serveur Godot ou service séparé ? | haute | ✅ décidé | scope |
| 005 | Quelle taille de carte pour le MVP ? (impact sur le design de la zone rétrécissante) | moyenne | 🟡 partiel | scope |
| 006 | Nombre de joueurs max par mode (BR Solo = 8, Teams = 8, Deathmatch = ?) | moyenne | ✅ décidé | scope |

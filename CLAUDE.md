# CLAUDE.md — Mémoire projet

This file provides guidance to Claude Code when working with code in this repository.

---

## Quick Reference

### Standards — lire avant toute implémentation

> **Règle** : avant d'implémenter ou de rédiger un plan, consulter les standards concernés.

| Standard | Consulter quand |
|----------|----------------|
| [`standards/architecture.md`](standards/architecture.md) | Structure de dossiers, séparation GameLogic/Godot, nouveaux composants |
| [`standards/network.md`](standards/network.md) | Ajout/modification de messages réseau, multijoueur |
| [`standards/csharp-code.md`](standards/csharp-code.md) | Écriture de code C# — naming, types, organisation |
| [`standards/testing.md`](standards/testing.md) | Écriture de tests |
| [`standards/error-handling.md`](standards/error-handling.md) | Gestion d'erreurs d'un composant |
| [`standards/logging.md`](standards/logging.md) | Ajout de logs |

### Équipe

Voir [`team.md`](team.md) — membres, rôles, contacts et responsabilités.

### Documentation

| Dossier | Contenu |
|---------|---------|
| [`docs/analyse/`](docs/analyse/CLAUDE.md) | Analyses fonctionnelles |
| [`docs/std/`](docs/std/CLAUDE.md) | Spécifications Techniques Détaillées |
| [`docs/adr/`](docs/adr/CLAUDE.md) | Architecture Decision Records |
| [`docs/contracts/`](docs/contracts/CLAUDE.md) | Contrats de protocole réseau (ENet/MessagePack) |

### Modules du projet

- **GameLogic** : `src/GameLogic/CLAUDE.md` *(à créer lors de l'implémentation)*
- **Godot** : `src/Godot/CLAUDE.md` *(à créer lors de l'implémentation)*

---

## Project Overview

| Champ | Valeur |
|-------|--------|
| **Nom** | battle-tank |
| **Domaine** | Jeu vidéo multijoueur |
| **Description** | Jeu battle royale multijoueur en temps réel où des tanks s'affrontent sur une carte 2D jusqu'au dernier survivant. |
| **Stack principale** | C#, Godot 4.x, ENet (UDP), .NET 8 |
| **Type** | Jeu PC (serveur dédié headless + client Godot) |

---

## Architecture

```
src/
├── GameLogic/                  → C# pur, zéro dépendance Godot
│   ├── Entities/               → Tank, Bullet, Zone
│   ├── Physics/                → Collisions, mouvement
│   ├── Network/                → Protocol, sérialisation MessagePack
│   ├── Rules/                  → GameRoom, BattleRoyaleRules, Spawn
│   └── Shared/                 → Constants, Types
├── Godot/                      → Thin wrappers Godot
│   ├── Nodes/                  → TankNode, BulletNode, GameRoomNode
│   ├── Network/                → ServerNetworkManager, ClientNetworkManager
│   ├── UI/                     → HUD, MainMenu
│   └── Renderer/               → GameRenderer
└── Tests/                      → Tests NUnit sur GameLogic uniquement
    ├── Entities/
    ├── Physics/
    └── Rules/
```

Flux réseau (ENet UDP) :
```
Client → [PlayerInput] → Serveur autoritaire → [GameStateDelta] → Tous les clients
```

---

## Development Workflow

**Toujours respecter cet ordre avant toute implémentation :**

1. **`backlog.md`** — définir ou référencer l'item avant de coder
2. **`issue.md`** — documenter les questions ouvertes, ambiguïtés ou blocages
3. **Implémenter** — écrire le code (utiliser le plan mode Claude pour les tâches complexes)
4. **`CHANGELOG.md`** — consigner la modification après implémentation (section `[Unreleased]`)

Ne pas implémenter sans que l'item soit tracé dans le backlog.
Ne pas terminer une session sans mettre à jour le changelog.

### Fichiers de suivi

| Fichier | Rôle |
|---------|------|
| `backlog.md` | Items à implémenter, organisés par phase/priorité |
| `issue.md` | Questions ouvertes, ambiguïtés, blocages identifiés |
| `changelog.md` | Historique des modifications — section `[Unreleased]` en attente de release |

---

## Build & Run

```bash
just build           # Compiler le projet C# (dotnet build)
just run             # Lancer le serveur Godot en mode headless
just dev             # Lancer l'éditeur Godot
just test            # Exécuter les tests NUnit
just test-cover      # Tests avec couverture de code
just trivy           # Scanner les vulnérabilités
```

---

## Testing

Stratégie de test :

```bash
just test            # Tests unitaires (NUnit)
just test-cover      # Couverture de code
```

Patterns :
- Tests unitaires : logique de jeu dans `GameLogic/` (collisions, méchaniques, boucle de jeu)
- Tests d'intégration : protocole réseau (sérialisation/désérialisation MessagePack)
- Pas de tests sur les nodes Godot — uniquement `GameLogic/`

---

## Commit Convention

**Conventional Commits** avec scope :

```
<type>(scope): <description>

[body optionnel]
```

**Types** : `feat`, `fix`, `docs`, `test`, `refactor`, `chore`, `perf`, `style`

**Scopes** : `game`, `network`, `physics`, `renderer`, `ui`, `rules`, `config`, `godot`

**Exemples** :
```
feat(game): add shrinking play zone mechanic
fix(physics): resolve bullet-wall collision detection
feat(renderer): add tank explosion animation
chore(config): update ENet port configuration
```

---

## Pre-commit Hooks

Installer : `pre-commit install` (après `just precommit_dependencies`)

Hooks actifs :
- Validation YAML/TOML
- Détection de fichiers volumineux
- Détection de clés privées
- Fin de fichier et whitespace
- Gitleaks (secrets)
- Conventional commits
- dotnet format (C#)

---

## Configuration

```bash
# Voir project.godot et export_presets.cfg pour la config Godot

TICK_RATE=20                 # Ticks par seconde du game loop (défaut: 20)
MAX_PLAYERS_PER_ROOM=10      # Nombre max de joueurs par salle
MIN_PLAYERS_TO_START=2       # Nombre min de joueurs pour lancer une partie
SERVER_PORT=4242             # Port ENet du serveur
```

---

## Chemins de référence

Le fichier `.path` (non versionné, basé sur `.path.example`) liste les chemins vers les projets utiles.

Dans un prompt, mentionne simplement le nom (ex: "comme dans template_project") — résoudre le chemin depuis `.path` sans le demander à l'utilisateur.

---

## Specs & Références

Workflow de spécification :
1. **Analyse** : rédiger dans `docs/analyse/[sujet].md`
2. **STD** : générer avec `/std de [sujet].md` → crée `docs/std/STD-[sujet].md`
3. **ADR** : documenter les décisions avec `/adr de [sujet].md` → crée `docs/adr/ADR-[NNN]-[sujet].md`
4. **Contrat** : maintenir `docs/contracts/` à jour après toute modification du protocole réseau

STDs et ADRs liés à ce projet :

| Document | Description | Statut |
|----------|-------------|--------|
| <!-- STD-xxx --> | <!-- Description --> | <!-- Accepté / Implémenté --> |
| <!-- ADR-001-xxx --> | <!-- Décision --> | <!-- Accepté --> |

---

## Common Operations

### Ajouter une mécanique de jeu
1. Définir le comportement dans `docs/analyse/`
2. Mettre à jour `src/GameLogic/Shared/Types.cs` si nouveaux types
3. Implémenter dans `src/GameLogic/`
4. Mettre à jour le protocole (`src/GameLogic/Network/Protocol.cs`)
5. Mettre à jour le contrat (`docs/contracts/protocol.md`)
6. Implémenter le node Godot (`src/Godot/Nodes/`)
7. Écrire les tests

### Ajouter un message réseau
1. Définir le message dans `docs/contracts/protocol.md` (contract-first)
2. Ajouter le type dans `src/GameLogic/Network/Protocol.cs`
3. Implémenter côté serveur (`src/Godot/Network/ServerNetworkManager.cs`)
4. Implémenter côté client (`src/Godot/Network/ClientNetworkManager.cs`)

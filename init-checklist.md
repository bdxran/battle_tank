# Init Checklist — battle-tank

Checklist d'initialisation du projet à partir du template.

---

## Statut : COMPLÉTÉ (2026-04-13)

---

## Étape 1 — Informations du service ✅

| Information | Valeur |
|-------------|--------|
| **Nom** | `battle-tank` |
| **Domaine** | Jeu vidéo multijoueur |
| **Description** | Jeu battle royale multijoueur temps réel — tanks sur carte 2D |
| **Stack** | C#, Godot 4.x, ENet (UDP), .NET 8 |
| **Dépendances** | Aucune (pas de BDD — état en mémoire) |
| **Port serveur** | `4242` (ENet) |

---

## Étape 2 — Standards ✅

- [x] **`standards/architecture.md`** — Séparation GameLogic/Godot, structure de dossiers
- [x] **`standards/network.md`** — ENet, serveur autoritaire, client prediction
- [x] **`standards/csharp-code.md`** — Conventions C#, naming, types
- [x] **`standards/testing.md`** — NUnit, couverture 80%, tests GameLogic
- [x] **`standards/error-handling.md`** — Result types, codes d'erreur
- [x] **`standards/logging.md`** — Serilog, JSON structuré

---

## Étape 3 — Équipe ✅

- [x] **`team.md`** — Randy (solo dev)

---

## Étape 4 — Fichiers de projet ✅

- [x] **`CLAUDE.md`** — Complet
- [x] **`backlog.md`** — Items Phase 1 définis
- [x] **`issue.md`** — Questions ouvertes initiales
- [x] **`changelog.md`** — Initialisé au 2026-04-13

---

## Étape 5 — Outillage ✅

- [x] **`justfile`** — Commandes dotnet/Godot (install, build, dev, run, test)
- [x] **`.pre-commit-config.yaml`** — dotnet format activé
- [x] **`project.godot`** — À créer lors du setup Godot

---

## Étapes manuelles restantes

1. Créer le projet Godot 4.x avec support C# (`godot --init`)
2. Créer la solution C# : `dotnet new sln -n BattleTank`
3. Créer les projets : `GameLogic`, `Godot`, `Tests`
4. Installer les packages NuGet : `MessagePack`, `Serilog`, `NUnit`
5. Configurer `nullable enable` dans chaque `.csproj`
6. Installer les hooks : `pip install pre-commit && just precommit_dependencies && pre-commit install`
7. Créer le contrat réseau : `docs/contracts/protocol.md`
8. Tester : `just build && just test`

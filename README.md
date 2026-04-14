# battle-tank

> Jeu battle royale multijoueur en temps réel : des tanks s'affrontent sur une carte 2D jusqu'au dernier survivant.

## Stack

| Composant | Technologie |
|-----------|-------------|
| Moteur | Godot 4.x |
| Langage | C# (.NET 8) |
| Réseau | ENet (UDP) via Godot MultiplayerAPI |
| Sérialisation | MessagePack |
| Tests | NUnit 4 |
| Déploiement | Export Godot (serveur headless + client) |

## Jouer

Les builds sont disponibles dans les [Releases GitHub](../../releases/latest).

- [Héberger un serveur](docs/server-setup.md) — Docker ou binaire direct
- [Installer le client Linux / macOS](docs/client-setup.md)

## Démarrage rapide

```bash
# Prérequis : just, Godot 4.x, .NET 8 SDK, pre-commit

# 1. Installer les dépendances
just install

# 2. Lancer l'éditeur Godot
just dev

# 3. Lancer le serveur en headless
just run
```

## Commandes

```bash
just install        # Restaurer les packages NuGet
just build          # Compiler le projet C#
just dev            # Ouvrir l'éditeur Godot
just run            # Lancer le serveur en mode headless
just test           # Tests NUnit
just test-cover     # Tests avec couverture
just trivy          # Scan de vulnérabilités
```

## Documentation

- [`CLAUDE.md`](CLAUDE.md) — guide complet (workflow, standards, architecture)
- [`team.md`](team.md) — équipe et contacts
- [`backlog.md`](backlog.md) — items en cours et à venir
- [`changelog.md`](changelog.md) — historique des modifications

## Protocole réseau

- `docs/contracts/protocol.md` — définition des messages ENet/MessagePack

## Architecture

Voir `CLAUDE.md` pour le diagramme d'architecture complet.

# Standards du projet

Ce dossier définit les standards que le projet doit appliquer et respecter.

**Claude Code doit consulter ces fichiers avant toute implémentation et pendant la création de plans.**

## Index

| Fichier | Domaine |
|---------|---------|
| [`architecture.md`](architecture.md) | Séparation GameLogic/Godot, structure de dossiers, patterns |
| [`network.md`](network.md) | Protocole réseau, multijoueur, client prediction |
| [`csharp-code.md`](csharp-code.md) | Conventions C#, naming, types, organisation |
| [`testing.md`](testing.md) | Stratégie de test, NUnit, couverture, patterns |
| [`error-handling.md`](error-handling.md) | Result types, exceptions, erreurs réseau |
| [`logging.md`](logging.md) | Serilog, niveaux, structured logging |

## Règle

> Avant d'implémenter une fonctionnalité ou de rédiger un plan, lire les fichiers standards concernés.
> En cas de contradiction entre un standard et une demande, le signaler explicitement.

## Stack technique

- **Moteur** : Godot 4.x
- **Langage** : C# (.NET 8)
- **Réseau** : ENet (UDP) via Godot MultiplayerAPI
- **Sérialisation** : MessagePack
- **Tests** : NUnit 4 + FluentAssertions
- **Logging** : Serilog + Microsoft.Extensions.Logging

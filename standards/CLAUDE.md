# CLAUDE.md — standards/

Ce dossier définit les standards que le projet doit appliquer et respecter.

**Règle : consulter les fichiers concernés avant toute implémentation ou plan.**

## Fichiers

| Fichier | Quand le consulter |
|---------|-------------------|
| `architecture.md` | Avant tout ajout de composant — séparation GameLogic/Godot |
| `network.md` | Avant tout ajout/modification de message réseau ou logique multijoueur |
| `csharp-code.md` | Avant d'écrire du code C# — naming, types, organisation |
| `testing.md` | Avant d'écrire des tests ou de valider une couverture |
| `error-handling.md` | Avant d'implémenter la gestion d'erreurs d'un composant |
| `logging.md` | Avant d'ajouter des logs dans le code |

## Mise à jour

Les standards évoluent avec le projet. Toute décision qui dévie d'un standard existant doit :
1. Être documentée dans `docs/adr/` (Architecture Decision Record)
2. Entraîner une mise à jour du standard concerné

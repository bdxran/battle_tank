# CLAUDE.md — docs/

Ce dossier contient toute la documentation du projet.

## Sous-dossiers

| Dossier | Contenu |
|---------|---------|
| `analyse/` | Analyses fonctionnelles — études de mécaniques de jeu |
| `std/` | Spécifications Techniques Détaillées (STD) |
| `adr/` | Architecture Decision Records (ADR) |
| `contracts/` | Contrats de protocole réseau (ENet/MessagePack) |
| `reviews/` | Rapports de review de code (générés par `/review`, `/review-changes`) |
| `pr/` | Descriptions de PR (générées par `/pr`) |

## Workflow

1. **Analyse** : `docs/analyse/[sujet].md` — décrire la mécanique ou la fonctionnalité
2. **STD** : `/std de [sujet].md` → `docs/std/STD-[sujet].md`
3. **ADR** : `/adr de [sujet].md` → `docs/adr/ADR-[NNN]-[sujet].md`
4. **Contrat** : mettre à jour `docs/contracts/protocol.md` après toute modification du protocole réseau

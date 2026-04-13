# Guide — battle-tank

## Workflow de développement

1. **Backlog** — vérifier `backlog.md` avant de commencer un item
2. **Issues** — documenter les blocages dans `issue.md`
3. **Standards** — lire les standards concernés dans `standards/`
4. **Implémentation** — utiliser `/plan` pour les tâches complexes
5. **Changelog** — mettre à jour `changelog.md` après chaque implémentation

## Structure du projet

Voir `CLAUDE.md` pour le diagramme d'architecture complet.

## Protocole WebSocket

Voir `standards/api-contracts.md` et `docs/contracts/asyncapi.yaml`.

## Tests

Voir `standards/testing.md`.

## Déploiement

```bash
# Build et run local
just build && just run

# Docker
just run-docker

# Arrêter Docker
just stop-docker
```

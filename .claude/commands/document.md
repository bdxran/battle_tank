---
description: Documenter une fonctionnalité implémentée — CLAUDE.md, docs/contracts/, docs/analyse/
---

# Documentation

Met à jour ou crée la documentation technique de la fonctionnalité implémentée.

---

## Phase 1 — Identifier ce qui doit être documenté

Lire :
- `docs/analyse/[feature].md` — pour comprendre le périmètre
- `CLAUDE.md` — pour identifier les sections à mettre à jour
- `docs/contracts/protocol.md` — pour voir si le protocole est impacté

Identifier les impacts :
- [ ] Nouvelle mécanique de jeu → section dans `docs/analyse/`
- [ ] Nouveau module ou composant → `CLAUDE.md` (section Modules)
- [ ] Nouvelle variable de configuration → `CLAUDE.md` (section Configuration) + `.env.example`
- [ ] Changement d'architecture → `CLAUDE.md` (section Architecture)
- [ ] Message réseau ajouté ou modifié → `docs/contracts/protocol.md`
- [ ] Nouvel ADR → `docs/adr/`

---

## Phase 2 — Mise à jour CLAUDE.md

Mettre à jour les sections concernées :

### Nouveaux modules
Ajouter dans la section "Modules du projet" :
```markdown
| `[module]` | `[path/CLAUDE.md]` | [Description courte] |
```

### Nouvelles variables de configuration
Ajouter dans la section "Configuration" :
```bash
[VAR_NAME]=   # [Description — valeurs possibles, défaut]
```

### Changement d'architecture
Mettre à jour le diagramme ASCII dans la section "Architecture".

---

## Phase 3 — Contrat protocole réseau (si applicable)

Si des messages ont été ajoutés ou modifiés :
- Mettre à jour `docs/contracts/protocol.md`
- Vérifier la cohérence avec `src/GameLogic/Network/Protocol.cs`
- Si breaking change : bumper la version du protocole et documenter dans le changelog

Pour utiliser le skill dédié : `/contract`

---

## Phase 4 — Changelog

Ajouter une entrée dans `changelog.md` section `[Unreleased]` :

```markdown
### Added / Changed / Fixed
- [Description de la fonctionnalité] (`[composant ou fichier principal]`)
```

---

## Handoff

À la fin, afficher :

```
Documentation mise à jour.

Étape suivante : review des changements avant merge.
Lancer /review-changes ? [O/n]
```

Si l'utilisateur valide → invoquer le skill `/review-changes`.

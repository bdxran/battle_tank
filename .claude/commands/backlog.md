---
description: Gérer le backlog du projet — ajouter, prioriser, lier aux analyses, marquer comme fait
---

# Backlog

Gère `backlog.md` de manière interactive.

---

## Phase 1 — Lire l'état actuel

```bash
cat backlog.md
```

Afficher un résumé de l'état actuel :
```
Backlog actuel :
  À faire    : N items
  En cours   : N items
  Terminé    : N items
```

---

## Phase 2 — Action demandée

Demander ou détecter l'action souhaitée :

```
Que veux-tu faire ?
  1. Ajouter un item
  2. Prioriser / réordonner
  3. Marquer un item comme en cours / terminé
  4. Lier un item à une analyse ou STD
  5. Archiver les items terminés
  6. Afficher le backlog complet
```

---

## Actions disponibles

### Ajouter un item

Collecter les informations :
- **Titre** : description courte
- **Phase / Priorité** : critique / haute / moyenne / basse
- **Type** : feat / fix / refactor / chore / docs
- **Scope** : game / network / physics / renderer / ui / rules / config / godot
- **Lien analyse** : `docs/analyse/[feature].md` (si existe)
- **Notes** : contexte, contraintes, dépendances

Format dans `backlog.md` :
```markdown
- [ ] **[type]([scope])** [Titre] — [priorité]
  - Analyse : `docs/analyse/[feature].md` (si applicable)
  - Notes : [contexte]
```

### Prioriser

Proposer un réordonnancement basé sur :
- Dépendances entre items
- Priorité déclarée
- Items bloquants d'autres items

Présenter l'ordre proposé et attendre validation avant de modifier.

### Changer de statut

- `[ ]` → `[~]` (en cours)
- `[~]` → `[x]` (terminé)
- Ajouter la date de complétion si l'item est terminé

### Lier à une analyse ou STD

Ajouter les références vers les documents existants :
```markdown
- Analyse : `docs/analyse/[feature].md`
- STD     : `docs/std/STD-[feature].md`
```

### Archiver les items terminés

Déplacer les items `[x]` vers une section `## Terminé` ou un fichier `backlog-archive.md`.

---

## Handoff contextuel

Si un item vient d'être ajouté et qu'aucune analyse n'existe encore :

```
Item ajouté au backlog.

Étape suivante : analyser la fonctionnalité.
Lancer /analyse ? [O/n]
```

Si l'utilisateur valide → invoquer le skill `/analyse`.

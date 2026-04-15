---
description: Refactorer du code existant — plan, breaking changes, tests avant/après, scope limité
---

# Refactor

Refactorise du code existant de manière sûre et contrôlée.

---

## Phase 1 — Définir le scope

Avant tout, clarifier avec l'utilisateur :

```
Quoi refactorer  : [fichier(s), composant, layer]
Objectif         : [lisibilité / performance / découplage / conformité standards / autre]
Trigger          : [dette technique / préparation feature / review / autre]
```

**Règle stricte** : le refactoring ne doit pas changer le comportement observable.
Si des changements fonctionnels sont nécessaires en parallèle → les séparer dans un autre commit.

---

## Phase 2 — État des tests avant refactoring

```bash
just test
just test-cover
```

Les tests doivent passer **avant** de commencer. Si ce n'est pas le cas → stopper et résoudre d'abord.

Noter la couverture actuelle — elle ne doit pas descendre après le refactoring.

---

## Phase 3 — Plan (mode plan)

**Utiliser le mode plan Claude (`EnterPlanMode`) avec un contexte propre.**

Le plan doit couvrir :
- Fichiers modifiés
- Changements de signatures publiques (breaking changes potentiels)
- Dépendances à mettre à jour (callers, tests)
- Ordre des modifications pour minimiser les états intermédiaires cassés
- Vérification de la séparation GameLogic/Godot (le refactoring ne doit pas introduire de couplage)

Identifier explicitement les **breaking changes** :
- Classes / méthodes renommées ou supprimées
- Signatures modifiées
- Interfaces changées

Présenter le plan à l'utilisateur et attendre validation.

---

## Phase 4 — Refactoring

Appliquer le plan dans l'ordre défini.

Après chaque étape significative :
```bash
just build
```

Règles pendant l'application :
- Ne pas ajouter de fonctionnalités non planifiées
- Ne pas corriger des bugs non liés au refactoring (noter dans `issue.md` à la place)
- Ne pas modifier les tests pour "faire passer" — si un test casse, comprendre pourquoi
- Respecter `standards/csharp-code.md` pour le code réécrit

---

## Phase 5 — Mise à jour des dépendances

Si des interfaces, signatures ou noms ont changé :
- Mettre à jour tous les callers dans GameLogic et Godot
- Vérifier que les Godot nodes (thin wrappers) s'adaptent si l'interface GameLogic a changé

---

## Phase 6 — Tests après refactoring

```bash
just test
just test-cover
```

- [ ] Tous les tests passent
- [ ] Couverture égale ou supérieure à avant (≥ 80%)
- [ ] Pas de test modifié pour masquer un problème

---

## Phase 7 — Changelog & backlog

- `changelog.md` section `[Unreleased]` : `refactor(scope): description`
- `backlog.md` : marquer l'item comme fait si applicable

---

## Handoff

À la fin, afficher :

```
Refactoring terminé.
Fichiers modifiés : N
Breaking changes  : [aucun / liste]
Tests             : ✅ (couverture : X%)

Étape suivante : review des changements.
Lancer /review-changes ? [O/n]
```

Si l'utilisateur valide → invoquer le skill `/review-changes`.

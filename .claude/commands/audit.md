---
description: Audit complet du projet — dette technique, sécurité, conformité aux standards, préparation de release
---

# Audit Complet du Projet

Effectue une revue exhaustive du projet. Usage : audit de fond, dette technique, préparation de release.

Génère un rapport dans `docs/reviews/audit-YYYY-MM-DD.md` (date actuelle). Créer le répertoire si nécessaire.

> Pour les changements récents avant un commit/merge, utiliser `/review-changes` à la place.

---

## Phase 1 — Conformité aux standards projet

Lire chaque standard dans `standards/` et auditer l'ensemble du code :

**Architecture** (`standards/architecture.md`) :
- Séparation GameLogic / Godot stricte (aucun `using Godot;` dans GameLogic)
- Nodes Godot sont des thin wrappers — logique dans GameLogic uniquement
- Répertoires respectés (Entities, Physics, Rules, Network, Shared)

**C# Code** (`standards/csharp-code.md`) :
- Naming conventions (PascalCase types, _camelCase fields, camelCase locals)
- Types appropriés : `record` immutable, `readonly struct` math, `class` mutable entities
- Nullable refs activé — pas de warnings ignorés
- Pas de LINQ dans le game loop (hot paths)

**Réseau** (`standards/network.md`) :
- Architecture serveur autoritaire respectée
- Messages définis dans `Protocol.cs` avant utilisation
- Sérialisation MessagePack correcte
- Pas de logique de jeu côté client

**Error Handling** (`standards/error-handling.md`) :
- `Result<T>` dans GameLogic pour les erreurs métier
- Pas d'exceptions silencieuses dans le game loop
- Exceptions tolérées uniquement en init Godot

**Logging** (`standards/logging.md`) :
- `ILogger<T>` via injection — jamais `GD.Print()` ou `Console.WriteLine()` dans GameLogic
- Structured logging avec templates (pas d'interpolation de chaînes)
- Niveaux corrects par context

**Tests** (`standards/testing.md`) :
- Couverture ≥ 80% sur GameLogic
- Tests uniquement sur GameLogic, jamais sur Godot nodes
- NUnit 4 + FluentAssertions, pattern Arrange/Act/Assert

---

## Phase 2 — Architecture & design

- Séparation GameLogic / Godot : aucune dépendance Godot dans GameLogic
- Boucle de jeu : tick rate 20 TPS, pas de logique hors GameLogic
- Protocol : messages définis dans `Protocol.cs`, séquence Client → Serveur → Clients respectée
- Pas de dépendances circulaires entre couches

---

## Phase 3 — Sécurité & fiabilité

```bash
just trivy    # scan vulnérabilités
```

- Validation des inputs serveur : jamais faire confiance au client
- Secrets et credentials : aucun hardcodé dans le code
- Graceful shutdown implémenté
- Game loop : exceptions catchées par entité, jamais de crash global
- Désérialisation réseau : log + ignore en cas d'échec (pas de crash)

---

## Phase 4 — Performance

- Hot paths (game loop) : pas de LINQ, pas d'allocations inutiles
- Collections : `IReadOnlyList<T>` préféré, `Dictionary<K,V>` pour lookups
- Tick rate respecté (20 TPS = 50ms/tick)
- Interpolation client : 100ms de delay, pas de jank

---

## Phase 5 — Tests & couverture

```bash
just test
just test-cover
```

- Couverture globale ≥ 80% sur GameLogic
- Physics, Rules, Entities couverts en priorité
- Tests indépendants (pas d'ordre imposé)
- Cas d'erreur testés (Result<T> failure paths)

---

## Phase 6 — Workflow & documentation

- `backlog.md` : items en cours vs code présent
- `changelog.md` : entrées `[Unreleased]` complètes
- `docs/contracts/protocol.md` : synchronisé avec `Protocol.cs`
- `issue.md` : blocages connus tracés
- `CLAUDE.md` : sections Modules, Architecture à jour

---

## Phase 7 — Rapport

Générer `docs/reviews/audit-YYYY-MM-DD.md` :

```markdown
# Audit Complet — [Date]

## Résumé Exécutif

- **Score global** : X/100
- **Couverture tests** : X%
- **Vulnérabilités** : critique:N haute:N moyenne:N
- **Top 3 problèmes critiques** : ...

## Scores par domaine

| Domaine | Score | Statut |
|---------|-------|--------|
| Conformité standards | /20 | ✅/⚠️/❌ |
| Architecture GameLogic/Godot | /20 | ✅/⚠️/❌ |
| Sécurité & fiabilité | /20 | ✅/⚠️/❌ |
| Tests | /20 | ✅/⚠️/❌ |
| Performance | /10 | ✅/⚠️/❌ |
| Documentation | /10 | ✅/⚠️/❌ |

## Recommandations

### Critique
| # | Problème | Fichier:ligne | Effort | Standard |
|---|----------|---------------|--------|----------|

### Haute priorité
| # | Problème | Fichier:ligne | Effort | Standard |
|---|----------|---------------|--------|----------|

### Moyenne priorité
| # | Problème | Fichier:ligne | Effort | |
|---|----------|---------------|--------|--|

## Plan d'action

- **Semaine 1** : Items critiques
- **Semaine 2** : Items haute priorité + tests
- **Semaine 3** : Items moyenne priorité + documentation

## Annexes
- Résultats scan sécurité (trivy)
- Statistiques couverture NUnit
```

---

**Règles** :
- Prioriser les recommandations par ROI (impact/effort)
- Inclure fichier:ligne pour chaque issue
- Chaque recommandation référence le standard concerné si applicable
- Toujours sauvegarder le rapport — ne jamais afficher uniquement dans le terminal

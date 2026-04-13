# Review Changes Command

Analyse les nouveaux développements (commits récents ou changements non commités) et effectue une revue complète de leur qualité, pertinence, sécurité et conformité aux bonnes pratiques.

## Objectif

Vérifier que les nouveaux développements respectent les standards du projet AVANT le commit/merge, en identifiant les problèmes potentiels et en s'assurant que les tests sont présents.

**IMPORTANT** : Cette commande doit TOUJOURS générer un rapport écrit dans `docs/reviews/review-changes-YYYY-MM-DD_HH-MM.md` au format markdown complet. Le rapport doit suivre la structure définie dans les sections ci-dessous.

## Phase 1 : Détection des Changements

### 1.1 Identifier les Changements à Analyser

Demande à l'utilisateur ce qu'il veut analyser :
- **Option 1** : Changements non commités (working directory + staged)
- **Option 2** : Dernier commit
- **Option 3** : N derniers commits
- **Option 4** : Différence avec une branche (ex: main, develop)
- **Option 5** : Pull Request spécifique

```bash
# Option 1: Changements non commités
git diff HEAD
git diff --cached  # Staged changes

# Option 2: Dernier commit
git show HEAD

# Option 3: N derniers commits
git log -n 5 --pretty=format:"%h - %s" --stat

# Option 4: Diff avec branche
git diff main...HEAD

# Option 5: PR (via gh CLI)
gh pr diff 123
```

### 1.2 Extraire les Fichiers Modifiés

Liste tous les fichiers modifiés avec leur type de changement :
```bash
git diff --stat HEAD
git diff --name-status HEAD
```

Catégorise les changements :
- 🆕 Nouveaux fichiers (A)
- ✏️ Fichiers modifiés (M)
- 🗑️ Fichiers supprimés (D)
- 📦 Fichiers renommés (R)

## Phase 2 : Analyse de Pertinence

### 2.1 Contexte et Justification

Pour chaque changement, vérifie :
- [ ] **Objectif clair** : Le changement a-t-il un but précis ?
- [ ] **Scope approprié** : Le changement est-il trop large ou bien ciblé ?
- [ ] **Cohérence** : Le changement est-il cohérent avec l'architecture ?
- [ ] **CLAUDE.md alignment** : Est-ce documenté et aligné avec les standards ?

**Questions à poser** :
```markdown
### Pertinence du Changement

**Quel problème résout ce changement ?**
- [ ] Bug fix clairement identifié
- [ ] Nouvelle fonctionnalité documentée
- [ ] Refactoring justifié
- [ ] Optimisation avec métriques
- [ ] ⚠️ Changement sans justification claire

**Est-ce le bon endroit pour ce changement ?**
- [ ] Changement dans la bonne layer (controller/service/repository)
- [ ] Fichier approprié
- [ ] Package/module correct
- [ ] ⚠️ Logique métier dans le controller
- [ ] ⚠️ HTTP status dans le service/repository

**Scope approprié ?**
- [ ] Changement focalisé sur un objectif
- [ ] ⚠️ Changement trop large (multiple concerns)
- [ ] ⚠️ Refactoring non lié au changement principal
```

### 2.2 Breaking Changes

Détecte les breaking changes potentiels :
- [ ] Modification de signatures de fonctions publiques
- [ ] Changement de schéma de base de données
- [ ] Modification de contrats API (OpenAPI/Swagger)
- [ ] Changement de format de messages/events
- [ ] Suppression de variables d'environnement

**Si breaking change détecté** :
```markdown
⚠️ **BREAKING CHANGE DÉTECTÉ**

**Type** : API Contract Change
**Fichier** : `path/to/file`
**Changement** : Description du changement

**Impact** :
- Clients API doivent être mis à jour
- Migration de base de données requise

**Recommandations** :
1. Versionner l'API (v1 → v2)
2. Période de dépréciation pour v1
3. Documentation de migration
```

## Phase 3 : Analyse de Code

### 3.1 Qualité du Code

Pour chaque fichier modifié, vérifie :

#### Best Practices
- [ ] **Nommage** : Variables, fonctions, types suivent les conventions
- [ ] **Erreurs** : Gestion appropriée des erreurs (wrapping, checking)
- [ ] **Context** : Propagation correcte du context
- [ ] **Defer/Cleanup** : Utilisation appropriée (close, unlock, cancel)
- [ ] **Goroutines/Async** : Pas de leak, gestion correcte

#### Code Smells
- [ ] **Duplication** : Code dupliqué (DRY violation)
- [ ] **Complexité** : Fonctions trop complexes (cyclomatic > 15)
- [ ] **Long methods** : Fonctions > 50 lignes
- [ ] **God objects** : Structs/classes avec trop de responsabilités
- [ ] **Magic numbers** : Constantes hardcodées
- [ ] **Deep nesting** : Plus de 3 niveaux d'indentation

#### Architecture Compliance
- [ ] **Layer separation** : Pas de skip de layer (controller → dao direct)
- [ ] **HTTP status codes** : Uniquement dans les controllers
- [ ] **Business logic** : Uniquement dans les services
- [ ] **Data access** : Uniquement dans les repositories/DAOs
- [ ] **Error mapping** : Domain errors → HTTP dans controller

### 3.2 Performance

Identifie les problèmes de performance :
- [ ] **N+1 queries** : Requêtes en boucle
- [ ] **Missing indexes** : Queries sur colonnes non indexées
- [ ] **Allocations** : Allocations inutiles dans hot paths
- [ ] **Resource leaks** : Connexions, fichiers non fermés

### 3.3 Sécurité

Scan les vulnérabilités potentielles :

#### Injection Vulnerabilities
- [ ] **SQL Injection** : Queries non paramétrées
- [ ] **Command Injection** : Utilisation de shell commands avec input user
- [ ] **Path Traversal** : Manipulation de paths sans validation

#### Data Exposure
- [ ] **Secrets hardcodés** : Passwords, API keys dans le code
- [ ] **Logs sensibles** : Données personnelles dans les logs
- [ ] **Stack traces** : Exposition de stack traces en production

```bash
# Scan secrets dans les changements
git diff HEAD | grep -i "password\|apikey\|secret\|token"
```

#### Authentication & Authorization
- [ ] **Missing auth checks** : Endpoints sans vérification
- [ ] **IDOR** : Accès direct aux ressources sans validation
- [ ] **Input validation** : Inputs non validés

## Phase 4 : Tests Coverage

### 4.1 Tests Existants pour les Changements

Pour chaque fichier modifié, vérifie si des tests existent et les exécute.

### 4.2 Coverage des Nouvelles Fonctions

Liste toutes les nouvelles fonctions et vérifie si elles sont testées.

**Rapport de coverage** :
```markdown
### Coverage des Changements

#### Nouveaux Fichiers
- ✅ `path/to/new_file` → test file exists (85% coverage)
- ❌ `path/to/other_file` → **AUCUN TEST**

#### Fonctions Ajoutées
- ✅ `ServiceName.Method` → Testé (N cas)
- ❌ `ServiceName.OtherMethod` → **NON TESTÉ** ⚠️ CRITIQUE
```

### 4.3 Qualité des Tests Ajoutés

- [ ] Cas de succès couvert
- [ ] Tous les cas d'erreur couverts
- [ ] Edge cases inclus
- [ ] Table-driven tests utilisés (si multiple cas)
- [ ] Assertions appropriées
- [ ] Pas de tests flaky (time-dependent)

## Phase 5 : Documentation

### 5.1 Code Documentation

- [ ] Fonctions publiques ont des doc comments
- [ ] Paramètres et return values décrits

### 5.2 API Documentation

Si modification de l'API :
- [ ] OpenAPI spec mise à jour
- [ ] Nouveaux endpoints documentés
- [ ] Exemples de requête/réponse

### 5.3 CLAUDE.md Updates

- [ ] Nouvelles variables d'environnement documentées
- [ ] Architecture mise à jour si changement

## Phase 6 : Conformité aux Standards

### 6.1 Linters & Static Analysis

```bash
# Exécute les linters sur fichiers modifiés
# Adapter selon le langage/tooling du projet
just precommit_dependencies 2>/dev/null || true
pre-commit run --all-files 2>/dev/null || true
```

### 6.2 Contract Validation

Si l'API a changé, valider le contrat OpenAPI si disponible.

## Phase 7 : Build & Tests

### 7.1 Compilation

```bash
just build
```

### 7.2 Tests Suite

```bash
just test
```

## Phase 8 : Rapport de Review

**TOUJOURS créer** un fichier de rapport complet dans `docs/reviews/review-changes-YYYY-MM-DD_HH-MM.md`.

**Étapes** :
1. Créer le répertoire `docs/reviews/` s'il n'existe pas
2. Créer le fichier avec la date et heure actuelles
3. Générer le rapport complet selon le template ci-dessous
4. Informer l'utilisateur du chemin du rapport créé

### Structure du Rapport

```markdown
# Review des Changements - [Date]

## 📊 Statistiques
- **Fichiers modifiés** : N
  - 🆕 Nouveaux : X
  - ✏️ Modifiés : Y
  - 🗑️ Supprimés : Z
- **Lignes** : +X / -Y
- **Commits analysés** : N

## 🎯 Score Global : X/10

| Critère | Score | Commentaire |
|---------|-------|-------------|
| Pertinence | /10 | ... |
| Qualité code | /10 | ... |
| Sécurité | /10 | ... |
| Tests | /10 | ... |
| Documentation | /10 | ... |
| Performance | /10 | ... |

## 🚨 Issues Critiques
## ⚠️ Issues Importantes
## ✅ Points Positifs
```

### Checklist de Validation

```markdown
## ✅ Checklist Avant Merge

### Code Quality
- [ ] Tous les linters passent
- [ ] Pas de code smell critique
- [ ] Pas de duplication excessive

### Sécurité
- [ ] Aucune vulnérabilité critique
- [ ] Input validation en place
- [ ] Pas de secrets hardcodés

### Tests
- [ ] Coverage > 80% pour nouveaux fichiers
- [ ] Toutes les nouvelles fonctions testées

### Documentation
- [ ] Doc comments pour exports
- [ ] OpenAPI spec à jour si applicable
- [ ] CLAUDE.md mis à jour si nécessaire

### Architecture
- [ ] Layer separation respectée
- [ ] Pas de breaking change non documenté

### Build & Deploy
- [ ] Build réussit
- [ ] Tests passent
```

### Actions Recommandées

```markdown
## 🎯 Actions à Réaliser (Priorisées)

### 🚨 Critique (À faire AVANT merge)
1. **[Action]** (Xh) - Fichier : `path/to/file:line`

### ⚠️ Important (À faire cette semaine)
2. **[Action]** (Xh)

### 📝 Nice to Have (Backlog)
3. **[Action]** (Xh)
```

---

**NE JAMAIS** :
- Afficher seulement le rapport dans le terminal sans le sauvegarder
- Créer un rapport partiel ou incomplet

**TOUJOURS** :
- Créer un rapport écrit complet et détaillé
- Utiliser le format markdown pour la lisibilité
- Inclure des exemples de code pour les problèmes identifiés
- Fournir des estimations d'effort pour chaque action
- Donner une recommandation finale claire (APPROVE / APPROVE AVEC CONDITIONS / REFUSE)

---

**Note** : Cette review doit être effectuée AVANT chaque merge vers main.

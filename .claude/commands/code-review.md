# Code Review Command

Effectue une revue de code complète et approfondie du projet avec les objectifs suivants.

Génère un rapport dans `docs/reviews/review-full-YYYY-MM-DD.md` (utiliser la date actuelle). Créer le répertoire `docs/reviews/` s'il n'existe pas.

## 1. PERFORMANCE & OPTIMISATION

### Base de données
- Analyse les requêtes N+1 et opportunités d'eager loading
- Vérifie l'utilisation des indexes (composite, unique)
- Identifie les requêtes lentes ou non optimisées
- Analyse les transactions et leur portée
- Vérifie l'utilisation appropriée des context timeouts
- Vérifie les opportunités de batch operations

### Concurrence & Async
- Identifie les race conditions
- Vérifie la gestion des goroutines/threads et patterns de synchronisation
- Analyse les patterns de concurrence (mutex, wait groups, channels)
- Vérifie la gestion du backpressure et des queues

### Mémoire & GC
- Identifie les allocations excessives (profiling)
- Analyse l'utilisation de pointeurs vs valeurs
- Vérifie les fuites potentielles (goroutines, connexions)
- Optimise les structures de données

### Cache & Recherche
- Vérifie la stratégie de cache (invalidation, TTL)
- Analyse les patterns de requêtes de recherche
- Identifie les opportunités d'optimisation

## 2. ARCHITECTURE & DESIGN

### Patterns & Principes
- Vérifie conformité SOLID, DRY, KISS
- Analyse la séparation des responsabilités (Controller/Service/Repository)
- Vérifie le Repository Pattern et Dependency Injection
- Identifie le couplage fort et opportunités de découplage
- Vérifie l'utilisation appropriée des interfaces

### Structure du Code
- Vérifie l'organisation des packages/modules
- Analyse les dépendances entre modules
- Identifie les dépendances circulaires
- Vérifie la cohérence de l'architecture

### Error Handling
- Vérifie l'utilisation des erreurs personnalisées
- S'assure que les status HTTP sont UNIQUEMENT dans les controllers
- Analyse la propagation des erreurs dans les layers
- Vérifie la corrélation des logs

## 3. SÉCURITÉ & FIABILITÉ

### Scan de Vulnérabilités
- Exécute les outils de scan disponibles (`just trivy`, `govulncheck ./...` si applicable)
- Vérifie les versions des dépendances

### Pratiques de Sécurité
- Vérifie tous les timeouts (HTTP, DB, context)
- Analyse la validation des inputs
- Vérifie la gestion des secrets et credentials
- Identifie les risques OWASP Top 10
- Vérifie la configuration CORS

### Resilience
- Analyse les retry policies et circuit breakers
- Vérifie la gestion du graceful shutdown
- Identifie les single points of failure
- Vérifie la gestion des health checks

## 4. STANDARDS & CONVENTIONS

### Code Quality
- Exécute les linters disponibles
- Vérifie les conventions de nommage
- Identifie les code smells
- Vérifie conformité aux conventions du langage

### Documentation
- Vérifie les doc comments (exported functions/types)
- S'assure de l'alignement avec le Swagger/OpenAPI si applicable
- Vérifie la documentation des error codes
- Compare avec les guidelines de CLAUDE.md

### Tests
- Analyse la couverture de tests
- Identifie les fonctions non testées
- Vérifie la qualité des tests (table-driven, mocks appropriés)

## 5. CONTRACT & API

### OpenAPI Validation
- Vérifie la cohérence spec vs implémentation
- Identifie les endpoints non documentés
- Analyse les modèles de requête/réponse

### Versioning & Breaking Changes
- Identifie les breaking changes potentiels
- Vérifie la gestion des versions d'API

## 6. FONCTIONNALITÉS

### Logique Métier
- Vérifie la correctness des règles métier
- Analyse les edge cases non gérés
- Valide les transformations de données

### Intégrations
- Vérifie les appels aux services externes
- Analyse la gestion des erreurs d'intégration
- Vérifie les timeouts et retry policies

## 7. RAPPORT FINAL

Génère un rapport détaillé dans `docs/reviews/review-full-YYYY-MM-DD.md` avec :

### Résumé Exécutif
- Score global (/100)
- Métriques clés : couverture tests, vulnérabilités, dette technique
- Top 5 problèmes critiques
- Gains de performance estimés

### Recommandations Détaillées
Pour chaque recommandation :
- **Priorité**: Critique / Haute / Moyenne / Basse
- **Catégorie**: Performance / Sécurité / Architecture / Standards
- **Localisation**: Fichier:ligne
- **Problème**: Description détaillée
- **Impact**: % de gain estimé (performance, maintenabilité)
- **Effort**: Estimation en heures
- **Code avant/après**: Exemples concrets
- **Risques**: Effets de bord potentiels

### Plan d'Implémentation (3 semaines)
- **Semaine 1**: Items critiques et haute priorité
- **Semaine 2**: Items moyenne priorité + tests
- **Semaine 3**: Refactoring et optimisations

### Métriques
- Nombre total de recommandations par priorité
- Temps total estimé
- Gains de performance cumulés
- Réduction de la dette technique

### Checklist d'Implémentation
- [ ] Liste actionnable de tous les items
- [ ] Tests à ajouter/améliorer
- [ ] Documentation à compléter

### Annexes
- Résultats des linters
- Résultats des scans de sécurité
- Statistiques de couverture de tests
- Profiling data (si disponible)

---

**Note**: Cette revue doit être complète mais pragmatique. Priorise les recommandations avec le meilleur ROI (impact/effort). Inclus toujours du code concret et actionnable.

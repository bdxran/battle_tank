# Optimize Command

Effectue une analyse approfondie des performances et propose des optimisations concrètes pour le projet.

## Objectif

Identifier et implémenter les optimisations avec le meilleur ROI (Retour sur Investissement) en termes de performance, coût d'infrastructure, et expérience utilisateur.

## Phase 1 : Profiling & Analyse

### 1.1 Database Performance
- Analyse les requêtes les plus lentes
- Identifie les N+1 queries avec exemples concrets
- Vérifie l'utilisation des indexes (EXPLAIN ANALYZE)
- Propose des indexes composites optimaux
- Analyse les transactions et leur durée
- Vérifie les opportunités de batch operations

### 1.2 Memory & CPU Profiling
```bash
# Génère des profiles si disponible pour le langage du projet
# Go:
go test -cpuprofile=cpu.prof -memprofile=mem.prof -bench=.
go tool pprof -http=:8080 cpu.prof
```
- Identifie les allocations excessives
- Analyse les hotspots CPU
- Vérifie les resource leaks potentiels

### 1.3 HTTP/API Performance
- Analyse les temps de réponse par endpoint
- Identifie les endpoints lents (p95, p99)
- Vérifie l'utilisation de compression (gzip)
- Analyse les opportunités de caching
- Vérifie la configuration CORS

### 1.4 Async / Workers Performance (si applicable)
- Analyse le throughput actuel
- Identifie les bottlenecks dans le processing
- Propose des optimisations de batching
- Analyse les retry patterns et leur impact

## Phase 2 : Optimisations Prioritaires

Pour chaque optimisation proposée :

### Format de Recommandation
```markdown
#### [PRIORITY] Optimisation Title
**Fichier**: `path/to/file:line`
**Impact**: +X% throughput / -Y% latency / -Z% memory
**Effort**: N heures
**Risque**: Faible/Moyen/Élevé

**Problème actuel**:
```
// Code actuel non optimisé
```

**Solution proposée**:
```
// Code optimisé avec explications
```

**Gains mesurables**:
- Throughput: +X%
- Latency: -Y ms
- Memory: -Z MB
- CPU: -W%

**Tests requis**:
- [ ] Tests unitaires
- [ ] Tests de charge
- [ ] Validation en staging
```

### Catégories d'Optimisations

#### A. Database (Priorité: Critique)
1. **Ajout d'indexes manquants**
2. **Optimisation des requêtes N+1**
3. **Batch operations pour les inserts/updates**
4. **Connection pooling optimization**
5. **Query result caching**

#### B. Memory (Priorité: Haute)
1. **Réduction des allocations dans hot paths**
2. **Utilisation de pools pour objets réutilisables**
3. **Optimisation des structures de données**
4. **String concatenation optimization**

#### C. Concurrency (Priorité: Haute)
1. **Worker pools pour traitement parallèle**
2. **Optimisation des goroutines/threads (éviter création excessive)**
3. **Buffering approprié des queues**
4. **Context propagation optimization**

#### D. API/HTTP (Priorité: Moyenne)
1. **Response compression (gzip)**
2. **HTTP/2 optimization**
3. **Connection keep-alive tuning**
4. **Request body size limits**

#### E. Caching (Priorité: Moyenne)
1. **Caching pour requêtes fréquentes**
2. **In-memory cache pour données statiques**
3. **Cache invalidation strategy**
4. **Distributed cache si applicable**

## Phase 3 : Benchmarking

### 3.1 Benchmarks
Crée ou améliore les benchmarks pour les fonctions critiques.

### 3.2 Load Testing
Propose un plan de load testing :
- Outils : `vegeta`, `k6`, `locust` pour HTTP
- Scenarios réalistes (peak hours, burst traffic)

### 3.3 Métriques Baseline
Documente les métriques actuelles (baseline) :
- Throughput API: X req/sec
- Latency p95/p99: Y ms
- Memory usage: W MB
- CPU usage: V%

## Phase 4 : Rapport d'Optimisation

Génère un rapport dans `docs/reviews/optimize-YYYY-MM-DD.md` (créer `docs/reviews/` si besoin) avec :

### Résumé Exécutif
- **Gains totaux estimés**: +X% performance globale
- **Coût infrastructure économisé**: Y€/mois (si applicable)
- **Top 3 optimisations critiques**
- **ROI par optimisation**

### Plan d'Implémentation (2 semaines)

**Semaine 1 : Quick Wins**
- Optimisations à faible effort, haut impact
- Ajout d'indexes critiques
- Batch operations
- Estimated gain: +30% performance

**Semaine 2 : Optimisations Structurelles**
- Refactoring pour worker pools
- Caching layer
- Query optimization
- Estimated gain: +50% performance additionnel

### Checklist d'Implémentation
- [ ] Backup des métriques baseline
- [ ] Implémentation optimisation #1
- [ ] Benchmarking & validation
- [ ] Deployment staging
- [ ] Load testing
- [ ] Monitoring & rollback plan
- [ ] Documentation

### Métriques de Succès
Définir les KPIs à atteindre :
- [ ] Throughput API: > X req/sec
- [ ] Latency p95: < Y ms
- [ ] Memory: < W MB
- [ ] CPU: < V%
- [ ] Error rate: < 0.1%

### Risques & Mitigations
- **Risque 1**: Description
  - Mitigation: Plan d'action
- **Risque 2**: Description
  - Mitigation: Plan d'action

---

**Note**: Toutes les optimisations doivent être validées par des benchmarks avant et après. Déployer progressivement si possible.

# Security Audit Command

Effectue un audit de sécurité complet du projet selon les standards OWASP et les best practices.

## Objectif

Identifier et corriger les vulnérabilités de sécurité, renforcer la posture de sécurité de l'application, et assurer la conformité avec les standards de l'industrie.

## Phase 1 : Scan Automatisé

### 1.1 Scan de Vulnérabilités
```bash
# Adapter selon le langage/tooling du projet

# Go:
govulncheck ./...
just trivy

# Node.js:
npm audit

# Python:
pip-audit

# Static analysis security testing
gosec ./...         # Go
semgrep --config=auto .  # Multi-language
```

Analyse les résultats et documente :
- Vulnérabilités critiques (CVE)
- Dépendances obsolètes
- Licenses incompatibles

### 1.2 Code Analysis
```bash
# Linters de sécurité selon le langage
golangci-lint run --enable=gosec,gocritic  # Go
```

## Phase 2 : OWASP Top 10 Analysis

### A01:2021 - Broken Access Control
- [ ] Vérifie l'authentification (si applicable)
- [ ] Analyse les autorisations par endpoint
- [ ] Identifie les IDOR (Insecure Direct Object Reference)
- [ ] Vérifie les contrôles d'accès dans les repositories/DAOs

**Exemple** :
```go
// Vérifier que l'utilisateur a accès à la ressource
if !hasAccess(userID, resourceID) {
    return ErrForbidden
}
```

### A02:2021 - Cryptographic Failures
- [ ] Vérifie le chiffrement des données sensibles en transit (TLS)
- [ ] Analyse la gestion des secrets (DB password, API keys)
- [ ] Vérifie qu'aucun secret n'est hardcodé dans le code
- [ ] Valide la configuration TLS (TLS 1.2+)
- [ ] Vérifie le stockage sécurisé des credentials

**Scan du code** :
```bash
grep -rn "password\s*=\s*\"" .
grep -rn "apikey\s*=\s*\"" .
grep -rn "secret\s*=\s*\"" .
```

### A03:2021 - Injection
- [ ] Vérifie les requêtes SQL (paramétrage)
- [ ] Analyse les inputs non validés
- [ ] Vérifie la validation des contrats API
- [ ] Identifie les command injection (exec, shell commands)
- [ ] Vérifie l'échappement des données dans les logs

**Points d'attention** :
- JSON unmarshaling / deserialization
- External API calls
- File path manipulation
- Query injection (search engines)

### A04:2021 - Insecure Design
- [ ] Vérifie les patterns de sécurité (defense in depth)
- [ ] Analyse les threat models
- [ ] Vérifie les retry policies et rate limiting
- [ ] Identifie les single points of failure
- [ ] Analyse la stratégie de backup et recovery

### A05:2021 - Security Misconfiguration
- [ ] Vérifie la configuration CORS (`ALLOWED_ORIGINS`)
- [ ] Analyse les headers de sécurité HTTP
- [ ] Vérifie la configuration TLS/SSL
- [ ] Identifie les endpoints exposés non intentionnellement
- [ ] Vérifie les permissions des fichiers de config
- [ ] Analyse la configuration des containers Docker

**Headers de sécurité à vérifier** :
```
X-Content-Type-Options: nosniff
X-Frame-Options: DENY
X-XSS-Protection: 1; mode=block
Strict-Transport-Security: max-age=31536000
Content-Security-Policy: default-src 'self'
```

### A06:2021 - Vulnerable Components
- [ ] Liste toutes les dépendances
- [ ] Identifie les dépendances non maintenues
- [ ] Propose des mises à jour de sécurité
- [ ] Vérifie la chaîne d'approvisionnement (supply chain)

### A07:2021 - Authentication Failures
- [ ] Vérifie l'implémentation d'authentification
- [ ] Analyse les sessions et timeouts
- [ ] Vérifie la gestion des tokens expirés
- [ ] Identifie les endpoints non protégés
- [ ] Vérifie la rotation des credentials

### A08:2021 - Software and Data Integrity Failures
- [ ] Vérifie la signature des artifacts (Docker, packages)
- [ ] Vérifie les checksums des dépendances
- [ ] Identifie les risques de tampering

### A09:2021 - Security Logging Failures
- [ ] Vérifie que tous les events de sécurité sont loggés
- [ ] Vérifie qu'aucune donnée sensible n'est loggée
- [ ] Vérifie les alertes de sécurité

**Events à logger** :
- Authentication attempts (success/failure)
- Authorization failures
- Input validation failures
- Rate limit violations

### A10:2021 - Server-Side Request Forgery (SSRF)
- [ ] Identifie les calls HTTP sortants
- [ ] Vérifie la validation des URLs
- [ ] Analyse les redirects
- [ ] Vérifie les allowlists pour external services

## Phase 3 : Security Best Practices

### 3.1 Input Validation
- [ ] Vérifie que tous les inputs sont validés
- [ ] Analyse les tailles max des payloads
- [ ] Vérifie les regex pour ReDoS
- [ ] Identifie les integer overflows potentiels

### 3.2 Error Handling
- [ ] Vérifie que les erreurs ne leakent pas d'informations sensibles
- [ ] Analyse les stack traces en production
- [ ] Vérifie la gestion des panics

### 3.3 Concurrency & Race Conditions
- [ ] Identifie les race conditions critiques
- [ ] Vérifie l'utilisation des mutexes
- [ ] Analyse les accès concurrents aux ressources partagées

### 3.4 Timeouts & Resource Exhaustion
- [ ] Vérifie tous les timeouts (HTTP, DB, Context)
- [ ] Vérifie les connection pools
- [ ] Identifie les risques de DoS

**Checklist des timeouts** :
```go
// HTTP client timeouts
httpClient := &http.Client{
    Timeout: 30 * time.Second,
}

// Database context timeout
ctx, cancel := context.WithTimeout(ctx, 10*time.Second)
defer cancel()
```

## Phase 4 : Secrets Management

### 4.1 Scan de Secrets
```bash
# Utilise gitleaks
gitleaks detect --source . --verbose

# Cherche des patterns
git grep -i "password"
git grep -i "apikey"
git grep -i "secret"
git grep -i "token"
```

### 4.2 Environment Variables
- [ ] Liste toutes les variables sensibles
- [ ] Vérifie qu'elles sont documentées dans CLAUDE.md / .env.example
- [ ] Propose une solution de secret management (Vault, K8s Secrets)
- [ ] Vérifie la rotation des secrets

### 4.3 Credentials en Clair
- [ ] Scan du repository (history inclus)
- [ ] Vérifie les fichiers de config (.env, config.yaml)
- [ ] Analyse les logs pour secrets exposés

## Phase 5 : Infrastructure Security

### 5.1 Docker Security
- [ ] Vérifie que l'image utilise un non-root user
- [ ] Analyse les layers Docker pour secrets
- [ ] Vérifie les security contexts
- [ ] Valide la minimal attack surface

### 5.2 Network Security
- [ ] Analyse les ports exposés
- [ ] Vérifie la configuration TLS pour les services externes
- [ ] Valide la segmentation réseau

### 5.3 Database Security
- [ ] Vérifie les permissions de la base de données
- [ ] Analyse les connection strings (TLS enforced?)
- [ ] Vérifie l'encryption at rest
- [ ] Valide les backup policies

## Phase 6 : Rapport de Sécurité

Génère un rapport dans `docs/reviews/security-YYYY-MM-DD.md` (créer `docs/reviews/` si besoin) avec :

### Résumé Exécutif
- **Score de sécurité global**: X/100
- **Vulnérabilités critiques**: N
- **Vulnérabilités hautes**: M
- **Conformité OWASP Top 10**: Y%
- **Risque global**: Faible/Moyen/Élevé

### Vulnérabilités Détaillées

Pour chaque vulnérabilité :
```markdown
#### [SEVERITY] Vulnerability Title
**CVE/CWE**: CVE-XXXX-XXXXX / CWE-XXX
**Fichier**: `path/to/file:line`
**Catégorie**: OWASP A0X
**Impact**: Description de l'impact
**CVSS Score**: X.X (Critical/High/Medium/Low)
**Exploitability**: Facile/Moyen/Difficile

**Description**: Explication détaillée

**Remediation**:
// Code sécurisé

**Priorité**: Critique/Haute/Moyenne/Basse
**Effort**: X heures
```

### Plan de Remediation (par priorité)

**Immédiat (< 24h)** :
- Vulnérabilités critiques avec exploit public
- Credentials exposés

**Court terme (< 1 semaine)** :
- Vulnérabilités hautes
- Configuration de sécurité

**Moyen terme (< 1 mois)** :
- Vulnérabilités moyennes
- Hardening général

**Long terme (< 3 mois)** :
- Vulnérabilités basses
- Améliorations proactives

### Checklist de Sécurité
- [ ] Scan de vulnérabilités exécuté
- [ ] Secrets révoqués si exposés
- [ ] Patches appliqués
- [ ] Tests de sécurité validés
- [ ] Documentation mise à jour
- [ ] Monitoring configuré

### Recommandations Générales
- Mise en place de SAST/DAST dans la CI/CD
- Formation sécurité pour l'équipe
- Penetration testing réguliers
- Security champions dans l'équipe

---

**Note**: Pour toute vulnérabilité critique, créer un incident immédiatement et notifier l'équipe concernée.

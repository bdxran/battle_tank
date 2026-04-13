# Test Command

Génère et améliore les tests pour atteindre une couverture élevée et une qualité de tests optimale.

## Objectif

Atteindre >80% de couverture de code avec des tests de qualité, robustes et maintenables.

## Phase 1 : Analyse de Couverture Actuelle

### 1.1 Génération du Rapport de Couverture
```bash
# Go
go test -coverprofile=coverage.out ./...
go tool cover -func=coverage.out | sort -k 3 -n
go tool cover -html=coverage.out -o coverage.html

# Adapter selon le langage du projet
just test-cover 2>/dev/null || true
```

### 1.2 Identification des Gaps
Identifie les packages/fichiers avec couverture < 80% :
- [ ] Liste les fichiers non testés
- [ ] Liste les fonctions non couvertes
- [ ] Identifie les branches non testées
- [ ] Priorise par criticité (business logic > utils)

**Format de rapport** :
```markdown
### Coverage Gaps

#### Critical (Business Logic)
- `internal/services/foo_service.go`: 45% (target: 90%)
  - `CreateFoo`: Non testé
  - `UpdateFoo`: Branches d'erreur non testées

#### High Priority
- `internal/repository/foo_repo.go`: 60% (target: 85%)
  - `GetByID`: Non testé

#### Medium Priority
- `internal/controllers/foo_controller.go`: 70% (target: 80%)
  - Edge cases non testés
```

## Phase 2 : Génération de Tests

**IMPORTANT**: Pour chaque fonction, créer UN SEUL test avec table-driven approach qui couvre TOUS les cas possibles :
- ✅ Cas nominal (success)
- ✅ Tous les cas d'erreur (database, validation, business logic)
- ✅ Tous les edge cases (nil, empty, limites)
- ✅ Cas de concurrence si applicable

**Principe**: Une fonction = Un test complet et générique

### 2.1 Tests Unitaires

Pour chaque fonction non testée, génère UN SEUL test complet :

```go
package services_test

import (
    "context"
    "testing"

    "github.com/stretchr/testify/assert"
    "github.com/stretchr/testify/mock"
    "github.com/stretchr/testify/require"

    "your-module/internal/repository"
    "your-module/internal/types"
    "your-module/internal/services"
)

func TestFooService_Create(t *testing.T) {
    tests := []struct {
        name        string
        input       *types.FooInput
        setupMock   func(*repository.MockFooRepository)
        wantErr     bool
        expectedErr error
        validate    func(*testing.T, *types.Foo)
    }{
        // ========== CAS DE SUCCÈS ==========
        {
            name: "success - valid input",
            input: &types.FooInput{
                Name: "Test Foo",
            },
            setupMock: func(m *repository.MockFooRepository) {
                m.On("Create", mock.Anything, mock.AnythingOfType("*types.FooInput")).
                    Return(&types.Foo{ID: 1, Name: "Test Foo"}, nil)
            },
            wantErr: false,
        },

        // ========== ERREURS DE VALIDATION ==========
        {
            name:      "error - nil input",
            input:     nil,
            setupMock: func(m *repository.MockFooRepository) {},
            wantErr:   true,
        },
        {
            name: "error - empty name",
            input: &types.FooInput{
                Name: "",
            },
            setupMock: func(m *repository.MockFooRepository) {},
            wantErr:   true,
        },

        // ========== ERREURS BASE DE DONNÉES ==========
        {
            name: "error - duplicate",
            input: &types.FooInput{
                Name: "Duplicate",
            },
            setupMock: func(m *repository.MockFooRepository) {
                m.On("Create", mock.Anything, mock.AnythingOfType("*types.FooInput")).
                    Return(nil, repository.ErrDuplicate)
            },
            wantErr:     true,
            expectedErr: repository.ErrDuplicate,
        },
        {
            name: "error - context timeout",
            input: &types.FooInput{
                Name: "Test",
            },
            setupMock: func(m *repository.MockFooRepository) {
                m.On("Create", mock.Anything, mock.AnythingOfType("*types.FooInput")).
                    Return(nil, context.DeadlineExceeded)
            },
            wantErr:     true,
            expectedErr: context.DeadlineExceeded,
        },

        // ========== EDGE CASES ==========
        {
            name: "edge - name with special characters",
            input: &types.FooInput{
                Name: "Test-Foo_123 日本語 🚀",
            },
            setupMock: func(m *repository.MockFooRepository) {
                m.On("Create", mock.Anything, mock.AnythingOfType("*types.FooInput")).
                    Return(&types.Foo{ID: 2, Name: "Test-Foo_123 日本語 🚀"}, nil)
            },
            wantErr: false,
        },
    }

    for _, tt := range tests {
        t.Run(tt.name, func(t *testing.T) {
            mockRepo := new(repository.MockFooRepository)
            tt.setupMock(mockRepo)

            service := services.NewFooService(mockRepo)

            ctx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
            defer cancel()

            result, err := service.Create(ctx, tt.input)

            if tt.wantErr {
                require.Error(t, err)
                if tt.expectedErr != nil {
                    assert.ErrorIs(t, err, tt.expectedErr)
                }
            } else {
                require.NoError(t, err)
                assert.NotNil(t, result)
                if tt.validate != nil {
                    tt.validate(t, result)
                }
            }

            mockRepo.AssertExpectations(t)
        })
    }
}
```

### 2.2 Tests d'Intégration

Pour les repositories/DAOs, génère des tests d'intégration avec une vraie base de données (testcontainers ou in-memory) :

```go
package repository_test

import (
    "context"
    "testing"

    "github.com/stretchr/testify/assert"
    "github.com/stretchr/testify/require"

    "your-module/internal/repository"
)

func TestFooRepository_Integration_GetByID(t *testing.T) {
    if testing.Short() {
        t.Skip("Skipping integration test")
    }

    // Setup test database (adapter selon le projet)
    db := setupTestDB(t)
    defer db.Close()

    ctx := context.Background()

    // Create test data
    created := createTestFoo(t, db, "Test Foo")

    // Test
    repo := repository.NewFooRepository(db)
    result, err := repo.GetByID(ctx, created.ID)
    require.NoError(t, err)

    assert.Equal(t, created.ID, result.ID)
    assert.Equal(t, "Test Foo", result.Name)
}
```

### 2.3 Tests de Contrôleurs

Génère UN SEUL test par endpoint qui couvre TOUS les scénarios HTTP :

```go
package controllers_test

import (
    "bytes"
    "encoding/json"
    "net/http"
    "net/http/httptest"
    "testing"

    "github.com/stretchr/testify/assert"
    "github.com/stretchr/testify/mock"
    "github.com/stretchr/testify/require"
)

func TestFooController_Create(t *testing.T) {
    tests := []struct {
        name           string
        requestBody    interface{}
        headers        map[string]string
        setupMock      func(*services.MockFooService)
        expectedStatus int
        expectedBody   map[string]interface{}
    }{
        // ========== SUCCÈS ==========
        {
            name: "201 - valid creation",
            requestBody: map[string]interface{}{
                "name": "Test Foo",
            },
            headers: map[string]string{
                "Content-Type": "application/json",
            },
            setupMock: func(m *services.MockFooService) {
                m.On("Create", mock.Anything, mock.AnythingOfType("*types.FooInput")).
                    Return(&types.Foo{ID: 1, Name: "Test Foo"}, nil)
            },
            expectedStatus: http.StatusCreated,
        },

        // ========== ERREURS 400 ==========
        {
            name:           "400 - invalid JSON",
            requestBody:    "invalid json{{{",
            setupMock:      func(m *services.MockFooService) {},
            expectedStatus: http.StatusBadRequest,
        },
        {
            name: "400 - missing name",
            requestBody: map[string]interface{}{
                "description": "Test",
            },
            setupMock:      func(m *services.MockFooService) {},
            expectedStatus: http.StatusBadRequest,
        },

        // ========== ERREURS 409 ==========
        {
            name: "409 - duplicate",
            requestBody: map[string]interface{}{
                "name": "Duplicate",
            },
            setupMock: func(m *services.MockFooService) {
                m.On("Create", mock.Anything, mock.AnythingOfType("*types.FooInput")).
                    Return(nil, services.ErrDuplicate)
            },
            expectedStatus: http.StatusConflict,
        },

        // ========== ERREURS 500 ==========
        {
            name: "500 - database error",
            requestBody: map[string]interface{}{
                "name": "Test",
            },
            setupMock: func(m *services.MockFooService) {
                m.On("Create", mock.Anything, mock.AnythingOfType("*types.FooInput")).
                    Return(nil, errors.New("database error"))
            },
            expectedStatus: http.StatusInternalServerError,
        },
    }

    for _, tt := range tests {
        t.Run(tt.name, func(t *testing.T) {
            mockService := new(services.MockFooService)
            tt.setupMock(mockService)

            // Setup router (adapter selon le framework)
            router := setupTestRouter(mockService)

            var body []byte
            if str, ok := tt.requestBody.(string); ok {
                body = []byte(str)
            } else if tt.requestBody != nil {
                body, _ = json.Marshal(tt.requestBody)
            }

            req := httptest.NewRequest(http.MethodPost, "/foos", bytes.NewReader(body))
            for key, value := range tt.headers {
                req.Header.Set(key, value)
            }
            w := httptest.NewRecorder()

            router.ServeHTTP(w, req)

            assert.Equal(t, tt.expectedStatus, w.Code)
            mockService.AssertExpectations(t)
        })
    }
}
```

## Phase 3 : Tests de Qualité

### 3.1 Benchmarks
Génère des benchmarks pour les fonctions critiques :

```go
func BenchmarkFooService_GetAll(b *testing.B) {
    mockRepo := new(repository.MockFooRepository)
    mockRepo.On("GetAll", mock.Anything).
        Return([]*types.Foo{}, nil)

    service := services.NewFooService(mockRepo)
    ctx := context.Background()

    b.ResetTimer()
    for i := 0; i < b.N; i++ {
        _, _ = service.GetAll(ctx)
    }
}
```

### 3.2 Checklist de Complétude des Tests

**Cas de succès** :
- [ ] Cas nominal simple
- [ ] Cas avec paramètres optionnels
- [ ] Cas avec données complexes

**Erreurs de validation** :
- [ ] Paramètres nil/null
- [ ] Paramètres vides
- [ ] Paramètres invalides

**Erreurs système** :
- [ ] Database errors (connection, timeout, constraint)
- [ ] Network errors
- [ ] Context cancellation/timeout

**Edge cases** :
- [ ] Valeurs limites (min/max)
- [ ] Données très volumineuses
- [ ] Caractères spéciaux/unicode
- [ ] Concurrent access (si applicable)

### 3.3 Test Helpers
Crée des helpers pour réduire la duplication :

```go
// internal/testhelpers/fixtures.go
package testhelpers

func CreateTestFoo(t *testing.T, db DB, opts ...FooOption) *Foo {
    // Helper pour créer des entités de test
}

func SetupTestDB(t *testing.T) DB {
    // Helper pour setup DB de test
}
```

## Phase 4 : Tests de Contrat

### 4.1 Contract Testing
Si le projet a des contrats OpenAPI, valide-les :

```bash
just validate-contract 2>/dev/null || true
```

### 4.2 API Response Validation
Vérifie que les réponses respectent le schéma OpenAPI/Swagger.

## Phase 5 : Tests de Performance

### 5.1 Load Testing
Crée des scripts de load testing avec `vegeta`, `k6`, ou `locust`.

### 5.2 Stress Testing
Teste les limites du système (throughput max, latency under load).

## Phase 6 : Rapport de Tests

Génère un rapport dans `docs/reviews/tests-YYYY-MM-DD.md` (créer `docs/reviews/` si besoin) avec :

### Résumé
- **Couverture globale**: X%
- **Tests ajoutés**: N
- **Tests améliorés**: M
- **Benchmarks créés**: P

### Couverture par Package
```markdown
| Package | Before | After | Delta | Target |
|---------|--------|-------|-------|--------|
| controllers | 50% | 85% | +35% | 80% ✅ |
| services | 60% | 92% | +32% | 90% ✅ |
| repository | 45% | 88% | +43% | 85% ✅ |
```

### Tests Générés

Pour chaque fichier :
```markdown
#### `internal/services/foo_service_test.go`
**Tests ajoutés**: N
**Couverture**: 92%

- ✅ `TestFooService_Create` (8 cases)
- ✅ `TestFooService_Update` (6 cases)
- ✅ `BenchmarkFooService_GetAll`
```

### Métriques de Qualité
- **Mock usage**: Z mocks créés
- **Edge cases**: W edge cases testés
- **Integration tests**: V tests d'intégration

### Checklist d'Implémentation
- [ ] Tests unitaires pour tous les services
- [ ] Tests d'intégration pour tous les repositories
- [ ] Tests de contrôleurs pour tous les endpoints
- [ ] Benchmarks pour fonctions critiques
- [ ] Contract tests (OpenAPI)
- [ ] Load tests

---

**Note**: Exécuter `just test` avant chaque commit. Viser >80% de couverture globale.

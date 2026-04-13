# Standard — Tests

## Stratégie

| Type | Scope | Framework |
|------|-------|-----------|
| **Unitaires** | `GameLogic/` — entités, physique, règles | NUnit |
| **Intégration** | Protocole réseau, game loop complète | NUnit |
| **E2E (post-MVP)** | Simulation de partie complète | NUnit + Godot headless |

Les tests Godot (nodes, scènes) ne sont **pas** dans le périmètre prioritaire — tester `GameLogic/` suffit.

---

## Framework

- **NUnit** — framework de test C# standard
- **FluentAssertions** — assertions lisibles (optionnel mais recommandé)
- Projet de test séparé : `Tests/BattleTank.Tests.csproj`
- Référence uniquement `GameLogic/` — jamais le SDK Godot

```xml
<!-- Tests/BattleTank.Tests.csproj -->
<PackageReference Include="NUnit" Version="4.*" />
<PackageReference Include="NUnit3TestAdapter" Version="4.*" />
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
<PackageReference Include="FluentAssertions" Version="6.*" />
```

---

## Couverture minimale

- Seuil global : **80%**
- Périmètre prioritaire : `GameLogic/Physics/`, `GameLogic/Rules/`, `GameLogic/Entities/`

```bash
dotnet test --collect:"XPlat Code Coverage"
reportgenerator -reports:coverage.xml -targetdir:coverage-report
```

---

## Structure des tests

```
Tests/
├── Entities/
│   ├── TankEntityTests.cs
│   └── BulletEntityTests.cs
├── Physics/
│   └── CollisionEngineTests.cs
├── Rules/
│   ├── GameRoomTests.cs
│   └── BattleRoyaleRulesTests.cs
├── Network/
│   └── ProtocolTests.cs
└── Fixtures/
    ├── MapFixtures.cs      # Cartes de test
    └── GameStateFixtures.cs
```

---

## Conventions

```csharp
[TestFixture]
public class TankEntityTests
{
    // Nommage : Méthode_Contexte_RésultatAttendu
    [Test]
    public void ApplyDamage_WhenHealthDropsToZero_SetsIsAliveFalse()
    {
        // Arrange
        var tank = new TankEntity(id: 1, position: Vector2.Zero);

        // Act
        tank.ApplyDamage(tank.MaxHealth);

        // Assert
        Assert.That(tank.IsAlive, Is.False);
    }
}
```

- Pattern **Arrange / Act / Assert** — toujours
- Nommage : `Méthode_Contexte_RésultatAttendu`
- Un test = un comportement — pas de multi-assert sur des comportements distincts
- Pas d'état partagé entre tests — chaque test crée ses propres objets
- Les fixtures (cartes, états initiaux) dans `Tests/Fixtures/`

---

## Ce qui ne doit pas être mocké

- La logique de jeu — tester les vraies classes
- `CollisionEngine` — utiliser la vraie implémentation

## Ce qui peut être mocké

- Les interfaces réseau (`INetworkManager`) dans les tests de règles
- L'horloge (`IClock`) pour contrôler le temps dans les tests

---

## Commandes

```bash
dotnet test                          # Tous les tests
dotnet test --filter "Category=Unit" # Tests unitaires uniquement
dotnet test --collect:"XPlat Code Coverage" # Avec couverture
```

---

## Règles pour Claude

1. Tout ajout dans `GameLogic/` doit avoir des tests correspondants
2. Les tests sont dans `Tests/` — jamais inline dans `GameLogic/`
3. Pas de `Thread.Sleep` dans les tests — utiliser une interface `IClock` mockable
4. Pas de fichiers temporaires ou I/O dans les tests unitaires
5. Les fixtures partagées vont dans `Tests/Fixtures/`, pas dans les fichiers de test

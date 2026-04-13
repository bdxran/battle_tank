# Standard — Code C#

## Naming conventions

| Élément | Convention | Exemple |
|---------|-----------|---------|
| Class, struct, record | PascalCase | `TankEntity`, `GameRoom` |
| Interface | `I` + PascalCase | `ICollidable`, `INetworkMessage` |
| Enum | PascalCase | `GamePhase`, `MessageType` |
| Enum value | PascalCase | `GamePhase.WaitingForPlayers` |
| Méthode publique | PascalCase | `ApplyInput()`, `GetState()` |
| Méthode privée | PascalCase | `CalculateDamage()` |
| Propriété | PascalCase | `CurrentHealth`, `Position` |
| Champ privé | `_camelCase` | `_currentHealth`, `_tickCount` |
| Variable locale | camelCase | `deltaTime`, `hitResult` |
| Paramètre | camelCase | `playerId`, `input` |
| Constante | PascalCase (dans une classe) | `MaxPlayers`, `TickRate` |
| Namespace | PascalCase hiérarchique | `BattleTank.GameLogic.Physics` |

---

## Namespaces

```csharp
// GameLogic (C# pur)
BattleTank.GameLogic
BattleTank.GameLogic.Entities
BattleTank.GameLogic.Physics
BattleTank.GameLogic.Network
BattleTank.GameLogic.Rules

// Godot wrappers
BattleTank.Godot.Nodes
BattleTank.Godot.UI

// Shared
BattleTank.Shared
```

---

## Types et immutabilité

- Utiliser `record` pour les données de valeur immuables (snapshots, messages réseau)
- Utiliser `readonly struct` pour les vecteurs et types mathématiques fréquents
- Utiliser `class` pour les entités avec état mutable (Tank, Bullet)

```csharp
// Bons exemples
public record PlayerInput(int PlayerId, InputFlags Flags, uint SequenceNumber);
public record TankSnapshot(int Id, Vector2 Position, float Rotation, int Health);
public class TankEntity { /* état mutable */ }
```

---

## Nullable reference types

Activé globalement (`<Nullable>enable</Nullable>` dans le `.csproj`).

```csharp
// Interdit — ne pas ignorer les warnings nullable
string name = null; // warning → erreur de compilation

// Correct
string? name = null;
string name = "default";
```

---

## var

Utiliser `var` uniquement quand le type est évident à droite :

```csharp
// Autorisé
var tank = new TankEntity(id, position);
var tanks = new List<TankEntity>();

// Interdit — type non évident
var result = GetState(); // quel type retourne GetState ?
```

---

## Gestion des collections

- Préférer `IReadOnlyList<T>`, `IReadOnlyDictionary<K,V>` pour les retours publics
- Utiliser `Dictionary<K,V>` pour les lookups fréquents par id
- Eviter LINQ dans les hot paths (game loop 20 TPS) — préférer des boucles `for`

```csharp
// Hot path — game loop
for (int i = 0; i < _bullets.Count; i++)
    _bullets[i].Update(deltaTime);

// Hors hot path — OK
var aliveTanks = _tanks.Values.Where(t => t.IsAlive).ToList();
```

---

## Async / await

- Pas d'`async` dans la game loop — logique de jeu synchrone uniquement
- `async/await` réservé aux opérations I/O : chargement de config, persistance
- Ne jamais utiliser `async void` sauf pour les event handlers Godot (et documenter pourquoi)

---

## Organisation d'un fichier

```csharp
namespace BattleTank.GameLogic.Entities;

// 1. Constantes et champs privés
// 2. Constructeur
// 3. Propriétés publiques
// 4. Méthodes publiques
// 5. Méthodes privées
```

Un fichier = une classe/record/interface. Pas de types multiples dans un même fichier.

---

## Ce qui est interdit dans `GameLogic/`

```csharp
// INTERDIT — aucune dépendance Godot dans GameLogic/
using Godot;
GD.Print("...");
GetNode<...>("...");
Vector2 // utiliser System.Numerics.Vector2 ou struct custom
```

`GameLogic/` doit compiler en C# pur sans le SDK Godot.

---

## Règles pour Claude

1. Respecter les namespaces hiérarchiques — ne pas tout mettre à la racine
2. Pas de `public` par défaut — choisir la visibilité minimale nécessaire
3. Pas de commentaires XML sur des méthodes triviales
4. Préférer des noms explicites plutôt que des commentaires
5. Longueur de méthode : idéalement < 30 lignes — extraire si plus long

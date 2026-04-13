# Standard — Architecture

## Principe fondamental : séparation GameLogic / Godot

```
GameLogic/          ← C# pur, zéro dépendance Godot, testable en CLI
Godot/              ← Thin wrappers, adaptateurs, nodes de scène
```

**La règle** : si un fichier importe `using Godot;`, il appartient à `Godot/`. Jamais dans `GameLogic/`.

---

## Structure de dossiers

```
src/
├── GameLogic/                  # C# pur — logique de jeu
│   ├── Entities/
│   │   ├── TankEntity.cs       # État, comportement du tank
│   │   ├── BulletEntity.cs     # Projectile
│   │   └── ZoneEntity.cs       # Zone de jeu (battle royale)
│   ├── Physics/
│   │   ├── CollisionEngine.cs  # Détection collisions
│   │   └── Movement.cs         # Calcul de déplacement
│   ├── Network/
│   │   ├── Protocol.cs         # Types de messages, sérialisation
│   │   └── GameStateSerializer.cs
│   ├── Rules/
│   │   ├── GameRoom.cs         # Salle de jeu, game loop
│   │   ├── BattleRoyaleRules.cs
│   │   └── SpawnSystem.cs
│   └── Shared/
│       ├── Constants.cs        # Constantes de jeu
│       └── Types.cs            # Structs/records partagés
│
├── Godot/                      # Wrappers Godot
│   ├── Nodes/
│   │   ├── TankNode.cs         # Node2D wrappant TankEntity
│   │   ├── BulletNode.cs
│   │   └── GameRoomNode.cs     # Orchestre la GameRoom
│   ├── Network/
│   │   ├── ServerNetworkManager.cs   # ENet côté serveur
│   │   └── ClientNetworkManager.cs   # ENet côté client
│   ├── UI/
│   │   ├── HUD.cs
│   │   └── MainMenu.cs
│   └── Renderer/
│       └── GameRenderer.cs     # Lecture du GameState → affichage Canvas
│
├── Tests/                      # Tests sur GameLogic uniquement
│   ├── Entities/
│   ├── Physics/
│   └── Rules/
│
└── Shared/                     # Types partagés client/serveur (si besoin)
```

---

## Pattern : Node = thin wrapper

Un node Godot ne contient pas de logique de jeu. Il :
1. Détient une référence à l'entité `GameLogic` correspondante
2. Lit l'état de l'entité pour le rendu
3. Transmet les inputs au gestionnaire réseau — jamais directement à l'entité

```csharp
// CORRECT — TankNode est un thin wrapper
public partial class TankNode : Node2D
{
    private TankEntity _entity = null!;

    public void Initialize(TankEntity entity) => _entity = entity;

    public override void _Process(double delta)
    {
        Position = _entity.Position.ToGodotVector();
        RotationDegrees = _entity.Rotation;
    }
}

// INTERDIT — logique de jeu dans un node
public partial class TankNode : Node2D
{
    public override void _Process(double delta)
    {
        if (Input.IsActionPressed("move_forward"))
            Position += new Vector2(0, -Speed * (float)delta); // ← logique de jeu !
    }
}
```

---

## Pattern : accès aux nodes

Pas de `GetNode<T>("../../Path/To/Node")` dans la logique.

```csharp
// INTERDIT — couplage fragile
var hud = GetNode<HUD>("/root/Game/UI/HUD");

// CORRECT — injection via Initialize() ou export
[Export] private HUD _hud = null!;
// ou
public void Initialize(HUD hud) => _hud = hud;
```

---

## Séparation serveur / client

Le projet Godot contiendra deux scènes principales :
- `Server.tscn` — lancé en mode serveur dédié (headless)
- `Client.tscn` — lancé en mode client (avec rendu)

`GameLogic/` est partagé entre serveur et client — c'est le même code de simulation.

```
Serveur : GameRoom (autoritaire) → ServerNetworkManager → clients
Client  : ClientNetworkManager → GameState local → Renderer
```

---

## Dépendances autorisées

| Couche | Peut dépendre de |
|--------|-----------------|
| `GameLogic/` | `System`, `System.Numerics`, librairies C# pures |
| `Godot/` | `GameLogic/`, SDK Godot |
| `Tests/` | `GameLogic/`, framework de test (NUnit) |

`GameLogic/` ne dépend **jamais** de `Godot/`.

---

## Règles pour Claude

1. Toute nouvelle entité de jeu → créer d'abord dans `GameLogic/Entities/`
2. Toute logique de collision/physique → `GameLogic/Physics/`
3. Les nodes Godot n'ont pas de logique — ils lisent et affichent
4. Un `TankNode` ne modifie jamais directement `TankEntity` — passe par le serveur
5. Vérifier qu'aucun `using Godot;` ne se glisse dans `GameLogic/`

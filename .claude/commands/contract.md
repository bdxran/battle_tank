---
description: Créer ou mettre à jour le contrat de protocole réseau ENet/MessagePack
---

# Contract Protocole Réseau

Crée ou met à jour le contrat de protocole réseau (`docs/contracts/protocol.md`) en suivant `standards/network.md`.

---

## Phase 1 — Lecture des standards

Lire `standards/network.md`.

Rappel de l'architecture :
```
Client → [PlayerInput] → Serveur autoritaire → [GameStateDelta] → Tous les clients
```
- ENet (UDP) comme transport principal
- Sérialisation MessagePack (binaire, compact)
- Messages définis dans `src/GameLogic/Network/Protocol.cs`

---

## Phase 2 — Identifier le contrat concerné

```bash
cat docs/contracts/protocol.md   # contrat actuel
cat src/GameLogic/Network/Protocol.cs   # source de vérité code
```

Vérifier la cohérence entre les deux.

---

## Phase 3 — Contract-First : rédiger avant le code

Si la feature est en cours de conception :

1. Définir les nouveaux messages dans `docs/contracts/protocol.md` :

```markdown
### [NomDuMessage]

**Direction** : Client → Serveur / Serveur → Clients / Broadcast

**Déclencheur** : [quand ce message est envoyé]

**Structure MessagePack** :
| Champ | Type C# | Description |
|-------|---------|-------------|
| field1 | uint | ... |
| field2 | float | ... |

**Exemple** :
```json
{ "field1": 42, "field2": 1.5 }
```
```

2. Présenter le contrat à l'utilisateur avant de continuer.

---

## Phase 4 — Code-First : synchroniser après implémentation

Si des messages ont été modifiés dans `Protocol.cs` :

1. Lire `Protocol.cs` pour extraire les types MessagePack
2. Mettre à jour `docs/contracts/protocol.md` pour refléter les changements
3. Vérifier la cohérence : chaque `[MessagePackObject]` doit apparaître dans le contrat

---

## Phase 5 — Breaking changes

Si le changement est un breaking change (suppression de champ, changement de type) :
- [ ] Version du protocole bumpée dans `Protocol.cs` et `protocol.md`
- [ ] Breaking change documenté dans `changelog.md`
- [ ] Compatibilité client/serveur vérifiée (migration si nécessaire)

---

## Phase 6 — Mise à jour changelog

```markdown
### Changed
- [Protocole] [description du changement] — `docs/contracts/protocol.md`
```

---

## Handoff

```
Contrat mis à jour : docs/contracts/protocol.md
Breaking change    : oui / non
Cohérence Protocol.cs : ✅ / ⚠️

Étape suivante selon le contexte :
- Si contract-first → Lancer /implement ? [O/n]
- Si post-implémentation → Lancer /document ? [O/n]
```

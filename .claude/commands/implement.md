---
description: Implémenter une fonctionnalité en suivant les standards du projet, avec plan d'implémentation
---

# Implémentation

Implémente la fonctionnalité décrite dans l'analyse et/ou la STD, en utilisant le mode plan.

---

## Phase 1 — Lecture des documents de référence

Avant tout code, lire dans l'ordre :

1. `docs/analyse/[feature].md` — analyse fonctionnelle
2. `docs/std/STD-[feature].md` — spécification technique (si elle existe)
3. Les standards concernés selon les composants à modifier :

| Composant | Standard |
|-----------|----------|
| GameLogic / Entities | `standards/architecture.md`, `standards/csharp-code.md` |
| GameLogic / Physics | `standards/architecture.md`, `standards/csharp-code.md` |
| GameLogic / Rules | `standards/architecture.md`, `standards/csharp-code.md` |
| GameLogic / Network | `standards/network.md`, `standards/csharp-code.md` |
| Godot / Nodes | `standards/architecture.md` |
| Godot / Network | `standards/network.md` |
| Tests | `standards/testing.md` |

---

## Phase 2 — Contrat réseau

Si la feature impacte le protocole réseau, lire `standards/network.md`.

### Contract-First
Le contrat a déjà été rédigé avant d'arriver ici (via `/contract`).
→ Lire `docs/contracts/protocol.md` comme référence d'implémentation.
→ Le code doit se conformer au contrat — jamais l'inverse.

### Code-First
Le contrat sera mis à jour après implémentation (via `/contract`).
→ Identifier les messages impactés pour le handoff.

Si la feature nécessite un contrat et qu'aucun n'existe → proposer de lancer `/contract` d'abord.

---

## Phase 3 — Plan d'implémentation (mode plan)

**Utiliser le mode plan Claude (`EnterPlanMode`) avec un contexte propre.**

Le plan doit couvrir :
- Ordre d'implémentation : GameLogic en premier (Entities → Physics/Rules → Network), Godot ensuite
- Fichiers à créer / modifier
- Types C# à définir (`record`, `readonly struct`, `class`)
- Impacts sur les composants existants
- Points de risque (séparation GameLogic/Godot, types nullable, hot paths)

Présenter le plan à l'utilisateur et attendre validation avant de continuer.

---

## Phase 4 — Implémentation

Implémenter en suivant le plan validé, dans l'ordre défini.

Pour chaque composant :
- Lire les fichiers existants avant de les modifier
- Respecter les conventions C# (`standards/csharp-code.md`)
- Jamais de `using Godot;` dans GameLogic
- Utiliser `Result<T>` pour les erreurs métier dans GameLogic
- Utiliser `ILogger<T>` pour les logs (jamais `GD.Print()` dans GameLogic)

Après chaque composant significatif :
```bash
just build
```

---

## Phase 5 — Mise à jour backlog & issue

- `backlog.md` : marquer l'item comme "Implémenté"
- `issue.md` : documenter les décisions prises ou blocages rencontrés

---

## Handoff

**Cas 1 — Pas de changement de protocole réseau :**
```
Implémentation terminée.

Étape suivante : écrire les tests NUnit.
Lancer /test ? [O/n]
```

**Cas 2 — Protocole réseau modifié ou ajouté :**
```
Implémentation terminée.

Étapes suivantes indépendantes — peuvent tourner en parallèle :
  → /test      (tests NUnit GameLogic)
  → /contract  (mise à jour docs/contracts/protocol.md)

Lancer les deux en parallèle ? [O/n] (ou choisir : test / contract)
```

Si l'utilisateur valide le parallèle → lancer deux agents simultanés :
- Agent 1 : exécuter le skill `/test`
- Agent 2 : exécuter le skill `/contract`

Les deux doivent être terminés avant de passer à `/document`.

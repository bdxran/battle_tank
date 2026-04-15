# Apply Review Command

Applique les corrections issues d'un rapport de review de manière interactive, par ordre de priorité.

---

## Phase 1 — Charger le rapport

Trouver le rapport de review le plus récent :

```bash
ls -t docs/reviews/ | head -5
```

- Si plusieurs rapports existent, afficher la liste et demander lequel appliquer
- Si aucun rapport n'existe : informer l'utilisateur et arrêter

Extraire toutes les actions du rapport, toutes priorités confondues, dans cet ordre :
1. Issues critiques / Avant merge
2. Issues importantes / Cette semaine
3. Issues moyenne priorité / Backlog / Nice to have

---

## Phase 2 — Présentation du plan

Avant tout changement, afficher le plan complet :

```
Rapport : docs/reviews/review-changes-2026-04-15_14-30.md
Actions identifiées : N

CRITIQUES (X)
  1. [description] — fichier:ligne
  2. ...

IMPORTANTES (Y)
  3. [description] — fichier:ligne
  ...

BACKLOG (Z)
  N. [description] — fichier:ligne
  ...

Appliquer tout ? [O/n] ou entrer les numéros à ignorer (ex: 3,5) :
```

Attendre la réponse de l'utilisateur avant de continuer.

---

## Phase 3 — Application interactive

Pour chaque action (dans l'ordre, en ignorant celles exclues par l'utilisateur) :

1. **Annoncer** l'action en cours :
   ```
   [2/N] CRITIQUE — Séparation GameLogic/Godot : import Godot dans GameLogic
   Fichier : src/GameLogic/Rules/GameRoom.cs:42
   Standard : standards/architecture.md
   ```

2. **Lire** le fichier concerné avant toute modification

3. **Proposer** le changement :
   ```
   Correction proposée :
   - using Godot;                        // ← import interdit dans GameLogic
   + // supprimé — logique déplacée dans Godot/Nodes/

   Appliquer ? [O/n/skip/stop] :
   ```
   - `O` ou entrée → appliquer
   - `n` → passer à l'action suivante
   - `skip` → passer à l'action suivante (identique à `n`)
   - `stop` → arrêter et passer au rapport final

4. **Appliquer** la correction si confirmée

5. **Vérifier** après chaque correction :
   ```bash
   just build
   ```
   Si le build échoue après une correction : signaler immédiatement, proposer de revenir en arrière.

---

## Phase 4 — Tests après corrections

Une fois toutes les corrections appliquées :

```bash
just test
just test-cover
```

- Si des tests cassent : identifier lesquels et proposer de les corriger (interactif, même processus)
- Si la couverture descend sous 80% : signaler

---

## Phase 5 — Mise à jour du rapport

Mettre à jour le rapport de review original pour refléter les corrections appliquées :

- Marquer chaque action traitée : ✅ Appliqué / ⏭️ Ignoré
- Ajouter une section en fin de rapport :

```markdown
## Application des corrections — [Date heure]

| # | Action | Statut |
|---|--------|--------|
| 1 | [description] | ✅ Appliqué |
| 2 | [description] | ⏭️ Ignoré |
| 3 | [description] | ✅ Appliqué |

**Résultat** : X/N corrections appliquées
**Build** : ✅ / ❌
**Tests** : ✅ / ❌ (couverture : X%)
```

---

## Phase 6 — Mise à jour du changelog

Ajouter une entrée dans `changelog.md` section `[Unreleased]` pour chaque correction appliquée de priorité critique ou importante.

---

## Handoff

À la fin, afficher :

```
Corrections appliquées : X/N
Build : ✅ / ❌
Tests : ✅ / ❌

Étape suivante : committer les changements.
Lancer /commit ? [O/n]
```

Si l'utilisateur valide → invoquer le skill `/commit`.

---

## Règles

- Ne jamais appliquer une correction sans l'avoir montrée à l'utilisateur
- Toujours lire le fichier avant de le modifier
- Arrêter et signaler si le build casse après une correction
- Ne pas regrouper plusieurs corrections en un seul edit si elles touchent des fichiers différents
- Si une correction nécessite de créer un nouveau fichier (ex: type Result<T> manquant), le signaler explicitement avant de le créer

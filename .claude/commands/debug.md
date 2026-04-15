---
description: Déboguer un problème ou bug de manière structurée — hypothèses, investigation, fix, non-régression
---

# Debug

Résout un bug ou comportement inattendu de manière structurée.

---

## Phase 1 — Définir le problème

Avant d'investiguer, formuler clairement :

```
Comportement observé  : [ce qui se passe]
Comportement attendu  : [ce qui devrait se passer]
Contexte              : [quand, dans quel cas, fréquence — ex: en multijoueur, au tick N, avec X joueurs]
Erreur / log          : [message d'erreur exact si disponible]
```

Si l'utilisateur n'a pas fourni ces infos, les demander.

---

## Phase 2 — Collecter les informations

```bash
git log --oneline -10          # changements récents pouvant être liés
just test                      # les tests passent-ils ?
```

Lire les fichiers concernés par le bug — ne pas modifier avant d'avoir compris.

Identifier :
- Le composant principal concerné (GameLogic/Entities, Physics, Rules, Network, Godot/Nodes…)
- Le dernier changement qui a pu introduire la régression (`git log -p -- [fichier]`)
- Les logs Serilog disponibles (niveau Warning/Error)

---

## Phase 3 — Hypothèses

Lister les hypothèses possibles, de la plus probable à la moins probable :

```
H1 : [hypothèse] — probabilité : haute / moyenne / basse
H2 : [hypothèse] — probabilité : ...
H3 : [hypothèse] — ...
```

Présenter les hypothèses à l'utilisateur avant d'investiguer.

---

## Phase 4 — Investigation

Tester chaque hypothèse dans l'ordre :

- Lire le code concerné en détail
- Vérifier les logs Serilog disponibles
- Identifier la ligne / fonction exacte qui produit le comportement incorrect

Règles :
- Ne pas modifier le code avant d'avoir confirmé la cause racine
- Si le bug est côté GameLogic, vérifier que ce n'est pas un bug de séparation (logique côté Godot qui devrait être dans GameLogic)
- Si le bug est réseau, vérifier que le serveur est bien autoritaire (le client ne devrait pas pouvoir causer cet état)

---

## Phase 5 — Fix

Une fois la cause racine identifiée :

1. Proposer le fix à l'utilisateur avant de l'appliquer :
   ```
   Cause racine : [description]
   Fichier : [fichier:ligne]

   Fix proposé :
   - [avant]
   + [après]

   Appliquer ? [O/n]
   ```

2. Appliquer le fix
3. Vérifier la compilation :
   ```bash
   just build
   ```

---

## Phase 6 — Non-régression

```bash
just test
```

Si aucun test ne couvre le cas bugué → en écrire un avant de conclure :
- Test NUnit qui reproduit le bug (aurait échoué avant le fix)
- Test dans `Tests/[layer]/` approprié (Entities, Physics, Rules…)
- Suivre le pattern `Méthode_Contexte_RésultatAttendu`

---

## Phase 7 — Documentation

- Si le bug vient d'une ambiguïté dans le code → ajouter un commentaire explicatif
- Si le bug révèle un gap dans les standards → noter dans `issue.md`
- Mettre à jour `changelog.md` section `[Unreleased]` : `fix(scope): description`

---

## Handoff

À la fin, afficher :

```
Bug résolu : [description courte]
Cause racine : [fichier:ligne]
Test de non-régression : ✅ / ⚠️ à écrire

Étape suivante : committer le fix.
Lancer /commit ? [O/n]
```

Si l'utilisateur valide → invoquer le skill `/commit`.

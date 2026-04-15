---
description: Analyser une fonctionnalité et produire une analyse fonctionnelle détaillée
---

# Analyse Fonctionnelle

Rédige une analyse fonctionnelle détaillée de la feature demandée.

Produit : `docs/analyse/[feature-kebab-case].md`

---

## Phase 1 — Contexte

Avant d'écrire, collecter les informations nécessaires :

- Lire `backlog.md` pour trouver l'item correspondant
- Lire les analyses existantes dans `docs/analyse/` pour cohérence
- Si des fichiers de code existants sont concernés, les lire
- Si un contrat réseau existe (`docs/contracts/protocol.md`), le consulter

Si des informations manquent pour rédiger l'analyse (besoins flous, contraintes inconnues), poser les questions à l'utilisateur avant de continuer.

---

## Phase 2 — Rédaction

Créer `docs/analyse/[feature-kebab-case].md` avec la structure suivante :

```markdown
# Analyse fonctionnalité : [Nom de la feature]

## Version

| Date | Version | Responsable | Description |
|------|---------|-------------|-------------|
| [date du jour] | v1.0.0 | | |

## Description

[Description claire de la fonctionnalité — ce qu'elle fait, pourquoi elle existe, qui l'utilise]

## Besoins

[Liste des besoins fonctionnels — ce que la fonctionnalité doit permettre de faire]

## Contraintes

[Contraintes techniques, métier, ou organisationnelles — règles de jeu, limites réseau, tick rate, etc.]

## Processus

[Description du déroulement de la fonctionnalité, étape par étape]

### Diagramme de séquence

[Insérer les diagrammes PlantUML — voir section diagrammes]

### Détail des données

[Données en entrée, en sortie, transformations, formats attendus]

## Modèle de données

[Nouvelles entités, types C#, structures MessagePack — si applicable]

## Impacts réseau

[Nouveaux messages ENet/MessagePack si la fonctionnalité touche au protocole]

## Cas particuliers

[Edge cases, cas d'erreur, comportements limites à anticiper]

## Questions ouvertes

[Points à clarifier, décisions non prises, hypothèses posées]
```

---

## Phase 3 — Diagrammes PlantUML

Pour chaque flux ou séquence décrit dans l'analyse, générer un diagramme PlantUML :

1. Créer le fichier `.puml` dans `docs/analyse/diagrams/[feature]-[nom].puml`
2. Générer le PNG :
   ```bash
   plantuml docs/analyse/diagrams/[feature]-[nom].puml -o ../../../.attachment/
   ```
3. Référencer l'image dans l'analyse :
   ```markdown
   ![Description du diagramme](../../.attachment/[feature]-[nom].png)
   ```

Types de diagrammes à générer selon le besoin :
- **Séquence** — pour les flux Client ↔ Serveur ↔ GameLogic
- **Activité** — pour les processus de jeu (tour de boucle, règles battle royale)
- **Classes** — pour le modèle d'entités C#

---

## Phase 4 — Mise à jour backlog

Mettre à jour `backlog.md` : marquer l'item comme "Analysé" ou ajouter le lien vers l'analyse.

---

## Handoff

À la fin, afficher :

```
Analyse créée : docs/analyse/[feature-kebab-case].md

Étape suivante : générer la STD sur base de cette analyse.
Lancer /std ? [O/n]
```

Si l'utilisateur valide → invoquer le skill `/std` avec le fichier d'analyse en argument.

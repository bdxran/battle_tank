---
description: Générer une Spécification Technique Détaillée (STD) basée sur le template standard
---

# Générer une STD

Créer un nouveau document Spécification Technique Détaillée (STD) en markdown dans le dossier `docs/std/` avec le contenu suivant :

## Template STD

```markdown
# [Titre de la STD]

## 📌Informations

| **Date**           | **Version** | **Statut** | **Auteur**      | **Compléments d'informations** |
|--------------------|-------------|------------|-----------------|--------------------------------|
| [Date du jour]     | v1.0.0      | Proposé    | [Auteur]        | [À compléter]                  |

## 📚 Contexte

[Le contexte du sujet que traite la STD]

## 🔍 Contraintes

[Les contraintes qui sont posées par le projet par rapport au contexte]

## 🎯 Objectifs

[Les objectifs visés par rapport au contexte]

## ⚙️ Design technique proposé

[Détails de comment est implémentée la solution]

## 📐 Architecture / Diagramme

[Diagramme de séquence, de flux, de classes, etc.]

[Schéma si pertinent]

## 🔌 API (si applicable)

| Méthode | Path | Description | Input | Output |
|---------|------|-------------|-------|--------|
| [À compléter] | [À compléter] | [À compléter] | [À compléter] | [À compléter] |

## 🗄️ Modèle de données

[Décrit les nouvelles entités, tables, colonnes ou structures JSON]

## ⚠️ Cas particuliers / gestion d'erreurs

[Description des cas particuliers et de la gestion d'erreurs]

## 🔐 Sécurité (si concerné)

[Méthodes, outils utilisés, …]

## 📊 Performance / Scalabilité (optionnel)

- Est-ce que cette fonctionnalité peut impacter les performances ?
- Y a-t-il des volumes élevés, des appels fréquents ou des opérations coûteuses à anticiper ?

## 🧪 Tests

[Lister ce qu'il faut tester, quel type de test, couverture visée, …]

## 📦 Déploiement

- Y a-t-il une migration de données à prévoir ?
- Une variable d'environnement ?
- Un redémarrage de services ?

## 🚧 Points ouverts / Limites connues

[Ce qui reste à valider, des hypothèses, des éléments non couverts dans cette version]

## 🧾 Sources

[Les sources qui ont permis de prendre les décisions, documentations, …]
```

## Instructions

### Si un fichier source est spécifié en argument (ex: `/std de monitoring_rms.md`)

1. Extraire le nom du fichier depuis les arguments (format: "de [nom-fichier].md")
2. Lire le fichier source dans `docs/analyse/[nom-fichier].md`
3. Analyser le contenu du fichier source
4. Créer une STD dans `docs/std/STD-[nom-fichier].md` en utilisant le template
5. Pré-remplir intelligemment les sections de la STD en se basant sur le contenu du fichier source :
   - Utiliser les sections existantes pour remplir les sections correspondantes
   - Transformer/adapter le contenu pour correspondre au format STD
   - Remplir la date du jour automatiquement
   - Conserver la structure et le format du template STD

### Si aucun fichier source n'est spécifié

1. Demander à l'utilisateur le titre/sujet de la STD
2. Créer un fichier dans `docs/std/` avec un nom approprié (format : `STD-[titre-en-kebab-case].md`)
3. Remplir automatiquement la date du jour dans le champ Date
4. Inclure toutes les sections du template
5. Laisser les autres champs à compléter par l'utilisateur

**Note importante :** Toutes les sections du template sont optionnelles. L'utilisateur peut supprimer ou ne pas remplir les sections qui ne sont pas pertinentes pour le sujet traité.

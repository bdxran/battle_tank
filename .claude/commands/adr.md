---
description: Générer une Architecture Decision Record (ADR) basée sur le template standard
---

# Générer une ADR

Créer un nouveau document Architecture Decision Record (ADR) en markdown dans le dossier `docs/adr/` avec le contenu suivant :

## Template ADR

```markdown
# [Titre de l'ADR]

## 📌Informations

| **Date**           | **Version** | **Statut** | **Auteur**      | **Compléments d'informations** |
|--------------------|-------------|------------|-----------------|--------------------------------|
| [Date du jour]     | v1.0.0      | Proposé    | [Auteur]        | [À compléter]                  |

## 📚 Contexte

[Le contexte du sujet que traite l'ADR]

## 🔍 Contraintes

[Les contraintes qui sont posées par le projet par rapport au contexte]

## 🎯 Décisions

[La ou les décisions prises par rapport au contexte]

## 💡 Propositions

[Les propositions liées aux décisions]

## 🚫 Solutions écartées

[Les solutions étudiées mais qui n'ont pas été retenues dans les décisions]

## 🧠 Justification

[Les justifications par rapport aux décisions, propositions et/ou des solutions écartées]

## ⚠️ Conséquence

[Les conséquences des décisions prises sur le projet]

## ⏭️ Prochaines étapes

[Les étapes qui doivent suivre après que cet ADR soit validée]

## ✅ Conclusion

[Une conclusion qui résume ce qu'impliquent les décisions prises sur le projet]

## 🧾 Sources

[Les sources qui ont permis de prendre les décisions, documentations, …]
```

## Instructions

### Si un fichier source est spécifié en argument (ex: `/adr de monitoring_rms.md`)

1. Extraire le nom du fichier depuis les arguments (format: "de [nom-fichier].md")
2. Lire le fichier source dans `docs/analyse/[nom-fichier].md`
3. Analyser le contenu du fichier source
4. Créer une ADR dans `docs/adr/ADR-[NNN]-[nom-fichier].md` en utilisant le template (NNN = prochain numéro séquentiel)
5. Pré-remplir intelligemment les sections de l'ADR en se basant sur le contenu du fichier source :
   - Utiliser les sections existantes pour remplir les sections correspondantes
   - Transformer/adapter le contenu pour correspondre au format ADR
   - Remplir la date du jour automatiquement
   - Conserver la structure et le format du template ADR

### Si aucun fichier source n'est spécifié

1. Demander à l'utilisateur le titre/sujet de l'ADR
2. Créer un fichier dans `docs/adr/` avec un nom approprié (format : `ADR-[NNN]-[titre-en-kebab-case].md`, NNN = prochain numéro séquentiel)
3. Remplir automatiquement la date du jour dans le champ Date
4. Inclure toutes les sections du template
5. Laisser les autres champs à compléter par l'utilisateur

**Note importante :** Toutes les sections du template sont optionnelles. L'utilisateur peut supprimer ou ne pas remplir les sections qui ne sont pas pertinentes pour le sujet traité.

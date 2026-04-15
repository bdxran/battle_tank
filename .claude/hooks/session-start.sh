#!/usr/bin/env bash
# session-start.sh — Injecte les obligations workflow et les skills disponibles au démarrage de session

PROJECT_DIR="/home/randy/perso/battle_tank"

OUTPUT=$(
  echo "OBLIGATIONS WORKFLOW (CLAUDE.md) — règles strictes pour cette session :"
  echo ""
  grep -A 40 '## Development Workflow' "$PROJECT_DIR/CLAUDE.md" 2>/dev/null | head -45
  echo ""
  echo "Standards — lire le fichier pertinent AVANT d'implémenter :"
  grep -A 10 '## Quick Reference' "$PROJECT_DIR/CLAUDE.md" 2>/dev/null | head -12
  echo ""
  echo "---"
  echo "SKILLS DISPONIBLES — utiliser ces commandes plutôt que de travailler manuellement :"
  echo ""
  echo "Workflow principal :"
  echo "  /backlog      → gérer les items (ajouter, prioriser, statut)"
  echo "  /analyse      → analyser une fonctionnalité (produit docs/analyse/)"
  echo "  /std          → générer la STD + diagrammes PlantUML (produit docs/std/)"
  echo "  /contract     → créer/mettre à jour le contrat de protocole réseau (ENet/MessagePack)"
  echo "  /implement    → implémenter avec plan mode (utilise EnterPlanMode)"
  echo "  /test         → écrire les tests NUnit (80% coverage GameLogic)"
  echo "  /document     → documenter (CLAUDE.md, docs/contracts/)"
  echo "  /review       → review complète du projet"
  echo "  /review-changes → review des changements en cours"
  echo "  /apply-review → appliquer les corrections interactivement"
  echo "  /commit       → commit conventionnel avec scope"
  echo ""
  echo "Tâches spécifiques :"
  echo "  /debug        → investiguer un bug (hypothèses → fix → non-régression)"
  echo "  /refactor     → refactoring sûr avec plan mode"
  echo "  /audit        → audit complet (dette technique, sécurité)"
  echo "  /adr          → documenter une décision d'architecture"
  echo "  /pr           → générer la description de PR"
  echo ""
  echo "Déclenchement : ces skills s'activent automatiquement selon le contexte."
  echo "Tu peux aussi les invoquer directement avec /nom-du-skill."
)

jq -n --arg ctx "$OUTPUT" '{hookSpecificOutput:{hookEventName:"SessionStart",additionalContext:$ctx}}'

#!/usr/bin/env bash
# session-start.sh — Injecte les obligations workflow au démarrage de session

PROJECT_DIR="/home/randy/perso/battle_tank"

OUTPUT=$(
  echo "OBLIGATIONS WORKFLOW (CLAUDE.md) — règles strictes pour cette session :"
  echo ""
  grep -A 40 '## Development Workflow' "$PROJECT_DIR/CLAUDE.md" 2>/dev/null | head -45
  echo ""
  echo "Standards — lire le fichier pertinent AVANT d'implémenter :"
  grep -A 10 '## Quick Reference' "$PROJECT_DIR/CLAUDE.md" 2>/dev/null | head -12
)

jq -n --arg ctx "$OUTPUT" '{hookSpecificOutput:{hookEventName:"SessionStart",additionalContext:$ctx}}'

#!/usr/bin/env bash
# workflow-guard.sh — Injecte le standard pertinent selon le fichier modifié

PROJECT_DIR="/home/randy/perso/battle_tank"
FILE=$(jq -r '.tool_input.file_path // ""')

if [[ -z "$FILE" ]]; then exit 0; fi

BASENAME=$(basename "$FILE")
STANDARD=""
STANDARD2=""
LABEL=""

# Tests → standard de test
if [[ "$FILE" == *.Tests.cs || "$FILE" == *Tests/*.cs || "$FILE" == */Tests/*.cs ]]; then
  STANDARD="$PROJECT_DIR/standards/testing.md"
  LABEL="STANDARD TESTS"

# Network (Protocol, sérialisation, messages) → standard réseau
elif [[ "$FILE" == */Network/*.cs ]]; then
  STANDARD="$PROJECT_DIR/standards/network.md"
  LABEL="STANDARD RÉSEAU"

# GameLogic (Entities, Physics, Rules, Shared) → standard architecture + C#
elif [[ "$FILE" == */GameLogic/*.cs || "$FILE" == */Entities/*.cs || "$FILE" == */Physics/*.cs || "$FILE" == */Rules/*.cs || "$FILE" == */Shared/*.cs ]]; then
  STANDARD="$PROJECT_DIR/standards/architecture.md"
  STANDARD2="$PROJECT_DIR/standards/csharp-code.md"
  LABEL="STANDARDS ARCHITECTURE + C#"

# Nodes Godot → standard architecture
elif [[ "$FILE" == */Godot/Nodes/*.cs || "$FILE" == */Godot/*.cs ]]; then
  STANDARD="$PROJECT_DIR/standards/architecture.md"
  LABEL="STANDARD ARCHITECTURE"

# Fichiers C# génériques → standard C#
elif [[ "$FILE" == *.cs ]]; then
  STANDARD="$PROJECT_DIR/standards/csharp-code.md"
  LABEL="STANDARD C#"
fi

# Construire le contexte à injecter
if [[ -n "$STANDARD" && -f "$STANDARD" ]]; then
  OUTPUT=$(
    echo "$LABEL — à respecter pour $BASENAME :"
    echo ""
    cat "$STANDARD"
    if [[ -n "$STANDARD2" && -f "$STANDARD2" ]]; then
      echo ""
      echo "---"
      cat "$STANDARD2"
    fi
    echo ""
    echo "---"
    echo "RAPPEL workflow :"
    grep -A 12 'Toujours respecter cet ordre' "$PROJECT_DIR/CLAUDE.md" 2>/dev/null
  )
elif [[ "$FILE" == *.cs || "$FILE" == *.gd || "$FILE" == *.yml || "$FILE" == *.yaml || "$FILE" == *.json ]]; then
  OUTPUT=$(
    echo "RAPPEL workflow avant modification de $BASENAME :"
    echo ""
    grep -A 12 'Toujours respecter cet ordre' "$PROJECT_DIR/CLAUDE.md" 2>/dev/null
    echo ""
    echo "Standards — consulter standards/ si applicable."
  )
else
  exit 0
fi

jq -n --arg ctx "$OUTPUT" '{hookSpecificOutput:{hookEventName:"PreToolUse",additionalContext:$ctx}}'

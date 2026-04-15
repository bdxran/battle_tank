#!/usr/bin/env bash
# workflow-guard.sh — Injecte le standard pertinent et le skill recommandé selon le fichier modifié

PROJECT_DIR="/home/randy/perso/battle_tank"
FILE=$(jq -r '.tool_input.file_path // ""')

if [[ -z "$FILE" ]]; then exit 0; fi

BASENAME=$(basename "$FILE")
STANDARD=""
STANDARD2=""
LABEL=""
SKILL_HINT=""

# Tests → standard de test
if [[ "$FILE" == *.Tests.cs || "$FILE" == *Tests/*.cs || "$FILE" == */Tests/*.cs ]]; then
  STANDARD="$PROJECT_DIR/standards/testing.md"
  LABEL="STANDARD TESTS"
  SKILL_HINT="Skill recommandé : /test (gère l'écriture des tests NUnit en suivant les standards)"

# Network (Protocol, sérialisation, messages) → standard réseau
elif [[ "$FILE" == */Network/*.cs ]]; then
  STANDARD="$PROJECT_DIR/standards/network.md"
  LABEL="STANDARD RÉSEAU"
  SKILL_HINT="Skill recommandé : /contract (mettre à jour le contrat ENet/MessagePack) ou /implement (plan mode)"

# GameLogic (Entities, Physics, Rules, Shared) → standard architecture + C#
elif [[ "$FILE" == */GameLogic/*.cs || "$FILE" == */Entities/*.cs || "$FILE" == */Physics/*.cs || "$FILE" == */Rules/*.cs || "$FILE" == */Shared/*.cs ]]; then
  STANDARD="$PROJECT_DIR/standards/architecture.md"
  STANDARD2="$PROJECT_DIR/standards/csharp-code.md"
  LABEL="STANDARDS ARCHITECTURE + C#"
  SKILL_HINT="Skill recommandé : /implement (plan mode) ou /refactor si restructuration"

# Nodes Godot → standard architecture
elif [[ "$FILE" == */Godot/Nodes/*.cs || "$FILE" == */Godot/*.cs ]]; then
  STANDARD="$PROJECT_DIR/standards/architecture.md"
  LABEL="STANDARD ARCHITECTURE"
  SKILL_HINT="Skill recommandé : /implement (plan mode)"

# Fichiers C# génériques → standard C#
elif [[ "$FILE" == *.cs ]]; then
  STANDARD="$PROJECT_DIR/standards/csharp-code.md"
  LABEL="STANDARD C#"
  SKILL_HINT="Skill recommandé : /implement (plan mode) ou /refactor"

# docs/analyse/ → en cours d'analyse
elif [[ "$FILE" == */docs/analyse/*.md ]]; then
  SKILL_HINT="Skill recommandé : /std (générer la STD depuis cette analyse)"

# docs/std/ → en cours de spec
elif [[ "$FILE" == */docs/std/*.md ]]; then
  SKILL_HINT="Skill recommandé : /implement (implémenter depuis cette STD)"

# docs/contracts/ → contrat réseau
elif [[ "$FILE" == */docs/contracts/*.md ]]; then
  SKILL_HINT="Skill recommandé : /contract (synchroniser Protocol.cs avec le contrat)"

# backlog.md → gestion backlog
elif [[ "$BASENAME" == "backlog.md" ]]; then
  SKILL_HINT="Skill recommandé : /backlog (gérer les items interactivement)"

# changelog.md → fin de workflow
elif [[ "$BASENAME" == "changelog.md" ]]; then
  SKILL_HINT="Skill recommandé : /commit (génère le commit conventionnel après le changelog)"
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
    if [[ -n "$SKILL_HINT" ]]; then
      echo ""
      echo "$SKILL_HINT"
    fi
  )
elif [[ -n "$SKILL_HINT" ]]; then
  OUTPUT=$(
    echo "RAPPEL workflow avant modification de $BASENAME :"
    echo ""
    grep -A 12 'Toujours respecter cet ordre' "$PROJECT_DIR/CLAUDE.md" 2>/dev/null
    echo ""
    echo "$SKILL_HINT"
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

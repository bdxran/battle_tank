#!/usr/bin/env bash
set -e

# Mise à jour automatique de BattleTank — télécharge la dernière release depuis GitHub.
# Ce script est installé dans ~/.local/share/games/battle-tank/ et appelé par le jeu.

REPO="randy/battle_tank"
INSTALL_DIR="$HOME/.local/share/games/battle-tank"

echo "Fetching latest release from github.com/$REPO..."
URL=$(curl -sSL "https://api.github.com/repos/$REPO/releases/latest" \
    | python3 -c "
import sys, json
assets = json.load(sys.stdin)['assets']
url = next(
    a['browser_download_url'] for a in assets
    if 'linux' in a['name'] and 'server' not in a['name']
)
print(url)
")

ARCHIVE="/tmp/battle-tank-update.tar.gz"
echo "Downloading $URL..."
curl -L "$URL" -o "$ARCHIVE"

echo "Installing..."
tar -xzf "$ARCHIVE" -C "$INSTALL_DIR" --strip-components=1
chmod +x "$INSTALL_DIR/BattleTank.x86_64"

echo ""
echo "BattleTank mis à jour avec succès."
echo "Relancer le jeu : battle-tank"

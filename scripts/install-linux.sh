#!/usr/bin/env bash
set -e

# Usage:
#   ./scripts/install-linux.sh                    # télécharge la dernière release
#   ./scripts/install-linux.sh archive.tar.gz     # installe depuis un fichier local

REPO="randy/battle_tank"
INSTALL_DIR="$HOME/.local/share/games/battle-tank"
BIN_LINK="$HOME/.local/bin/battle-tank"
DESKTOP="$HOME/.local/share/applications/battle-tank.desktop"

if [[ -n "$1" ]]; then
    ARCHIVE="$1"
else
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
    ARCHIVE="/tmp/battle-tank-linux.tar.gz"
    echo "Downloading $URL..."
    curl -L "$URL" -o "$ARCHIVE"
fi

echo "Installing to $INSTALL_DIR..."
mkdir -p "$INSTALL_DIR" "$HOME/.local/bin" "$(dirname "$DESKTOP")"
tar -xzf "$ARCHIVE" -C "$INSTALL_DIR" --strip-components=1
chmod +x "$INSTALL_DIR/BattleTank.x86_64"

ln -sf "$INSTALL_DIR/BattleTank.x86_64" "$BIN_LINK"

cat > "$DESKTOP" <<EOF
[Desktop Entry]
Name=BattleTank
Exec=$INSTALL_DIR/BattleTank.x86_64
Icon=$INSTALL_DIR/icon.png
Type=Application
Categories=Game;ActionGame;
EOF

cp "$(dirname "$0")/update-linux.sh" "$INSTALL_DIR/update.sh"
chmod +x "$INSTALL_DIR/update.sh"

echo ""
echo "BattleTank installed successfully."
echo "  Run:     battle-tank"
echo "  Data:    ~/Documents/BattleTank/"
echo "  Update:  $INSTALL_DIR/update.sh"
echo "  Uninstall: rm -rf $INSTALL_DIR $BIN_LINK $DESKTOP"
